//! Event types for dungeon protocol
//!
//! This module defines all event types that can be sent from the game server
//! to dev-tool, including state changes and log events.

use serde::{Deserialize, Serialize};
use std::collections::HashMap;

/// All game events that can be streamed to dev-tool
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(tag = "event", rename_all = "snake_case")]
pub enum GameEvent {
    /// Entity component state change
    EntityState(EntityStateEvent),
    /// Player movement
    PlayerMove(PlayerMoveEvent),
    /// Combat event
    Combat(CombatEvent),
    /// Map generation complete
    MapGenerated(MapGeneratedEvent),
    /// Game state change (pause, resume, etc.)
    GameStateChange(GameStateChangeEvent),
    /// Error occurred
    Error(ErrorEvent),
}

/// Entity component state change
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct EntityStateEvent {
    /// Timestamp of event
    pub timestamp: String,
    /// Entity identifier
    pub entity: String,
    /// Component name
    pub component: String,
    /// New value
    pub value: serde_json::Value,
    /// Previous value (optional)
    #[serde(skip_serializing_if = "Option::is_none")]
    pub previous: Option<serde_json::Value>,
}

/// Player movement event
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct PlayerMoveEvent {
    /// Timestamp of event
    pub timestamp: String,
    /// Player identifier
    pub player: String,
    /// From position
    pub from: Position,
    /// To position
    pub to: Position,
    /// Movement type (walk, teleport, etc.)
    #[serde(default)]
    pub movement_type: MovementType,
}

/// Combat event
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct CombatEvent {
    /// Timestamp of event
    pub timestamp: String,
    /// Event type
    #[serde(rename = "type")]
    pub event_type: CombatEventType,
    /// Attacker identifier
    pub attacker: String,
    /// Target identifier
    pub target: String,
    /// Damage dealt (optional)
    #[serde(skip_serializing_if = "Option::is_none")]
    pub damage: Option<i32>,
    /// Weapon used (optional)
    #[serde(skip_serializing_if = "Option::is_none")]
    pub weapon: Option<String>,
    /// Additional event data
    #[serde(skip_serializing_if = "HashMap::is_empty", default)]
    pub data: HashMap<String, serde_json::Value>,
}

/// Map generation complete event
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct MapGeneratedEvent {
    /// Timestamp of event
    pub timestamp: String,
    /// Map identifier
    pub map_id: String,
    /// Map dimensions
    pub dimensions: Dimensions,
    /// Seed used for generation
    pub seed: u64,
    /// Generator type
    pub generator: String,
    /// Generation statistics
    #[serde(skip_serializing_if = "HashMap::is_empty", default)]
    pub stats: HashMap<String, serde_json::Value>,
}

/// Game state change event
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct GameStateChangeEvent {
    /// Timestamp of event
    pub timestamp: String,
    /// Previous state
    #[serde(skip_serializing_if = "Option::is_none")]
    pub previous: Option<GameState>,
    /// New state
    pub current: GameState,
    /// Reason for change (optional)
    #[serde(skip_serializing_if = "Option::is_none")]
    pub reason: Option<String>,
}

/// Error event
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ErrorEvent {
    /// Timestamp of event
    pub timestamp: String,
    /// Error severity
    pub severity: ErrorSeverity,
    /// Error code
    pub code: String,
    /// Error message
    pub message: String,
    /// Stack trace (optional, debug builds only)
    #[serde(skip_serializing_if = "Option::is_none")]
    pub stack_trace: Option<String>,
    /// Context data
    #[serde(skip_serializing_if = "HashMap::is_empty", default)]
    pub context: HashMap<String, serde_json::Value>,
}

/// Log event data (for event.log messages)
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct LogEvent {
    /// Timestamp of log entry
    pub timestamp: String,
    /// Log level
    pub level: LogLevel,
    /// Log message
    pub message: String,
    /// Source/module name
    #[serde(skip_serializing_if = "Option::is_none")]
    pub source: Option<String>,
    /// Additional context data
    #[serde(skip_serializing_if = "HashMap::is_empty", default)]
    pub context: HashMap<String, serde_json::Value>,
}

/// Position coordinates
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Position {
    /// X coordinate
    pub x: i32,
    /// Y coordinate
    pub y: i32,
    /// Z coordinate (optional, for 3D)
    #[serde(skip_serializing_if = "Option::is_none")]
    pub z: Option<i32>,
}

/// Map dimensions
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Dimensions {
    /// Width
    pub width: u32,
    /// Height
    pub height: u32,
    /// Depth (optional, for 3D)
    #[serde(skip_serializing_if = "Option::is_none")]
    pub depth: Option<u32>,
}

/// Movement types
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum MovementType {
    /// Normal walking
    Walk,
    /// Running
    Run,
    /// Teleportation
    Teleport,
    /// Flying
    Fly,
    /// Swimming
    Swim,
}

impl Default for MovementType {
    fn default() -> Self {
        MovementType::Walk
    }
}

