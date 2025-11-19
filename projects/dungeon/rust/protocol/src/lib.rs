//! Dungeon Protocol
//!
//! This crate defines the shared communication protocol between the Pigeon Pea dungeon game
//! and development tools. It provides types for serialization/deserialization of JSON messages
//! exchanged over WebSocket connections.

use serde::{Deserialize, Serialize};
use thiserror::Error;
use uuid::Uuid;

pub mod commands;
pub mod events;

/// Current protocol version
pub const PROTOCOL_VERSION: u32 = 1;

/// Message envelope that wraps all protocol messages
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Envelope {
    /// Protocol version
    pub version: u32,
    /// Message type
    #[serde(rename = "type")]
    pub msg_type: MessageType,
    /// Unique message identifier
    pub id: String,
    /// For replies, echoes the original command's ID
    #[serde(rename = "correlation_id", skip_serializing_if = "Option::is_none")]
    pub correlation_id: Option<String>,
    /// Message-specific data
    pub payload: serde_json::Value,
}

/// Message types in the protocol
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum MessageType {
    /// Command from dev-tool to game
    GmCommand,
    /// Reply from game to dev-tool
    GmReply,
    /// Game state change event
    EventState,
    /// Game log event
    EventLog,
}

/// Standard reply structure for command responses
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Reply {
    /// Whether the command succeeded
    pub ok: bool,
    /// Optional response data
    #[serde(skip_serializing_if = "Option::is_none")]
    pub data: Option<serde_json::Value>,
    /// Optional error information
    #[serde(skip_serializing_if = "Option::is_none")]
    pub error: Option<ErrorInfo>,
}

/// Error information in replies
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct ErrorInfo {
    /// Error code
    pub code: ErrorCode,
    /// Human-readable error message
    pub message: String,
}

/// Error codes for protocol errors
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "snake_case")]
pub enum ErrorCode {
    /// Unknown command
    InvalidCommand,
    /// Invalid command arguments
    BadArgs,
    /// Server-side error
    InternalError,
    /// Missing/invalid authentication
    Unauthorized,
    /// Command execution timeout
    Timeout,
}

/// Authentication message
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct AuthMessage {
    /// Authentication token
    pub token: String,
}

impl Envelope {
    /// Create a new command envelope
    pub fn new_command(payload: serde_json::Value) -> Self {
        Self {
            version: PROTOCOL_VERSION,
            msg_type: MessageType::GmCommand,
            id: Uuid::new_v4().to_string(),
            correlation_id: None,
            payload,
        }
    }

    /// Create a new reply envelope
    pub fn new_reply(correlation_id: String, payload: Reply) -> Self {
        Self {
            version: PROTOCOL_VERSION,
            msg_type: MessageType::GmReply,
            id: Uuid::new_v4().to_string(),
            correlation_id: Some(correlation_id),
            payload: serde_json::to_value(payload).unwrap(),
        }
    }

    /// Create a new state event envelope
    pub fn new_state_event(payload: serde_json::Value) -> Self {
        Self {
            version: PROTOCOL_VERSION,
            msg_type: MessageType::EventState,
            id: Uuid::new_v4().to_string(),
            correlation_id: None,
            payload,
        }
    }

    /// Create a new log event envelope
    pub fn new_log_event(payload: serde_json::Value) -> Self {
        Self {
            version: PROTOCOL_VERSION,
            msg_type: MessageType::EventLog,
            id: Uuid::new_v4().to_string(),
            correlation_id: None,
            payload,
        }
    }

    /// Validate the envelope structure
    pub fn validate(&self) -> Result<(), ProtocolError> {
        if self.version != PROTOCOL_VERSION {
            return Err(ProtocolError::UnsupportedVersion(self.version));
        }

        if self.id.is_empty() {
            return Err(ProtocolError::InvalidEnvelope("Missing message ID".to_string()));
        }

        // Validate correlation ID for replies
        if self.msg_type == MessageType::GmReply && self.correlation_id.is_none() {
            return Err(ProtocolError::InvalidEnvelope(
                "Reply missing correlation_id".to_string(),
            ));
        }

        Ok(())
    }
}

/// Protocol-specific errors
#[derive(Debug, Error, Clone)]
pub enum ProtocolError {
    #[error("Unsupported protocol version: {0}")]
    UnsupportedVersion(u32),

    #[error("Invalid envelope: {0}")]
    InvalidEnvelope(String),

    #[error("Invalid message type: {0}")]
    InvalidMessageType(String),

    #[error("Serialization error: {0}")]
    SerializationError(String),

    #[error("Invalid payload: {0}")]
    InvalidPayload(String),
}

impl From<serde_json::Error> for ProtocolError {
    fn from(err: serde_json::Error) -> Self {
        ProtocolError::SerializationError(err.to_string())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use serde_json::json;

    #[test]
    fn test_create_command_envelope() {
        let payload = json!({
            "cmd": "spawn",
            "args": {"mob": "goblin", "x": 10, "y": 5}
        });

        let envelope = Envelope::new_command(payload.clone());

        assert_eq!(envelope.version, PROTOCOL_VERSION);
        assert_eq!(envelope.msg_type, MessageType::GmCommand);
        assert!(!envelope.id.is_empty());
        assert!(envelope.correlation_id.is_none());
        assert_eq!(envelope.payload, payload);
    }

    #[test]
    fn test_create_reply_envelope() {
        let correlation_id = "test-id".to_string();
        let reply = Reply {
            ok: true,
            data: Some(json!({"spawned": "goblin"})),
            error: None,
        };

        let envelope = Envelope::new_reply(correlation_id.clone(), reply);

        assert_eq!(envelope.version, PROTOCOL_VERSION);
        assert_eq!(envelope.msg_type, MessageType::GmReply);
        assert!(!envelope.id.is_empty());
        assert_eq!(envelope.correlation_id, Some(correlation_id));
    }

    #[test]
    fn test_validate_valid_envelope() {
        let envelope = Envelope::new_command(json!({"cmd": "test"}));
        assert!(envelope.validate().is_ok());
    }

    #[test]
    fn test_validate_invalid_version() {
        let mut envelope = Envelope::new_command(json!({"cmd": "test"}));
        envelope.version = 999;
        assert!(envelope.validate().is_err());
    }

    #[test]
    fn test_validate_reply_without_correlation() {
        let envelope = Envelope {
            version: PROTOCOL_VERSION,
            msg_type: MessageType::GmReply,
            id: "test".to_string(),
            correlation_id: None,
            payload: json!({"ok": true}),
        };
        assert!(envelope.validate().is_err());
    }

    #[test]
    fn test_serialize_deserialize() {
        let original = Envelope::new_command(json!({
            "cmd": "spawn",
            "args": {"mob": "goblin", "x": 10, "y": 5}
        }));

        let serialized = serde_json::to_string(&original).unwrap();
        let deserialized: Envelope = serde_json::from_str(&serialized).unwrap();

        assert_eq!(original, deserialized);
    }
}
