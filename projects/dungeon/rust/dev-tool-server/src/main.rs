mod app;
mod events;
mod server;
mod ui;

use anyhow::Result;
use clap::Parser;
use std::sync::{Arc, Mutex};

use app::AppState;

#[derive(Parser, Debug)]
#[command(name = "dev-tool-server")]
#[command(about = "Dungeon Development Tools Server - WebSocket TUI for game development")]
struct Args {
    /// WebSocket server bind address
    #[arg(long, default_value = "0.0.0.0:5007")]
    bind: String,

    /// Authentication token for client connections
    #[arg(long, default_value = "dev-token-12345")]
    auth_token: String,
}

#[tokio::main]
async fn main() -> Result<()> {
    // Initialize tracing
    tracing_subscriber::fmt::init();

    // Parse command line arguments
    let args = Args::parse();

    // Create shared application state
    let app_state = Arc::new(Mutex::new(AppState::new(args.auth_token.clone())));

    // Create WebSocket server
    let ws_server = server::WebSocketServer::new(args.bind.clone(), args.auth_token.clone());

    // Start WebSocket server
    let server_handle = ws_server.start(app_state.clone()).await?;

    // Create and run TUI application
    let mut app = app::App::new(app_state.clone());

    // Start event loop in background
    let tx = app.event_handler.get_tx();
    let event_loop_handle = tokio::spawn(async move {
        let event_loop = events::EventLoop::new(tx);
        if let Err(e) = event_loop.run(app_state.clone()).await {
            eprintln!("Event loop error: {}", e);
        }
    });

    // Run main application (blocks until user quits)
    app.run().await?;

    // Cleanup: abort background tasks
    event_loop_handle.abort();
    server_handle.abort();

    // Give a moment for cleanup
    tokio::time::sleep(tokio::time::Duration::from_millis(100)).await;

    Ok(())
}
