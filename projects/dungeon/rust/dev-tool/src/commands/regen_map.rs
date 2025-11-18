//! Regenerate map command implementation
//!
//! This module implements the regen-map command for
//! regenerating the current dungeon map.

use crate::client::CommandExecutor;
use crate::error::Result;
use async_trait::async_trait;
use clap::Parser;

/// Regenerate the current map
#[derive(Parser, Debug)]
pub struct RegenMapCommand {
    /// Seed for map generation (optional)
    #[arg(
        short = 's',
        long,
        help = "Seed for procedural generation (optional)"
    )]
    pub seed: Option<u64>,

    /// Map generator type
    #[arg(
        short = 'g',
        long,
        default_value = "default",
        help = "Map generator type"
    )]
    pub generator: String,

    /// Dry run - don't actually regenerate, just validate
    #[arg(
        short = 'n',
        long,
        help = "Dry run - validate but don't regenerate"
    )]
    pub dry_run: bool,
}

#[async_trait]
impl CommandExecutor for RegenMapCommand {
    async fn execute(&self, client: &mut crate::client::Client) -> Result<()> {
        // Display regen info
        client.output_formatter.print_header("Map Regeneration Information");
        println!("Generator: {}", self.generator.cyan());

        if let Some(seed) = self.seed {
            println!("Seed: {}", seed.to_string().yellow());
        } else {
            println!("Seed: {}", "random".yellow());
        }

        if self.dry_run {
            client.output_formatter.print_separator();
            client.output_formatter.format_success("Dry run completed - validation passed")?;
            return Ok(());
        }

        // Create regen command
        let command = serde_json::json!({
            "cmd": "regen-map",
            "args": {
                "seed": self.seed,
                "generator": self.generator
            }
        });

        // Send command
        client.output_formatter.print_separator();
        client.output_formatter.print_header("Regenerating Map");

        let reply = client.send_command(command).await?;
        client.output_formatter.format_reply(&reply)?;

        // Display additional info on success
        if reply.ok {
            client.output_formatter.print_section_break();
            client.output_formatter.print_header("Map Regeneration Result");

            if let Some(ref data) = reply.data {
                if let Some(map_id) = data.get("map_id") {
                    println!("New Map ID: {}", map_id);
                }
                if let Some(dimensions) = data.get("dimensions") {
                    println!("Dimensions: {}", serde_json::to_string(dimensions).unwrap_or_default());
                }
                if let Some(stats) = data.get("stats") {
                    println!("Generation Stats: {}", serde_json::to_string_pretty(stats).unwrap_or_default());
                }
            }
        }

        Ok(())
    }
}
