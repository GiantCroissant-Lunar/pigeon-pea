//! Send command implementation
//!
//! This module implements send command for sending
//! raw JSON commands to the server.

use crate::client::CommandExecutor;
use crate::error::Result;
use async_trait::async_trait;
use clap::Parser;
use serde_json;

/// Send raw JSON command to server
#[derive(Parser, Debug)]
pub struct SendCommand {
    /// JSON command to send
    #[arg(help = "JSON command to send")]
    pub command: String,

    /// Dry run - don't actually send, just validate JSON
    #[arg(
        short = 'n',
        long,
        help = "Dry run - validate JSON but don't send"
    )]
    pub dry_run: bool,
}

#[async_trait]
impl CommandExecutor for SendCommand {
    async fn execute(&self, client: &mut crate::client::Client) -> Result<()> {
        // Parse and validate JSON
        let parsed_command: serde_json::Value = serde_json::from_str(&self.command)
            .map_err(|e| crate::error::CliError::InvalidInput(
                format!("Invalid JSON: {}", e)
            ))?;

        // Display send info
        client.output_formatter.print_header("Send Command Information");
        println!("Command: {}", serde_json::to_string_pretty(&parsed_command).unwrap_or_default());

        if self.dry_run {
            client.output_formatter.print_separator();
            client.output_formatter.format_success("Dry run completed - JSON is valid")?;
            return Ok(());
        }

        // Send command
        client.output_formatter.print_separator();
        client.output_formatter.print_header("Sending Command");

        let reply = client.send_command(parsed_command).await?;
        client.output_formatter.format_reply(&reply)?;

        Ok(())
    }
}
