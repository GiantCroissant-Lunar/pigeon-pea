use anyhow::Result;
use chrono::{DateTime, Utc};
use crossterm::event::{KeyCode, KeyEvent, KeyModifiers};
use dungeon_protocol::{Envelope, MessageType};
use ratatui::{
    backend::CrosstermBackend,
    layout::{Constraint, Direction, Layout},
    style::{Color, Style},
    widgets::{Block, Borders, List, ListItem, Paragraph, Wrap},
    Frame, Terminal,
};
use std::{
    collections::{HashMap, VecDeque},
    time::Instant,
};
use std::sync::{Arc, Mutex};

use crate::events::{Event, EventHandler};

#[derive(Debug, Clone)]
pub struct ClientConnection {
    pub id: String,
    pub address: std::net::SocketAddr,
    pub player_pos: (i32, i32),
    pub connected_at: chrono::DateTime<chrono::Utc>,
    pub last_ping: chrono::DateTime<chrono::Utc>,
}

impl ClientConnection {
    pub fn new(address: std::net::SocketAddr) -> Self {
        Self {
            id: uuid::Uuid::new_v4().to_string(),
            address,
            player_pos: (0, 0),
            connected_at: chrono::Utc::now(),
            last_ping: chrono::Utc::now(),
        }
    }
}

#[derive(Debug, Clone)]
pub struct LogEntry {
    pub timestamp: DateTime<Utc>,
    pub level: LogLevel,
    pub source: String,
    pub message: String,
}

#[derive(Debug, Clone, PartialEq)]
pub enum LogLevel {
    Info,
    Warn,
    Error,
    Debug,
}

impl LogEntry {
    pub fn info(source: &str, message: &str) -> Self {
        Self {
            timestamp: Utc::now(),
            level: LogLevel::Info,
            source: source.to_string(),
            message: message.to_string(),
        }
    }

    pub fn warn(source: &str, message: &str) -> Self {
        Self {
            timestamp: Utc::now(),
            level: LogLevel::Warn,
            source: source.to_string(),
            message: message.to_string(),
        }
    }

    pub fn error(source: &str, message: &str) -> Self {
        Self {
            timestamp: Utc::now(),
            level: LogLevel::Error,
            source: source.to_string(),
            message: message.to_string(),
        }
    }

    pub fn debug(source: &str, message: &str) -> Self {
        Self {
            timestamp: Utc::now(),
            level: LogLevel::Debug,
            source: source.to_string(),
            message: message.to_string(),
        }
    }
}

#[derive(Debug, Clone)]
pub struct AppState {
    pub auth_token: String,
    pub clients: HashMap<String, ClientConnection>,
    pub logs: VecDeque<LogEntry>,
    pub command_history: VecDeque<String>,
    pub current_command: String,
    pub selected_client_index: usize,
    pub server_start_time: Instant,
    pub log_filter: Option<LogLevel>,
}

impl AppState {
    pub fn new(auth_token: String) -> Self {
        Self {
            auth_token,
            clients: HashMap::new(),
            logs: VecDeque::new(),
            command_history: VecDeque::new(),
            current_command: String::new(),
            selected_client_index: 0,
            server_start_time: Instant::now(),
            log_filter: None,
        }
    }

    pub fn add_log(&mut self, entry: LogEntry) {
        self.logs.push_back(entry);
        // Keep only last 1000 logs
        if self.logs.len() > 1000 {
            self.logs.pop_front();
        }
    }

    pub fn add_client(&mut self, id: String, client: ClientConnection) {
        self.clients.insert(id, client);
    }

    pub fn remove_client(&mut self, id: &str) {
        self.clients.remove(id);
    }

    pub fn get_client_ids(&self) -> Vec<String> {
        self.clients.keys().cloned().collect()
    }

    pub fn get_selected_client_id(&self) -> Option<String> {
        self.clients
            .keys()
            .nth(self.selected_client_index)
            .cloned()
    }
}

pub struct App {
    pub state: Arc<Mutex<AppState>>,
    pub should_quit: bool,
    pub event_handler: EventHandler,
}

impl App {
    pub fn new(state: Arc<Mutex<AppState>>) -> Self {
        Self {
            state,
            should_quit: false,
            event_handler: EventHandler::new(),
        }
    }

    pub async fn run(&mut self) -> Result<()> {
        // Initialize terminal
        let mut terminal = Terminal::new(
            CrosstermBackend::new(std::io::stdout()),
        )?;
        terminal.clear()?;

        // Main event loop
        while !self.should_quit {
            self.draw(&mut terminal)?;

            // Handle events
            while let Ok(event) = self.event_handler.try_recv() {
                self.handle_event(event).await?;
            }

            // Small delay to prevent CPU spinning
            tokio::time::sleep(tokio::time::Duration::from_millis(16)).await;
        }

        // Restore terminal
        terminal.show_cursor()?;
        terminal.clear()?;
        Ok(())
    }

