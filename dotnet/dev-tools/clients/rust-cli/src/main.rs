use clap::{Parser, Subcommand};
use colored::Colorize;
use futures_util::{SinkExt, StreamExt};
use serde::{Deserialize, Serialize};
use serde_json::json;
use std::io::{self, Write};
use tokio::io::{AsyncBufReadExt, BufReader};
use tokio_tungstenite::{connect_async, tungstenite::Message};

#[derive(Parser)]
#[command(name = "pp-dev")]
#[command(about = "PigeonPea DevTools CLI - Control your running game", long_about = None)]
struct Cli {
    /// WebSocket server URL
    #[arg(short, long, default_value = "ws://127.0.0.1:5007")]
    connect: String,

    #[command(subcommand)]
    command: Option<Commands>,
}

#[derive(Subcommand)]
enum Commands {
    /// Spawn an entity
    Spawn {
        /// Entity type (goblin, potion)
        entity: String,
        /// X coordinate
        x: i32,
        /// Y coordinate
        y: i32,
    },
    /// Teleport player
    Tp {
        /// X coordinate
        x: i32,
        /// Y coordinate
        y: i32,
    },
    /// Query entities
    Query,
    /// Give item to player
    Give {
        /// Item name (potion, health_potion)
        item: String,
    },
    /// Heal player
    Heal {
        /// Amount to heal
        #[arg(default_value = "100")]
        amount: i32,
    },
    /// Kill enemies
    Kill {
        /// Target (all, enemies, or omit for nearest)
        #[arg(default_value = "nearest")]
        target: String,
    },
    /// Interactive REPL mode
    Repl,
}

#[derive(Serialize)]
struct DevCommand {
    #[serde(rename = "type")]
    cmd_type: String,
    cmd: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    args: Option<serde_json::Value>,
}

#[derive(Deserialize, Debug)]
struct DevEvent {
    #[serde(rename = "type")]
    event_type: String,
    event: String,
    #[serde(default)]
    success: bool,
    #[serde(default)]
    message: String,
    #[serde(default)]
    result: Option<serde_json::Value>,
}

#[tokio::main]
async fn main() {
    let cli = Cli::parse();

    match &cli.command {
        Some(Commands::Repl) => {
            run_repl(&cli.connect).await;
        }
        Some(cmd) => {
            run_single_command(&cli.connect, cmd).await;
        }
        None => {
            // Default to REPL mode if no command
            run_repl(&cli.connect).await;
        }
    }
}

async fn run_single_command(url: &str, command: &Commands) {
    match connect_to_server(url).await {
        Ok((mut ws_stream, _)) => {
            let cmd = build_command(command);

            if let Err(e) = ws_stream.send(Message::Text(serde_json::to_string(&cmd).unwrap())).await {
                eprintln!("{} {}", "Error:".red().bold(), e);
                return;
            }

            // Wait for response
            if let Some(msg) = ws_stream.next().await {
                match msg {
                    Ok(Message::Text(text)) => {
                        print_response(&text);
                    }
                    Ok(_) => {}
                    Err(e) => {
                        eprintln!("{} {}", "Error:".red().bold(), e);
                    }
                }
            }

            let _ = ws_stream.close(None).await;
        }
        Err(e) => {
            eprintln!("{} Failed to connect to {}: {}", "Error:".red().bold(), url, e);
            eprintln!("Make sure the game is running with --enable-dev-tools");
        }
    }
}

async fn run_repl(url: &str) {
    println!("{}", "PigeonPea DevTools REPL".cyan().bold());
    println!("Connecting to {}...", url);

    let (mut ws_stream, _) = match connect_to_server(url).await {
        Ok(conn) => conn,
        Err(e) => {
            eprintln!("{} Failed to connect: {}", "Error:".red().bold(), e);
            eprintln!("Make sure the game is running with --enable-dev-tools");
            return;
        }
    };

    println!("{}", "Connected! Type 'help' for commands, 'exit' to quit.".green());
    println!();

    let stdin = tokio::io::stdin();
    let reader = BufReader::new(stdin);
    let mut lines = reader.lines();

    loop {
        print!("{} ", "pp>".cyan().bold());
        io::stdout().flush().unwrap();

        tokio::select! {
            // Handle user input
            line = lines.next_line() => {
                match line {
                    Ok(Some(input)) => {
                        let input = input.trim();

                        if input.is_empty() {
                            continue;
                        }

                        if input == "exit" || input == "quit" {
                            println!("Goodbye!");
                            break;
                        }

                        if input == "help" {
                            print_help();
                            continue;
                        }

                        // Parse and send command
                        if let Some(cmd) = parse_repl_command(input) {
                            if let Err(e) = ws_stream.send(Message::Text(serde_json::to_string(&cmd).unwrap())).await {
                                eprintln!("{} {}", "Error:".red().bold(), e);
                                break;
                            }
                        } else {
                            eprintln!("{} Unknown command. Type 'help' for available commands.", "Error:".red().bold());
                        }
                    }
                    Ok(None) => break,
                    Err(e) => {
                        eprintln!("{} {}", "Error:".red().bold(), e);
                        break;
                    }
                }
            }

            // Handle server messages
            msg = ws_stream.next() => {
                match msg {
                    Some(Ok(Message::Text(text))) => {
                        print_response(&text);
                    }
                    Some(Ok(Message::Close(_))) => {
                        println!("{}", "Server closed connection".yellow());
                        break;
                    }
                    Some(Err(e)) => {
                        eprintln!("{} {}", "Error:".red().bold(), e);
                        break;
                    }
                    _ => {}
                }
            }
        }
    }

    let _ = ws_stream.close(None).await;
}

