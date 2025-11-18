//! Teleport command implementation
//!
//! This module implements the teleport command for moving the player
//! to specified coordinates.

use crate::client::CommandExecutor;
use crate::commands::validate_coordinates;
use crate::error::Result;
use crate::output::helpers;
use async_trait::async_trait;
use clap::Parser;

/// Teleport player to specified coordinates
#[derive(Parser, Debug)]
pub struct TeleportCommand {
    /// X coordinate to teleport to
    #[arg(short, long, help = "X coordinate")]
    pub x: i32,

    /// Y coordinate to teleport to
    #[arg(short, long, help = "Y coordinate")]
    pub y: i32,

    /// Player/entity to teleport (default: player)
    #[arg(
        short = 't',
        long,
        default_value = "player",
        help = "Entity to teleport (default: player)"
    )]
    pub target: String,

    /// Dry run - don't actually teleport, just validate
    #[arg(
        short = 'n',
        long,
        help = "Dry run - validate but don't teleport"
    )]
    pub dry_run: bool,
}

#[async_trait]
impl CommandExecutor for TeleportCommand {
    async fn execute(&self, client: &mut crate::client::Client) -> Result<()> {
        // Validate coordinates
        validate_coordinates(self.x, self.y)?;

        // Display teleport info
        client.output_formatter.print_header("Teleport Information");
        println!("Target Entity: {}", self.target.cyan());
        println!("Destination: {}", helpers::format_coords(self.x, self.y));

        if self.dry_run {
            client.output_formatter.print_separator();
            client.output_formatter.format_success("Dry run completed - validation passed")?;
            return Ok(());
        }

        // Create teleport command
        let command = serde_json::json!({
            "cmd": "tp",
            "args": {
                "x": self.x,
                "y": self.y
            }
        });

        // Send command
        client.output_formatter.print_separator();
        client.output_formatter.print_header("Teleporting");

        let reply = client.send_command(command).await?;
        client.output_formatter.format_reply(&reply)?;

        // Display additional info on success
        if reply.ok {
            client.output_formatter.print_section_break();
            client.output_formatter.print_header("Teleport Result");

            if let Some(ref data) = reply.data {
                if let Some(position) = data.get("position") {
                    println!("Final Position: {}", helpers::format_coords(
                        position.get("x").and_then(|v| v.as_i64()).unwrap_or(0) as i32,
                        position.get("y").and_then(|v| v.as_i64()).unwrap_or(0) as i32
                    ));
                }
                if let Some(entity) = data.get("entity") {
                    println!("Entity Status: {}", entity);
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
    fn test_teleport_command_parsing() {
        let cmd = TeleportCommand::try_parse_from(&[
            "teleport",
            "--x", "20",
            "--y", "15"
        ]).unwrap();

        assert_eq!(cmd.x, 20);
        assert_eq!(cmd.y, 15);
        assert_eq!(cmd.target, "player");
        assert!(!cmd.dry_run);
    }

    #[test]
    fn test_teleport_command_with_target() {
        let cmd = TeleportCommand::try_parse_from(&[
            "teleport",
            "--x", "30",
            "--y", "25",
            "--target", "goblin_123"
        ]).unwrap();

        assert_eq!(cmd.x, 30);
        assert_eq!(cmd.y, 25);
        assert_eq!(cmd.target, "goblin_123");
        assert!(!cmd.dry_run);
    }

    #[test]
    fn test_teleport_command_dry_run() {
        let cmd = TeleportCommand::try_parse_from(&[
            "teleport",
            "--x", "10",
            "--y", "5",
            "--dry-run"
        ]).unwrap();

        assert_eq!(cmd.x, 10);
        assert_eq!(cmd.y, 5);
        assert_eq!(cmd.target, "player");
        assert!(cmd.dry_run);
    }

    #[test]
    fn test_teleport_command_help() {
        let help_text = TeleportCommand::try_parse_from(&["teleport", "--help"])
            .unwrap_err()
            .to_string();

        assert!(help_text.contains("Teleport player"));
        assert!(help_text.contains("--x"));
        assert!(help_text.contains("--y"));
        assert!(help_text.contains("--target"));
        assert!(help_text.contains("--dry-run"));
    }
}
