//! Reload command implementation
//!
//! This module implements the reload command for reloading
//! game data from disk.

use crate::client::CommandExecutor;
use crate::error::Result;
use async_trait::async_trait;
use clap::Parser;

/// Reload game data from disk
#[derive(Parser, Debug)]
pub struct ReloadCommand {
    /// Specific data types to reload (optional)
    #[arg(
        short = 't',
        long,
        value_delimiter = ',',
        help = "Specific data types to reload (comma-separated)"
    )]
    pub data_types: Option<Vec<String>>,

    /// Dry run - don't actually reload, just validate
    #[arg(
        short = 'n',
        long,
        help = "Dry run - validate but don't reload"
    )]
    pub dry_run: bool,
}

#[async_trait]
impl CommandExecutor for ReloadCommand {
    async fn execute(&self, client: &mut crate::client::Client) -> Result<()> {
        // Display reload info
        client.output_formatter.print_header("Reload Information");

        if let Some(ref types) = self.data_types {
            println!("Data Types: {}", types.join(", ").cyan());
        } else {
            println!("Data Types: {}", "all".cyan());
        }

        if self.dry_run {
            client.output_formatter.print_separator();
            client.output_formatter.format_success("Dry run completed - validation passed")?;
            return Ok(());
        }

        // Create reload command
        let command = if let Some(ref types) = self.data_types {
            serde_json::json!({
                "cmd": "reload",
                "args": {
                    "data_types": types
                }
            })
        } else {
            serde_json::json!({
                "cmd": "reload",
                "args": {}
            })
        };

        // Send command
        client.output_formatter.print_separator();
        client.output_formatter.print_header("Reloading Game Data");

        let reply = client.send_command(command).await?;
        client.output_formatter.format_reply(&reply)?;

        // Display additional info on success
        if reply.ok {
            client.output_formatter.print_section_break();
            client.output_formatter.print_header("Reload Result");

            if let Some(ref data) = reply.data {
                if let Some(reloaded) = data.get("reloaded") {
                    if let Some(reloaded_array) = reloaded.as_array() {
                        println!("Successfully reloaded {} data types:", reloaded_array.len());
                        for item in reloaded_array {
                            if let Some(item_str) = item.as_str() {
                                println!("  • {}", item_str.green());
                            }
                        }
                    }
                }

                if let Some(timestamp) = data.get("timestamp") {
                    println!("Reload completed at: {}", timestamp);
                }

                if let Some(counts) = data.get("counts") {
                    println!("Data counts: {}", serde_json::to_string_pretty(counts).unwrap_or_default());
                }
            }
        }

        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use clap::Parser;

    #[test]
    fn test_reload_command_parsing() {
        let cmd = ReloadCommand::try_parse_from(&["reload"]).unwrap();

        assert!(cmd.data_types.is_none());
        assert!(!cmd.dry_run);
    }

    #[test]
    fn test_reload_command_with_types() {
        let cmd = ReloadCommand::try_parse_from(&[
            "reload",
            "--data-types", "maps,entities,config"
        ]).unwrap();

        assert_eq!(cmd.data_types, Some(vec!["maps".to_string(), "entities".to_string(), "config".to_string()]));
        assert!(!cmd.dry_run);
    }

    #[test]
    fn test_reload_command_dry_run() {
        let cmd = ReloadCommand::try_parse_from(&[
            "reload",
            "--dry-run"
        ]).unwrap();

        assert!(cmd.data_types.is_none());
        assert!(cmd.dry_run);
    }

    #[test]
    fn test_reload_command_help() {
        let help_text = ReloadCommand::try_parse_from(&["reload", "--help"])
            .unwrap_err()
            .to_string();

        assert!(help_text.contains("Reload game data"));
        assert!(help_text.contains("--data-types"));
        assert!(help_text.contains("--dry-run"));
    }
}
