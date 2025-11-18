//! State command implementation
//!
//! This module implements state command for requesting
//! current game state from server.

use crate::client::CommandExecutor;
use crate::error::Result;
use async_trait::async_trait;
use clap::Parser;

/// Get current game state
#[derive(Parser, Debug)]
pub struct StateCommand {
    /// Specific state components to request
    #[arg(
        short = 'c',
        long,
        value_delimiter = ',',
        help = "Specific state components to request (comma-separated)"
    )]
    pub components: Option<Vec<String>>,

    /// Format output as JSON
    #[arg(
        short = 'j',
        long,
        help = "Format output as JSON"
    )]
    pub json: bool,
}

#[async_trait]
impl CommandExecutor for StateCommand {
    async fn execute(&self, client: &mut crate::client::Client) -> Result<()> {
        // Display state request info
        client.output_formatter.print_header("State Request Information");

        if let Some(ref components) = self.components {
            println!("Components: {}", components.join(", ").cyan());
        } else {
            println!("Components: {}", "all".cyan());
        }

        // Create state command
        let command = if let Some(ref components) = self.components {
            serde_json::json!({
                "cmd": "get-state",
                "args": {
                    "components": components
                }
            })
        } else {
            serde_json::json!({
                "cmd": "get-state",
                "args": {}
            })
        };

        // Send command
        client.output_formatter.print_separator();
        client.output_formatter.print_header("Requesting Game State");

        let reply = client.send_command(command).await?;
        client.output_formatter.format_reply(&reply)?;

        // Display additional info on success
        if reply.ok {
            client.output_formatter.print_section_break();
            client.output_formatter.print_header("Game State Result");

            if let Some(ref data) = reply.data {
                if self.json {
                    println!("{}", serde_json::to_string_pretty(data).unwrap_or_default());
                } else {
                    // Pretty print common state components
                    if let Some(player) = data.get("player") {
                        println!("Player: {}", serde_json::to_string_pretty(player).unwrap_or_default());
                    }
                    if let Some(map) = data.get("map") {
                        println!("Map: {}", serde_json::to_string_pretty(map).unwrap_or_default());
                    }
                    if let Some(entities) = data.get("entities") {
                        if let Some(count) = entities.as_u64() {
                            println!("Entity Count: {}", count);
                        } else {
                            println!("Entities: {}", serde_json::to_string_pretty(entities).unwrap_or_default());
                        }
                    }
                }
            }
        }

        Ok(())
    }
}
