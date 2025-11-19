//! Command types for the dungeon protocol
//!
//! This module defines all command types that can be sent from the dev-tool
//! to the game server.

use serde::{Deserialize, Serialize};
use std::collections::HashMap;

/// All available GM commands
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum GmCommand {
    /// Spawn an entity at specified coordinates
    Spawn(SpawnCommand),
    /// Teleport player to coordinates
    Teleport(TeleportCommand),
    /// Reload game data from disk
    Reload(ReloadCommand),
    /// Regenerate current map
    RegenMap(RegenMapCommand),
    /// Load specific map file
    LoadMap(LoadMapCommand),
    /// Custom command with arbitrary payload
    Custom(CustomCommand),
}

/// Spawn an entity
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct SpawnCommand {
    /// Entity type to spawn
    pub mob: String,
    /// X coordinate
    pub x: i32,
    /// Y coordinate
    pub y: i32,
    /// Optional additional properties
    #[serde(skip_serializing_if = "HashMap::is_empty", default)]
    pub properties: HashMap<String, serde_json::Value>,
}

/// Teleport player
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct TeleportCommand {
    /// X coordinate
    pub x: i32,
    /// Y coordinate
    pub y: i32,
}

/// Reload game data
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ReloadCommand {
    /// Optional specific data types to reload
    #[serde(skip_serializing_if = "Option::is_none")]
    pub data_types: Option<Vec<String>>,
}

/// Regenerate map
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct RegenMapCommand {
    /// Optional seed for generation (None = random)
    #[serde(skip_serializing_if = "Option::is_none")]
    pub seed: Option<u64>,
    /// Optional map generator type
    #[serde(skip_serializing_if = "Option::is_none")]
    pub generator: Option<String>,
}

/// Load specific map
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct LoadMapCommand {
    /// Path to map file
    pub path: String,
    /// Optional additional load options
    #[serde(skip_serializing_if = "HashMap::is_empty", default)]
    pub options: HashMap<String, serde_json::Value>,
}

/// Custom command with arbitrary payload
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct CustomCommand {
    /// Command name
    pub cmd: String,
    /// Command arguments
    #[serde(rename = "args")]
    pub arguments: HashMap<String, serde_json::Value>,
}

impl GmCommand {
    /// Get the command name as string
    pub fn name(&self) -> &str {
        match self {
            GmCommand::Spawn(_) => "spawn",
            GmCommand::Teleport(_) => "tp",
            GmCommand::Reload(_) => "reload",
            GmCommand::RegenMap(_) => "regen-map",
            GmCommand::LoadMap(_) => "load-map",
            GmCommand::Custom(custom) => custom.cmd.as_str(),
        }
    }

    /// Convert to the protocol payload format
    pub fn to_payload(&self) -> serde_json::Value {
        match self {
            GmCommand::Spawn(cmd) => serde_json::json!({
                "cmd": "spawn",
                "args": cmd
            }),
            GmCommand::Teleport(cmd) => serde_json::json!({
                "cmd": "tp",
                "args": cmd
            }),
            GmCommand::Reload(cmd) => serde_json::json!({
                "cmd": "reload",
                "args": cmd
            }),
            GmCommand::RegenMap(cmd) => serde_json::json!({
                "cmd": "regen-map",
                "args": cmd
            }),
            GmCommand::LoadMap(cmd) => serde_json::json!({
                "cmd": "load-map",
                "args": cmd
            }),
            GmCommand::Custom(cmd) => serde_json::json!({
                "cmd": cmd.cmd,
                "args": cmd.arguments
            }),
        }
    }