    async fn handle_event(&mut self, event: Event) -> Result<()> {
        match event {
            Event::Key(key) => {
                self.handle_key_event(key).await?;
                Ok(())
            }
            Event::Tick => {
                // Update any time-based UI elements
                Ok(())
            }
            Event::WebSocketMessage(message, client_id) => {
                self.handle_websocket_message(message, client_id).await?;
                Ok(())
            }
            Event::ClientConnected(client_id, client) => {
                let mut state = self.state.lock().unwrap();
                state.add_client(client_id.clone(), client);
                state.add_log(LogEntry::info(
                    "Server",
                    &format!("Client connected: {}", client_id),
                ));
                drop(state);
                Ok(())
            }
            Event::ClientDisconnected(client_id) => {
                let mut state = self.state.lock().unwrap();
                state.remove_client(&client_id);
                state.add_log(LogEntry::info(
                    "Server",
                    &format!("Client disconnected: {}", client_id),
                ));
                drop(state);
                Ok(())
            }
        }
    }

    async fn handle_key_event(&mut self, key: KeyEvent) -> Result<()> {
        eprintln!("[KEY_HANDLER] Received key event: {:?}", key);
        match key.code {
            // Handle Ctrl+C first (more specific pattern)
            KeyCode::Char('c') if key.modifiers.contains(KeyModifiers::CONTROL) => {
                eprintln!("[KEY_HANDLER] Ctrl+C detected, quitting");
                self.should_quit = true;
            }
            KeyCode::Char(c) => {
                eprintln!("[KEY_HANDLER] Processing char: '{}' (before push)", c);
                let mut state = self.state.lock().unwrap();
                state.current_command.push(c);
                eprintln!("[KEY_HANDLER] Current command is now: '{}'", state.current_command);
                drop(state);
            }
            KeyCode::Backspace => {
                let mut state = self.state.lock().unwrap();
                state.current_command.pop();
                drop(state);
            }
            KeyCode::Enter => {
                let command = {
                    let mut state = self.state.lock().unwrap();
                    if state.current_command.is_empty() {
                        None
                    } else {
                        let command = state.current_command.clone();
                        state.command_history.push_back(command.clone());
                        if state.command_history.len() > 100 {
                            state.command_history.pop_front();
                        }
                        state.current_command.clear();
                        Some(command)
                    }
                }; // Lock drops here

                if let Some(cmd) = command {
                    self.execute_command(cmd).await?;
                }
            }
            KeyCode::Up => {
                let mut state = self.state.lock().unwrap();
                if state.selected_client_index > 0 {
                    state.selected_client_index -= 1;
                }
                drop(state);
            }
            KeyCode::Down => {
                let mut state = self.state.lock().unwrap();
                if state.selected_client_index < state.clients.len().saturating_sub(1) {
                    state.selected_client_index += 1;
                }
                drop(state);
            }
            KeyCode::Tab => {
                // Navigate command history
                let mut state = self.state.lock().unwrap();
                if let Some(last_command) = state.command_history.back() {
                    state.current_command = last_command.clone();
                }
                drop(state);
            }
            KeyCode::F(1) => {
                let mut state = self.state.lock().unwrap();
                state.log_filter = None; // Show all logs
                drop(state);
            }
            KeyCode::F(2) => {
                let mut state = self.state.lock().unwrap();
                state.log_filter = Some(LogLevel::Error); // Show only errors
                drop(state);
            }
            KeyCode::F(3) => {
                let mut state = self.state.lock().unwrap();
                state.logs.clear();
                drop(state);
            }
            _ => {}
        }
        Ok(())
    }

    async fn execute_command(&mut self, command: String) -> Result<()> {
        let mut state = self.state.lock().unwrap();

        // Simple command parsing
        if command.starts_with('/') {
            let parts: Vec<&str> = command.trim_start_matches('/').split_whitespace().collect();
            if parts.is_empty() {
                return Ok(());
            }

            match parts[0] {
                "help" => {
                    state.add_log(LogEntry::info("Command", "Available commands: help, broadcast <cmd>, select <index>, quit"));
                }
                "quit" | "exit" => {
                    self.should_quit = true;
                }
                "broadcast" => {
                    if parts.len() > 1 {
                        let cmd = parts[1..].join(" ");
                        state.add_log(LogEntry::info("Command", &format!("Broadcasting: {}", cmd)));
                        // TODO: Send to all clients
                    }
                }
                "select" => {
                    if parts.len() > 1 {
                        if let Ok(index) = parts[1].parse::<usize>() {
                            state.selected_client_index = index.min(state.clients.len().saturating_sub(1));
                            state.add_log(LogEntry::info("Command", &format!("Selected client index: {}", index)));
                        }
                    }
                }
                _ => {
                    state.add_log(LogEntry::warn("Command", &format!("Unknown command: {}", parts[0])));
                }
            }
        } else {
            state.add_log(LogEntry::warn("Command", "Commands must start with '/'"));
        }

        drop(state);
        Ok(())
    }

