use anyhow::Result;
use crossterm::event::{Event as CEvent, KeyEvent};
use dungeon_protocol::Envelope;
use std::sync::{Arc, Mutex};
use tokio::sync::mpsc;

use crate::app::{AppState, ClientConnection};

#[derive(Debug, Clone)]
pub enum Event {
    Key(KeyEvent),
    Tick,
    WebSocketMessage(Envelope, String),
    ClientConnected(String, ClientConnection),
    ClientDisconnected(String),
}

pub struct EventHandler {
    tx: mpsc::UnboundedSender<Event>,
    rx: mpsc::UnboundedReceiver<Event>,
}

impl EventHandler {
    pub fn new() -> Self {
        let (tx, rx) = mpsc::unbounded_channel();
        Self { tx, rx }
    }

    pub fn get_tx(&self) -> mpsc::UnboundedSender<Event> {
        self.tx.clone()
    }

    pub fn try_recv(&mut self) -> Result<Event> {
        match self.rx.try_recv() {
            Ok(event) => Ok(event),
            Err(_) => Err(anyhow::anyhow!("No events available")),
        }
    }

    pub fn send_key(&self, key: KeyEvent) -> Result<()> {
        self.tx.send(Event::Key(key))?;
        Ok(())
    }

    pub fn send_tick(&self) -> Result<()> {
        self.tx.send(Event::Tick)?;
        Ok(())
    }

    pub fn send_websocket_message(&self, message: Envelope, client_id: String) -> Result<()> {
        self.tx.send(Event::WebSocketMessage(message, client_id))?;
        Ok(())
    }

    pub fn send_client_connected(&self, client_id: String, client: ClientConnection) -> Result<()> {
        self.tx.send(Event::ClientConnected(client_id, client))?;
        Ok(())
    }

    pub fn send_client_disconnected(&self, client_id: String) -> Result<()> {
        self.tx.send(Event::ClientDisconnected(client_id))?;
        Ok(())
    }
}

pub struct EventLoop {
    tx: mpsc::UnboundedSender<Event>,
}

impl EventLoop {
    pub fn new(tx: mpsc::UnboundedSender<Event>) -> Self {
        Self { tx }
    }

    pub async fn run(
        &self,
        _app_state: Arc<Mutex<AppState>>,
    ) -> Result<()> {
        // Enable crossterm raw mode
        crossterm::execute!(
            std::io::stdout(),
            crossterm::event::EnableMouseCapture
        )?;
        crossterm::terminal::enable_raw_mode()?;

        let tx = self.tx.clone();

        // Spawn blocking event reader
        tokio::task::spawn_blocking(move || {
            loop {
                if let Ok(event) = crossterm::event::read() {
                    match event {
                        CEvent::Key(key) => {
                            eprintln!("[EVENT_READER] Read key event: {:?}", key);
                            if let Err(e) = tx.send(Event::Key(key)) {
                                eprintln!("[EVENT_READER] Failed to send event: {}", e);
                            } else {
                                eprintln!("[EVENT_READER] Sent key event to channel");
                            }
                        }
                        CEvent::Mouse(_) => {
                            // Handle mouse events if needed
                        }
                        CEvent::Resize(_, _) => {
                            // Terminal resize handled by ratatui
                        }
                        _ => {}
                    }
                }
            }
        });

        // Send periodic tick events
        let tx = self.tx.clone();
        tokio::spawn(async move {
            let mut tick_interval = tokio::time::interval(tokio::time::Duration::from_millis(16));
            loop {
                tick_interval.tick().await;
                let _ = tx.send(Event::Tick);
            }
        });

        // Keep running
        std::future::pending::<()>().await;
        Ok(())
    }
}

impl Drop for EventLoop {
    fn drop(&mut self) {
        // Restore terminal state
        let _ = crossterm::terminal::disable_raw_mode();
        let _ = crossterm::execute!(
            std::io::stdout(),
            crossterm::event::DisableMouseCapture
        );
    }
}
