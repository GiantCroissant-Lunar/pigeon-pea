//! Load map command implementation
//!
//! This module implements the load-map command for loading
//! a specific map file.

use crate::client::CommandExecutor;
use crate::commands::validate_file_path;
use crate::error::Result;
use async_trait::async_trait;
use clap::Parser;

/// Load a specific map file
#[derive(Parser, Debug)]
pub struct LoadMapCommand {
    /// Path to map file to load
    #[arg(help = "Path to map file")]
    pub path: String,

    /// Dry run - don't actually load, just validate
    #[arg(
        short = 'n',
        long,
        help = "Dry run - validate but don't load"
    )]
    pub dry_run: bool,
}

#[async_trait]
impl CommandExecutor for LoadMapCommand {
    async fn execute(&self, client: &mut crate::client::Client) -> Result<()> {
        // Validate path
        validate_file_path(&self.path)?;

        // Display load info
        client.output_formatter.print_header("Map Load Information");
        println!("Map Path: {}", self.path.cyan());

        if self.dry_run {
            client.output_formatter.print_separator();
            client.output_formatter.format_success("Dry run completed - validation passed")?;
            return Ok(());
        }

        // Create load command
        let command = serde_json::json!({
            "cmd": "load-map",
            "args": {
                "path": self.path
            }
        });

        // Send command
        client.output_formatter.print_separator();
        client.output_formatter.print_header("Loading Map");

        let reply = client.send_command(command).await?;
        client.output_formatter.format_reply(&reply)?;

        // Display additional info on success
        if reply.ok {
            client.output_formatter.print_section_break();
            client.output_formatter.print_header("Map Load Result");

            if let Some(ref data) = reply.data {
                if let Some(map_id) = data.get("map_id") {
                    println!("Loaded Map ID: {}", map_id);
                }
                if let Some(name) = data.get("name") {
                    println!("Map Name: {}", name);
                }
                if let Some(dimensions) = data.get("dimensions") {
                    println!("Dimensions: {}", serde_json::to_string(dimensions).unwrap_or_default());
                }
            }
        }

        Ok(())
    }
}