fn parse_repl_command(input: &str) -> Option<DevCommand> {
    let parts: Vec<&str> = input.split_whitespace().collect();
    if parts.is_empty() {
        return None;
    }

    let cmd = parts[0];

    match cmd {
        "spawn" => {
            if parts.len() < 4 {
                eprintln!("Usage: spawn <entity> <x> <y>");
                return None;
            }
            Some(DevCommand {
                cmd_type: "command".to_string(),
                cmd: "spawn".to_string(),
                args: Some(json!({
                    "entity": parts[1],
                    "x": parts[2].parse::<i32>().ok()?,
                    "y": parts[3].parse::<i32>().ok()?,
                })),
            })
        }
        "tp" | "teleport" => {
            if parts.len() < 3 {
                eprintln!("Usage: tp <x> <y>");
                return None;
            }
            Some(DevCommand {
                cmd_type: "command".to_string(),
                cmd: "tp".to_string(),
                args: Some(json!({
                    "x": parts[1].parse::<i32>().ok()?,
                    "y": parts[2].parse::<i32>().ok()?,
                })),
            })
        }
        "query" | "q" => Some(DevCommand {
            cmd_type: "command".to_string(),
            cmd: "query".to_string(),
            args: None,
        }),
        "give" => {
            if parts.len() < 2 {
                eprintln!("Usage: give <item>");
                return None;
            }
            Some(DevCommand {
                cmd_type: "command".to_string(),
                cmd: "give".to_string(),
                args: Some(json!({
                    "item": parts[1],
                })),
            })
        }
        "heal" => {
            let amount = if parts.len() > 1 {
                parts[1].parse::<i32>().ok()?
            } else {
                100
            };
            Some(DevCommand {
                cmd_type: "command".to_string(),
                cmd: "heal".to_string(),
                args: Some(json!({
                    "amount": amount,
                })),
            })
        }
        "kill" => {
            let target = if parts.len() > 1 { parts[1] } else { "nearest" };
            Some(DevCommand {
                cmd_type: "command".to_string(),
                cmd: "kill".to_string(),
                args: Some(json!({
                    "entity": target,
                })),
            })
        }
        "ping" => Some(DevCommand {
            cmd_type: "command".to_string(),
            cmd: "ping".to_string(),
            args: None,
        }),
        _ => None,
    }
}

fn build_command(command: &Commands) -> DevCommand {
    match command {
        Commands::Spawn { entity, x, y } => DevCommand {
            cmd_type: "command".to_string(),
            cmd: "spawn".to_string(),
            args: Some(json!({
                "entity": entity,
                "x": x,
                "y": y,
            })),
        },
        Commands::Tp { x, y } => DevCommand {
            cmd_type: "command".to_string(),
            cmd: "tp".to_string(),
            args: Some(json!({
                "x": x,
                "y": y,
            })),
        },
        Commands::Query => DevCommand {
            cmd_type: "command".to_string(),
            cmd: "query".to_string(),
            args: None,
        },
        Commands::Give { item } => DevCommand {
            cmd_type: "command".to_string(),
            cmd: "give".to_string(),
            args: Some(json!({
                "item": item,
            })),
        },
        Commands::Heal { amount } => DevCommand {
            cmd_type: "command".to_string(),
            cmd: "heal".to_string(),
            args: Some(json!({
                "amount": amount,
            })),
        },
        Commands::Kill { target } => DevCommand {
            cmd_type: "command".to_string(),
            cmd: "kill".to_string(),
            args: Some(json!({
                "entity": target,
            })),
        },
        Commands::Repl => unreachable!(),
    }
}

fn print_response(text: &str) {
    match serde_json::from_str::<DevEvent>(text) {
        Ok(event) => {
            if event.event == "command_result" {
                if event.success {
                    println!("{} {}", "✓".green().bold(), event.message);
                    if let Some(result) = event.result {
                        println!("{}", serde_json::to_string_pretty(&result).unwrap().dimmed());
                    }
                } else {
                    println!("{} {}", "✗".red().bold(), event.message);
                }
            } else {
                println!("{} {}", "Event:".yellow(), event.message);
            }
        }
        Err(e) => {
            eprintln!("{} Failed to parse response: {}", "Error:".red().bold(), e);
            eprintln!("{}", text.dimmed());
        }
    }
}

fn print_help() {
    println!("{}", "Available Commands:".cyan().bold());
    println!("  {}         - Spawn entity at position", "spawn <entity> <x> <y>".green());
    println!("               Examples: spawn goblin 10 5, spawn potion 20 10");
    println!("  {}                  - Teleport player", "tp <x> <y>".green());
    println!("  {}                      - Query all entities", "query".green());
    println!("  {}                  - Give item to player", "give <item>".green());
    println!("               Examples: give potion, give health_potion");
    println!("  {}             - Heal player", "heal [amount]".green());
    println!("  {}           - Kill enemies", "kill [target]".green());
    println!("               Targets: nearest (default), all, enemies");
    println!("  {}                     - Ping server", "ping".green());
    println!("  {}                      - Show this help", "help".green());
    println!("  {}                      - Exit REPL", "exit".green());
    println!();
}

async fn connect_to_server(url: &str) -> Result<(tokio_tungstenite::WebSocketStream<tokio_tungstenite::MaybeTlsStream<tokio::net::TcpStream>>, tokio_tungstenite::tungstenite::http::Response<()>), tokio_tungstenite::tungstenite::Error> {
    connect_async(url).await
}