/// Combat event types
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum CombatEventType {
    /// Attack initiated
    Attack,
    /// Hit successful
    Hit,
    /// Attack missed
    Miss,
    /// Critical hit
    Critical,
    /// Entity defeated
    Defeat,
    /// Spell cast
    Spell,
    /// Effect applied
    Effect,
}

/// Game states
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum GameState {
    /// Game is running normally
    Running,
    /// Game is paused
    Paused,
    /// Game is in menu
    Menu,
    /// Game is loading
    Loading,
    /// Game is stopped
    Stopped,
    /// Game is in debug mode
    Debug,
}

/// Error severity levels
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum ErrorSeverity {
    /// Informational error
    Info,
    /// Warning
    Warning,
    /// Error
    Error,
    /// Critical error
    Critical,
}

/// Log levels
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum LogLevel {
    /// Debug information
    Debug,
    /// Informational message
    Info,
    /// Warning message
    Warn,
    /// Error message
    Error,
}

impl GameEvent {
    /// Convert to protocol payload format
    pub fn to_payload(&self) -> serde_json::Value {
        serde_json::to_value(self).unwrap()
    }

    /// Get event timestamp
    pub fn timestamp(&self) -> &str {
        match self {
            GameEvent::EntityState(e) => &e.timestamp,
            GameEvent::PlayerMove(e) => &e.timestamp,
            GameEvent::Combat(e) => &e.timestamp,
            GameEvent::MapGenerated(e) => &e.timestamp,
            GameEvent::GameStateChange(e) => &e.timestamp,
            GameEvent::Error(e) => &e.timestamp,
        }
    }
}

impl LogEvent {
    /// Convert to protocol payload format
    pub fn to_payload(&self) -> serde_json::Value {
        serde_json::to_value(self).unwrap()
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use serde_json::json;

    #[test]
    fn test_entity_state_event_serialization() {
        let event = GameEvent::EntityState(EntityStateEvent {
            timestamp: "2025-01-15T10:30:00Z".to_string(),
            entity: "player".to_string(),
            component: "health".to_string(),
            value: json!(100),
            previous: Some(json!(80)),
        });

        let payload = event.to_payload();
        assert_eq!(payload["event"], "entity_state");
        assert_eq!(payload["entity"], "player");
        assert_eq!(payload["component"], "health");
        assert_eq!(payload["value"], 100);
        assert_eq!(payload["previous"], 80);
    }

    #[test]
    fn test_player_move_event_serialization() {
        let event = GameEvent::PlayerMove(PlayerMoveEvent {
            timestamp: "2025-01-15T10:30:00Z".to_string(),
            player: "player".to_string(),
            from: Position { x: 10, y: 5, z: None },
            to: Position { x: 11, y: 5, z: None },
            movement_type: MovementType::Walk,
        });

        let payload = event.to_payload();
        assert_eq!(payload["event"], "player_move");
        assert_eq!(payload["player"], "player");
        assert_eq!(payload["from"]["x"], 10);
        assert_eq!(payload["from"]["y"], 5);
        assert_eq!(payload["to"]["x"], 11);
        assert_eq!(payload["to"]["y"], 5);
        assert_eq!(payload["movement_type"], "walk");
    }

    #[test]
    fn test_log_event_serialization() {
        let event = LogEvent {
            timestamp: "2025-01-15T10:30:00Z".to_string(),
            level: LogLevel::Info,
            message: "Player moved north".to_string(),
            source: Some("game.movement".to_string()),
            context: HashMap::new(),
        };

        let payload = event.to_payload();
        assert_eq!(payload["level"], "info");
        assert_eq!(payload["message"], "Player moved north");
        assert_eq!(payload["source"], "game.movement");
    }

    #[test]
    fn test_combat_event_serialization() {
        let event = GameEvent::Combat(CombatEvent {
            timestamp: "2025-01-15T10:30:00Z".to_string(),
            event_type: CombatEventType::Hit,
            attacker: "goblin".to_string(),
            target: "player".to_string(),
            damage: Some(15),
            weapon: Some("rusty_sword".to_string()),
            data: HashMap::new(),
        });

        let payload = event.to_payload();
        assert_eq!(payload["event"], "combat");
        assert_eq!(payload["type"], "hit");
        assert_eq!(payload["attacker"], "goblin");
        assert_eq!(payload["target"], "player");
        assert_eq!(payload["damage"], 15);
        assert_eq!(payload["weapon"], "rusty_sword");
    }

    #[test]
    fn test_event_timestamp_access() {
        let timestamp = "2025-01-15T10:30:00Z".to_string();
        let event = GameEvent::EntityState(EntityStateEvent {
            timestamp: timestamp.clone(),
            entity: "player".to_string(),
            component: "position".to_string(),
            value: json!({"x": 10, "y": 5}),
            previous: None,
        });

        assert_eq!(event.timestamp(), timestamp);
    }
}
