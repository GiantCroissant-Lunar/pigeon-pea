//! Spawn command implementation
//!
//! This module implements the spawn command for spawning entities
//! in the dungeon game world.

use crate::client::CommandExecutor;
use crate::commands::{validate_coordinates, validate_mob_type};
use crate::error::Result;
use crate::output::helpers;
use async_trait::async_trait;
use clap::Parser;
use serde_json;
use std::collections::HashMap;

/// Spawn an entity at specified coordinates
#[derive(Parser, Debug)]
pub struct SpawnCommand {
    /// Entity type to spawn (e.g., goblin, dragon, player)
    #[arg(short, long, help = "Entity type to spawn")]
    pub mob: String,

    /// X coordinate where to spawn the entity
    #[arg(short, long, help = "X coordinate")]
    pub x: i32,

    /// Y coordinate where to spawn the entity
    #[arg(short, long, help = "Y coordinate")]
    pub y: i32,

    /// Additional entity properties as JSON
    #[arg(
        short = 'p',
        long,
        help = "Additional entity properties as JSON (e.g., '{\"health\": 100}')"
    )]
    pub properties: Option<String>,

    /// Dry run - don't actually spawn, just validate
    #[arg(
        short = 'n',
        long,
        help = "Dry run - validate but don't spawn the entity"
    )]
    pub dry_run: bool,
}

