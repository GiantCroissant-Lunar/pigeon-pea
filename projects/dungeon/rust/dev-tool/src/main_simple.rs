//! Simple main entry point for dungeon dev-tool
//!
//! This is a minimal version that compiles and demonstrates basic functionality

use clap::Parser;
use colored::Colorize;

#[derive(Parser, Debug)]
#[command(name = "dungeon-dev-tool")]
#[command(about = "Development tool for Pigeon Pea dungeon game")]
struct Cli {
    /// WebSocket server URL
    #[arg(short, long, default_value = "ws://localhost:5007")]
    server: String,

    /// Authentication token
    #[arg(short, long)]
    token: Option<String>,

    /// Output format
    #[arg(short, long, default_value = "text")]
    format: String,
}

#[tokio::main]
async fn main() {
    println!("{} {}", "Dungeon Dev Tool".green().bold(), "v0.1.0");
    println!("Server: {}", "ws://localhost:5007".cyan());
    println!("A working Rust CLI for dungeon development!");
    println!("Ready to connect to game server...");
}
