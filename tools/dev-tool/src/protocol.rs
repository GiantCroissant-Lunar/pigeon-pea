use serde::{Deserialize, Serialize};

/// Protocol version - start with 1
pub const PROTOCOL_VERSION: u32 = 1;

/// Message types in the protocol
#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum MessageType {
    #[serde(rename = "gm.command")]
    GmCommand,
    #[serde(rename = "gm.reply")]
    GmReply,
    #[serde(rename = "event.state")]
    EventState,
    #[serde(rename = "event.log")]
    EventLog,
}

/// Versioned JSON envelope for communication
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Envelope<T> {
    /// Protocol version
    pub version: u32,

    /// Message type
    #[serde(rename = "type")]
    pub message_type: MessageType,

    /// Unique message identifier
    pub id: String,

    /// Correlation ID for request-response pairing
    #[serde(rename = "correlationId", skip_serializing_if = "Option::is_none")]
    pub correlation_id: Option<String>,

    /// Message payload
    pub payload: T,
}

impl<T> Envelope<T> {
    /// Create a new envelope with the current protocol version
    pub fn new(message_type: MessageType, id: String, payload: T) -> Self {
        Self {
            version: PROTOCOL_VERSION,
            message_type,
            id,
            correlation_id: None,
            payload,
        }
    }

    /// Create a new envelope with correlation ID
    #[allow(dead_code)]
    pub fn new_with_correlation(
        message_type: MessageType,
        id: String,
        correlation_id: Option<String>,
        payload: T,
    ) -> Self {
        Self {
            version: PROTOCOL_VERSION,
            message_type,
            id,
            correlation_id,
            payload,
        }
    }
}

/// Authentication payload
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AuthPayload {
    pub auth: AuthInfo,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AuthInfo {
    pub token: String,
}

impl AuthPayload {
    pub fn new(token: String) -> Self {
        Self {
            auth: AuthInfo { token },
        }
    }
}

/// Noop command payload
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct NoopCommand {
    pub command: String,
}

impl NoopCommand {
    pub fn new() -> Self {
        Self {
            command: "noop".to_string(),
        }
    }
}

impl Default for NoopCommand {
    fn default() -> Self {
        Self::new()
    }
}

/// Generic reply payload
#[allow(dead_code)]
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ReplyPayload {
    pub status: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub message: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub data: Option<serde_json::Value>,
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_envelope_serialization() {
        let payload = NoopCommand::new();
        let envelope = Envelope::new(MessageType::GmCommand, "test-id".to_string(), payload);

        let json = serde_json::to_string(&envelope).unwrap();
        assert!(json.contains("\"version\":1"));
        assert!(json.contains("\"type\":\"gm.command\""));
        assert!(json.contains("\"id\":\"test-id\""));
        assert!(json.contains("\"command\":\"noop\""));
    }

    #[test]
    fn test_envelope_deserialization() {
        let json = r#"{
            "version": 1,
            "type": "gm.reply",
            "id": "reply-1",
            "payload": {
                "status": "ok",
                "message": "Success"
            }
        }"#;

        let envelope: Envelope<ReplyPayload> = serde_json::from_str(json).unwrap();
        assert_eq!(envelope.version, 1);
        assert_eq!(envelope.message_type, MessageType::GmReply);
        assert_eq!(envelope.id, "reply-1");
        assert_eq!(envelope.payload.status, "ok");
        assert_eq!(envelope.payload.message, Some("Success".to_string()));
    }

    #[test]
    fn test_envelope_with_correlation_id() {
        let payload = NoopCommand::new();
        let envelope = Envelope::new_with_correlation(
            MessageType::GmCommand,
            "test-id".to_string(),
            Some("corr-id".to_string()),
            payload,
        );

        let json = serde_json::to_string(&envelope).unwrap();
        assert!(json.contains("\"correlationId\":\"corr-id\""));
    }

    #[test]
    fn test_auth_payload() {
        let auth = AuthPayload::new("secret-token".to_string());
        let json = serde_json::to_string(&auth).unwrap();
        assert!(json.contains("\"auth\""));
        assert!(json.contains("\"token\":\"secret-token\""));
    }

    #[test]
    fn test_message_type_serialization() {
        assert_eq!(
            serde_json::to_string(&MessageType::GmCommand).unwrap(),
            "\"gm.command\""
        );
        assert_eq!(
            serde_json::to_string(&MessageType::GmReply).unwrap(),
            "\"gm.reply\""
        );
        assert_eq!(
            serde_json::to_string(&MessageType::EventState).unwrap(),
            "\"event.state\""
        );
        assert_eq!(
            serde_json::to_string(&MessageType::EventLog).unwrap(),
            "\"event.log\""
        );
    }

    #[test]
    fn test_noop_command_default() {
        let noop = NoopCommand::default();
        assert_eq!(noop.command, "noop");
    }
}
