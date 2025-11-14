use anyhow::{Context, Result};
use futures_util::{SinkExt, StreamExt};
use std::time::Duration;
use tokio_tungstenite::{connect_async, tungstenite::Message};
use url::Url;

use crate::protocol::{AuthPayload, Envelope, MessageType, NoopCommand, PROTOCOL_VERSION};

/// WebSocket client for communicating with the game server
pub struct WsClient {
    url: String,
    token: Option<String>,
    timeout: Duration,
}

impl WsClient {
    /// Create a new WebSocket client
    pub fn new(url: String, token: Option<String>, timeout_ms: u64) -> Self {
        Self {
            url,
            token,
            timeout: Duration::from_millis(timeout_ms),
        }
    }

    /// Connect to the WebSocket server and perform initial handshake
    pub async fn connect(&self) -> Result<()> {
        let url = Url::parse(&self.url)
            .with_context(|| format!("Invalid WebSocket URL: {}", self.url))?;

        println!("Connecting to: {}", self.url);

        let (ws_stream, _) = connect_async(url).await.context(
            "Failed to connect to WebSocket server. Ensure the server is running and accessible.",
        )?;

        println!("✓ Connected successfully");

        let (mut write, mut read) = ws_stream.split();

        // Send authentication if token is provided
        if let Some(ref token) = self.token {
            let auth_payload = AuthPayload::new(token.clone());
            let auth_envelope = Envelope::new(MessageType::GmCommand, generate_id(), auth_payload);

            let auth_json = serde_json::to_string(&auth_envelope)
                .context("Failed to serialize authentication message")?;

            write
                .send(Message::Text(auth_json))
                .await
                .context("Failed to send authentication message")?;

            println!("✓ Authentication token sent");
        }

        // Send noop command
        let noop_payload = NoopCommand::new();
        let noop_id = generate_id();
        let noop_envelope = Envelope::new(MessageType::GmCommand, noop_id.clone(), noop_payload);

        let noop_json =
            serde_json::to_string(&noop_envelope).context("Failed to serialize noop command")?;

        write
            .send(Message::Text(noop_json))
            .await
            .context("Failed to send noop command")?;

        println!("✓ Sent noop command (id: {})", noop_id);

        // Wait for reply with timeout
        let reply_result = tokio::time::timeout(self.timeout, read.next()).await;

        match reply_result {
            Ok(Some(msg)) => match msg {
                Ok(Message::Text(text)) => {
                    self.handle_reply(&text, &noop_id)?;
                }
                Ok(Message::Close(_)) => {
                    anyhow::bail!("Server closed connection before sending reply");
                }
                Err(e) => {
                    anyhow::bail!("WebSocket error: {}", e);
                }
                _ => {
                    anyhow::bail!("Received non-text message when expecting reply");
                }
            },
            Ok(None) => {
                anyhow::bail!("Connection closed without reply");
            }
            Err(_) => {
                anyhow::bail!(
                    "Timeout waiting for server reply ({}ms)",
                    self.timeout.as_millis()
                );
            }
        }

        Ok(())
    }

    /// Handle a reply message from the server
    fn handle_reply(&self, text: &str, expected_correlation_id: &str) -> Result<()> {
        // Try to parse as a generic envelope first
        let envelope: Envelope<serde_json::Value> =
            serde_json::from_str(text).context("Failed to parse reply envelope")?;

        // Check protocol version compatibility
        if envelope.version != PROTOCOL_VERSION {
            anyhow::bail!(
                "Protocol version mismatch: server version {} differs from client version {}. Protocol incompatibility detected.",
                envelope.version, PROTOCOL_VERSION
            );
        }

        // Check if it's a reply message
        if envelope.message_type != MessageType::GmReply {
            anyhow::bail!("Expected gm.reply but got {:?}", envelope.message_type);
        }

        // Check correlation ID
        match &envelope.correlation_id {
            Some(corr_id) if corr_id != expected_correlation_id => {
                anyhow::bail!(
                    "Correlation ID mismatch. Expected: {}, Got: {}",
                    expected_correlation_id,
                    corr_id
                );
            }
            None => {
                anyhow::bail!(
                    "Missing correlation ID in reply. Expected: {}",
                    expected_correlation_id
                );
            }
            _ => {}
        }

        // Print the reply
        println!("\n=== Received gm.reply ===");
        println!("ID: {}", envelope.id);
        if let Some(corr_id) = envelope.correlation_id {
            println!("Correlation ID: {}", corr_id);
        }
        println!("Version: {}", envelope.version);
        println!("Payload:");
        println!("{}", serde_json::to_string_pretty(&envelope.payload)?);
        println!("========================\n");

        Ok(())
    }
}

/// Generate a unique ID for messages
fn generate_id() -> String {
    use std::time::{SystemTime, UNIX_EPOCH};
    let timestamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap_or_default()
        .as_millis();
    format!("msg-{}", timestamp)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_generate_id() {
        let id1 = generate_id();
        let id2 = generate_id();

        assert!(id1.starts_with("msg-"));
        assert!(id2.starts_with("msg-"));
        // IDs should be different (assuming test runs fast enough)
        // but we can't guarantee this in all cases, so just check format
    }

    #[test]
    fn test_ws_client_creation() {
        let client = WsClient::new("ws://localhost:5007".to_string(), None, 5000);
        assert_eq!(client.url, "ws://localhost:5007");
        assert!(client.token.is_none());
        assert_eq!(client.timeout.as_millis(), 5000);

        let client_with_token = WsClient::new(
            "ws://localhost:5007".to_string(),
            Some("test-token".to_string()),
            10000,
        );
        assert_eq!(client_with_token.token, Some("test-token".to_string()));
        assert_eq!(client_with_token.timeout.as_millis(), 10000);
    }

    #[test]
    fn test_handle_reply_valid() {
        let client = WsClient::new("ws://localhost:5007".to_string(), None, 5000);
        let reply_json = r#"{
            "version": 1,
            "type": "gm.reply",
            "id": "reply-123",
            "correlationId": "msg-123",
            "payload": {
                "status": "ok",
                "message": "Command executed successfully"
            }
        }"#;

        let result = client.handle_reply(reply_json, "msg-123");
        assert!(result.is_ok());
    }

    #[test]
    fn test_handle_reply_version_mismatch() {
        let client = WsClient::new("ws://localhost:5007".to_string(), None, 5000);
        let reply_json = r#"{
            "version": 2,
            "type": "gm.reply",
            "id": "reply-123",
            "payload": {
                "status": "ok"
            }
        }"#;

        let result = client.handle_reply(reply_json, "msg-123");
        // Now should fail due to version mismatch
        assert!(result.is_err());
    }

    #[test]
    fn test_handle_reply_invalid_json() {
        let client = WsClient::new("ws://localhost:5007".to_string(), None, 5000);
        let result = client.handle_reply("invalid json", "msg-123");
        assert!(result.is_err());
    }
}