#[async_trait]
impl CommandExecutor for SpawnCommand {
    async fn execute(&self, client: &mut crate::client::Client) -> Result<()> {
        // Validate inputs
        validate_coordinates(self.x, self.y)?;
        validate_mob_type(&self.mob)?;

        // Parse additional properties
        let properties = if let Some(ref props_str) = self.properties {
            serde_json::from_str(props_str)
                .map_err(|e| crate::error::CliError::InvalidInput(
                    format!("Invalid JSON in properties: {}", e)
                ))?
        } else {
            HashMap::new()
        };

        // Display spawn info
        client.output_formatter.print_header("Spawn Information");
        println!("Entity Type: {}", self.mob.cyan());
        println!("Position: {}", helpers::format_coords(self.x, self.y));
        if !properties.is_empty() {
            println!("Properties: {}", serde_json::to_string_pretty(&properties).unwrap_or_default());
        }

        if self.dry_run {
            client.output_formatter.print_separator();
            client.output_formatter.format_success("Dry run completed - validation passed")?;
            return Ok(());
        }

        // Create spawn command
        let mut spawn_args = serde_json::json!({
            "mob": self.mob,
            "x": self.x,
            "y": self.y
        });

        // Add additional properties if provided
        if !properties.is_empty() {
            for (key, value) in properties {
                spawn_args[key] = value;
            }
        }

        // Send command
        client.output_formatter.print_separator();
        client.output_formatter.print_header("Spawning Entity");

        let command = serde_json::json!({
            "cmd": "spawn",
            "args": spawn_args
        });

        let reply = client.send_command(command).await?;
        client.output_formatter.format_reply(&reply)?;

        // Display additional info on success
        if reply.ok {
            client.output_formatter.print_section_break();
            client.output_formatter.print_header("Spawn Result");

            if let Some(ref data) = reply.data {
                if let Some(entity_id) = data.get("entity_id") {
                    println!("Entity ID: {}", entity_id);
                }
                if let Some(position) = data.get("position") {
                    println!("Final Position: {}", helpers::format_coords(
                        position.get("x").and_then(|v| v.as_i64()).unwrap_or(0) as i32,
                        position.get("y").and_then(|v| v.as_i64()).unwrap_or(0) as i32
                    ));
                }
                if let Some(health) = data.get("health") {
                    println!("Health: {}", health);
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
    fn test_spawn_command_parsing() {
        let cmd = SpawnCommand::try_parse_from(&[
            "spawn",
            "--mob", "goblin",
            "--x", "10",
            "--y", "5"
        ]).unwrap();

        assert_eq!(cmd.mob, "goblin");
        assert_eq!(cmd.x, 10);
        assert_eq!(cmd.y, 5);
        assert!(cmd.properties.is_none());
        assert!(!cmd.dry_run);
    }

    #[test]
    fn test_spawn_command_with_properties() {
        let cmd = SpawnCommand::try_parse_from(&[
            "spawn",
            "--mob", "dragon",
            "--x", "20",
            "--y", "15",
            "--properties", "{\"health\": 200, \"level\": 5}"
        ]).unwrap();

        assert_eq!(cmd.mob, "dragon");
        assert_eq!(cmd.x, 20);
        assert_eq!(cmd.y, 15);
        assert!(cmd.properties.is_some());
        assert!(!cmd.dry_run);

        // Test properties parsing
        if let Some(props) = cmd.properties {
            let parsed: serde_json::Value = serde_json::from_str(&props).unwrap();
            assert_eq!(parsed["health"], 200);
            assert_eq!(parsed["level"], 5);
        }
    }

    #[test]
    fn test_spawn_command_dry_run() {
        let cmd = SpawnCommand::try_parse_from(&[
            "spawn",
            "--mob", "goblin",
            "--x", "10",
            "--y", "5",
            "--dry-run"
        ]).unwrap();

        assert_eq!(cmd.mob, "goblin");
        assert_eq!(cmd.x, 10);
        assert_eq!(cmd.y, 5);
        assert!(cmd.dry_run);
    }

    #[test]
    fn test_spawn_command_invalid_properties() {
        let cmd = SpawnCommand::try_parse_from(&[
            "spawn",
            "--mob", "goblin",
            "--x", "10",
            "--y", "5",
            "--properties", "{invalid json}"
        ]).unwrap();

        assert_eq!(cmd.mob, "goblin");
        assert_eq!(cmd.x, 10);
        assert_eq!(cmd.y, 5);

        // Properties should be invalid JSON when parsed
        if let Some(props) = cmd.properties {
            let parsed = serde_json::from_str::<serde_json::Value>(&props);
            assert!(parsed.is_err());
        }
    }

    #[test]
    fn test_spawn_command_help() {
        let help_text = SpawnCommand::try_parse_from(&["spawn", "--help"])
            .unwrap_err()
            .to_string();

        assert!(help_text.contains("Spawn an entity"));
        assert!(help_text.contains("--mob"));
        assert!(help_text.contains("--x"));
        assert!(help_text.contains("--y"));
        assert!(help_text.contains("--properties"));
        assert!(help_text.contains("--dry-run"));
    }

    #[tokio::test]
    async fn test_spawn_command_validation() {
        let cmd = SpawnCommand {
            mob: "goblin".to_string(),
            x: 10,
            y: 5,
            properties: None,
            dry_run: true,
        };

        // This should pass validation
        let result = cmd.execute(&mut create_mock_client()).await;
        assert!(result.is_ok());
    }

    #[tokio::test]
    async fn test_spawn_command_invalid_coordinates() {
        let cmd = SpawnCommand {
            mob: "goblin".to_string(),
            x: 100001, // Invalid coordinate
            y: 5,
            properties: None,
            dry_run: true,
        };

        // This should fail validation
        let result = cmd.execute(&mut create_mock_client()).await;
        assert!(result.is_err());
    }

    // Helper function for testing
    fn create_mock_client() -> crate::client::Client {
        use crate::config::{Config, OutputFormat};

        let config = Config {
            server_url: "ws://localhost:8080".to_string(),
            token: None,
            timeout_ms: 5000,
            output_format: OutputFormat::Text,
        };

        // Note: This will fail to connect, but that's okay for validation tests
        crate::client::Client {
            config,
            ws_stream: None,
            output_formatter: crate::output::OutputFormatter::new(OutputFormat::Text),
        }
    }
}