    async fn handle_websocket_message(&self, message: Envelope, client_id: String) -> Result<()> {
        let mut state = self.state.lock().unwrap();

        match message.msg_type {
            MessageType::EventState => {
                state.add_log(LogEntry::debug(&client_id, "State update received"));
            }
            MessageType::EventLog => {
                state.add_log(LogEntry::debug(&client_id, "Log event received"));
            }
            MessageType::GmReply => {
                state.add_log(LogEntry::info(&client_id, "Command reply received"));
            }
            _ => {
                state.add_log(LogEntry::warn(&client_id, "Unexpected message type"));
            }
        }

        drop(state);
        Ok(())
    }

    fn draw(&mut self, terminal: &mut Terminal<CrosstermBackend<std::io::Stdout>>) -> Result<()> {
        terminal.draw(|f| {
            let size = f.area();

            // Create main layout
            let chunks = Layout::default()
                .direction(Direction::Vertical)
                .constraints([
                    Constraint::Length(3),  // Status bar
                    Constraint::Min(8),   // Clients panel
                    Constraint::Min(10),   // Logs panel
                    Constraint::Length(3),  // Command input
                ])
                .split(size);

            self.draw_status_bar(f, chunks[0]);
            self.draw_clients_panel(f, chunks[1]);
            self.draw_logs_panel(f, chunks[2]);
            self.draw_command_input(f, chunks[3]);
        })?;
        Ok(())
    }

    fn draw_status_bar(&self, f: &mut Frame, area: ratatui::layout::Rect) {
        let state = self.state.lock().unwrap();
        let uptime = state.server_start_time.elapsed();
        let uptime_str = format!("{}s", uptime.as_secs());

        let status_text = format!(
            "🎮 Dungeon Dev Server | ● Running | ws://0.0.0.0:5007 | Uptime: {} | Clients: {}",
            uptime_str,
            state.clients.len()
        );

        let paragraph = Paragraph::new(status_text)
            .style(Style::default().fg(Color::Cyan))
            .block(Block::default().borders(Borders::ALL));

        f.render_widget(paragraph, area);
        drop(state);
    }

    fn draw_clients_panel(&self, f: &mut Frame, area: ratatui::layout::Rect) {
        let state = self.state.lock().unwrap();

        let client_items: Vec<ListItem> = state
            .clients
            .iter()
            .enumerate()
            .map(|(index, (id, client))| {
                let style = if index == state.selected_client_index {
                    Style::default().bg(Color::Blue).fg(Color::White)
                } else {
                    Style::default()
                };

                let text = format!("● {} | {} | Player ({}, {})",
                    id,
                    client.address,
                    client.player_pos.0,
                    client.player_pos.1
                );

                ListItem::new(text).style(style)
            })
            .collect();

        let clients_list = List::new(client_items)
            .block(
                Block::default()
                    .borders(Borders::ALL)
                    .title("Connected Clients")
                    .title_style(Style::default().fg(Color::Yellow))
            );

        f.render_widget(clients_list, area);
        drop(state);
    }

    fn draw_logs_panel(&self, f: &mut Frame, area: ratatui::layout::Rect) {
        let state = self.state.lock().unwrap();

        let log_items: Vec<ListItem> = state
            .logs
            .iter()
            .rev() // Show newest first
            .take((area.height as usize).saturating_sub(2))
            .map(|log| {
                let timestamp = log.timestamp.format("%H:%M:%S").to_string();
                let level_style = match log.level {
                    LogLevel::Info => Style::default().fg(Color::Green),
                    LogLevel::Warn => Style::default().fg(Color::Yellow),
                    LogLevel::Error => Style::default().fg(Color::Red),
                    LogLevel::Debug => Style::default().fg(Color::Gray),
                };

                let text = format!("{} [{}] {}", timestamp, log.source, log.message);
                ListItem::new(text).style(level_style)
            })
            .collect();

        let logs_list = List::new(log_items)
            .block(
                Block::default()
                    .borders(Borders::ALL)
                    .title(format!("Logs [F1: All | F2: Errors | F3: Clear]"))
                    .title_style(Style::default().fg(Color::Yellow))
            );

        f.render_widget(logs_list, area);
        drop(state);
    }

    fn draw_command_input(&self, f: &mut Frame, area: ratatui::layout::Rect) {
        let state = self.state.lock().unwrap();

        let input_text = format!("> {}_", state.current_command);

        let paragraph = Paragraph::new(input_text)
            .style(Style::default().fg(Color::White))
            .block(
                Block::default()
                    .borders(Borders::ALL)
                    .title("Command [Tab: History | Ctrl+C: Exit]")
                    .title_style(Style::default().fg(Color::Yellow))
            )
            .wrap(Wrap { trim: true });

        f.render_widget(paragraph, area);
        drop(state);
    }
}
