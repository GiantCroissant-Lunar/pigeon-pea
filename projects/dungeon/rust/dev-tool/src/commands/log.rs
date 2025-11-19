//! Log command implementation
//!
//! This module implements log command for streaming
//! game logs from server.

use crate::client::CommandExecutor;
use crate::error::Result;
use async_trait::async_trait;
use clap::Parser;

/// Stream game logs from server
#[derive(Parser, Debug)]
pub struct LogCommand {
    /// Log levels to filter
    #[arg(
        short = 'l',
        long,
        value_delimiter = ',',
        help = "Log levels to filter (comma-separated)"
    )]
    pub levels: Option<Vec<String>>,

    /// Follow log stream
    #[arg(
        short = 'f',
        long,
        help = "Follow log stream (continuously output)"
    )]
    pub follow: bool,

    /// Limit number of log entries
    #[arg(
        short = 'n',
        long,
        help = "Limit number of log entries to output"
    )]
    pub limit: Option<usize>,

    /// Format output as JSON
    #[arg(
        short = 'j',
        long,
        help = "Format output as JSON"
    )]
    pub json: bool,
}

#[async_trait]
impl CommandExecutor for LogCommand {
    async fn execute(&self, client: &mut crate::client::Client) -> Result<()> {
        // Display log request info
        client.output_formatter.print_header("Log Stream Information");

        if let Some(ref levels) = self.levels {
            println!("Log Levels: {}", levels.join(", ").cyan());
        } else {
            println!("Log Levels: {}", "all".cyan());
        }

        println!("Follow: {}", if self.follow { "yes".green() } else { "no".red() });

        if let Some(limit) = self.limit {
            println!("Limit: {}", limit.to_string().yellow());
        }

        // Create log command
        let command = serde_json::json!({
            "cmd": "stream-logs",
            "args": {
                "levels": self.levels,
                "follow": self.follow,
                "limit": self.limit
            }
        });

        // Send command
        client.output_formatter.print_separator();
        client.output_formatter.print_header("Starting Log Stream");

        let reply = client.send_command(command).await?;
        client.output_formatter.format_reply(&reply)?;

        // If follow is true, start streaming events
        if reply.ok && self.follow {
            client.output_formatter.print_section_break();
            client.output_formatter.print_header("Live Log Stream");

            // Start streaming events from the client
            client.stream_events().await?;
        }

        Ok(())
    }
}