    /// Parse from protocol payload
    pub fn from_payload(payload: serde_json::Value) -> Result<Self, crate::ProtocolError> {
        let obj = payload.as_object()
            .ok_or_else(|| crate::ProtocolError::InvalidPayload("Payload must be object".to_string()))?;

        let cmd = obj.get("cmd")
            .and_then(|v| v.as_str())
            .ok_or_else(|| crate::ProtocolError::InvalidPayload("Missing 'cmd' field".to_string()))?;

        let args = obj.get("args")
            .cloned()
            .unwrap_or_else(|| serde_json::Value::Object(serde_json::Map::new()));

        match cmd {
            "spawn" => {
                let spawn_cmd: SpawnCommand = serde_json::from_value(args)
                    .map_err(|e| crate::ProtocolError::InvalidPayload(
                        format!("Invalid spawn command: {}", e)
                    ))?;
                Ok(GmCommand::Spawn(spawn_cmd))
            }
            "tp" => {
                let tp_cmd: TeleportCommand = serde_json::from_value(args)
                    .map_err(|e| crate::ProtocolError::InvalidPayload(
                        format!("Invalid teleport command: {}", e)
                    ))?;
                Ok(GmCommand::Teleport(tp_cmd))
            }
            "reload" => {
                let reload_cmd: ReloadCommand = serde_json::from_value(args)
                    .map_err(|e| crate::ProtocolError::InvalidPayload(
                        format!("Invalid reload command: {}", e)
                    ))?;
                Ok(GmCommand::Reload(reload_cmd))
            }
            "regen-map" => {
                let regen_cmd: RegenMapCommand = serde_json::from_value(args)
                    .map_err(|e| crate::ProtocolError::InvalidPayload(
                        format!("Invalid regen-map command: {}", e)
                    ))?;
                Ok(GmCommand::RegenMap(regen_cmd))
            }
            "load-map" => {
                let load_cmd: LoadMapCommand = serde_json::from_value(args)
                    .map_err(|e| crate::ProtocolError::InvalidPayload(
                        format!("Invalid load-map command: {}", e)
                    ))?;
                Ok(GmCommand::LoadMap(load_cmd))
            }
            _ => {
                let custom_cmd = CustomCommand {
                    cmd: cmd.to_string(),
                    arguments: serde_json::from_value(args).unwrap_or_default(),
                };
                Ok(GmCommand::Custom(custom_cmd))
            }
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use serde_json::json;

    #[test]
    fn test_spawn_command_serialization() {
        let cmd = GmCommand::Spawn(SpawnCommand {
            mob: "goblin".to_string(),
            x: 10,
            y: 5,
            properties: HashMap::new(),
        });

        let payload = cmd.to_payload();
        assert_eq!(payload["cmd"], "spawn");
        assert_eq!(payload["args"]["mob"], "goblin");
        assert_eq!(payload["args"]["x"], 10);
        assert_eq!(payload["args"]["y"], 5);
    }

    #[test]
    fn test_teleport_command_serialization() {
        let cmd = GmCommand::Teleport(TeleportCommand { x: 20, y: 3 });

        let payload = cmd.to_payload();
        assert_eq!(payload["cmd"], "tp");
        assert_eq!(payload["args"]["x"], 20);
        assert_eq!(payload["args"]["y"], 3);
    }

    #[test]
    fn test_spawn_command_parsing() {
        let payload = json!({
            "cmd": "spawn",
            "args": {
                "mob": "goblin",
                "x": 10,
                "y": 5
            }
        });

        let cmd = GmCommand::from_payload(payload).unwrap();
        match cmd {
            GmCommand::Spawn(spawn) => {
                assert_eq!(spawn.mob, "goblin");
                assert_eq!(spawn.x, 10);
                assert_eq!(spawn.y, 5);
            }
            _ => panic!("Expected spawn command"),
        }
    }

    #[test]
    fn test_custom_command_parsing() {
        let payload = json!({
            "cmd": "custom_command",
            "args": {
                "param1": "value1",
                "param2": 42
            }
        });

        let cmd = GmCommand::from_payload(payload).unwrap();
        match cmd {
            GmCommand::Custom(custom) => {
                assert_eq!(custom.cmd, "custom_command");
                assert_eq!(custom.arguments.get("param1"), Some(&json!("value1")));
                assert_eq!(custom.arguments.get("param2"), Some(&json!(42)));
            }
            _ => panic!("Expected custom command"),
        }
    }

    #[test]
    fn test_command_names() {
        assert_eq!(GmCommand::Spawn(SpawnCommand { mob: "test".to_string(), x: 0, y: 0, properties: HashMap::new() }).name(), "spawn");
        assert_eq!(GmCommand::Teleport(TeleportCommand { x: 0, y: 0 }).name(), "tp");
        assert_eq!(GmCommand::Reload(ReloadCommand { data_types: None }).name(), "reload");
        assert_eq!(GmCommand::RegenMap(RegenMapCommand { seed: None, generator: None }).name(), "regen-map");
        assert_eq!(GmCommand::LoadMap(LoadMapCommand { path: "test".to_string(), options: HashMap::new() }).name(), "load-map");
        assert_eq!(GmCommand::Custom(CustomCommand { cmd: "test".to_string(), arguments: HashMap::new() }).name(), "test");
    }
}
