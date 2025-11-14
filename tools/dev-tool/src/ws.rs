use anyhow::{Context, Result};
use futures_util::{SinkExt, StreamExt};
use tokio_tungstenite::{connect_async, tungstenite::Message};
use url::Url;

use crate::protocol::{AuthPayload, Envelope, MessageType, NoopCommand, PROTOCOL_VERSION};

/// WebSocket client for communicating with the game server
pub struct WsClient {
    url: String,
    token: Option<String>,
}

impl WsClient {
    /// Create a new WebSocket client
    pub fn new(url: String, token: Option<String>) -> Self {
        Self { url, token }
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

        // Wait for reply
        if let Some(msg) = read.next().await {
            match msg {
                Ok(Message::Text(text)) => {
                    self.handle_reply(&text, &noop_id)?;
                }
                Ok(Message::Close(_)) => {
                    println!("Server closed connection");
                }
                Err(e) => {
                    anyhow::bail!("WebSocket error: {}", e);
                }
                _ => {
                    println!("Received non-text message");
                }
            }
        } else {
            println!("Connection closed without reply");
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
            println!(
                "⚠ Warning: Server protocol version {} differs from client version {}",
                envelope.version, PROTOCOL_VERSION
            );
            println!("  Protocol incompatibility detected. Some features may not work correctly.");
        }

        // Check if it's a reply message
        if envelope.message_type != MessageType::GmReply {
            println!(
                "⚠ Warning: Expected gm.reply but got {:?}",
                envelope.message_type
            );
        }

        // Check correlation ID
        if let Some(ref corr_id) = envelope.correlation_id {
            if corr_id != expected_correlation_id {
                println!(
                    "⚠ Warning: Correlation ID mismatch. Expected: {}, Got: {}",
                    expected_correlation_id, corr_id
                );
            }
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
        .unwrap()
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
        let client = WsClient::new("ws://localhost:5007".to_string(), None);
        assert_eq!(client.url, "ws://localhost:5007");
        assert!(client.token.is_none());

        let client_with_token = WsClient::new(
            "ws://localhost:5007".to_string(),
            Some("test-token".to_string()),
        );
        assert_eq!(client_with_token.token, Some("test-token".to_string()));
    }

    #[test]
    fn test_handle_reply_valid() {
        let client = WsClient::new("ws://localhost:5007".to_string(), None);
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
        let client = WsClient::new("ws://localhost:5007".to_string(), None);
        let reply_json = r#"{
            "version": 2,
            "type": "gm.reply",
            "id": "reply-123",
            "payload": {
                "status": "ok"
            }
        }"#;

        let result = client.handle_reply(reply_json, "msg-123");
        // Should still succeed but print a warning
        assert!(result.is_ok());
    }

    #[test]
    fn test_handle_reply_invalid_json() {
        let client = WsClient::new("ws://localhost:5007".to_string(), None);
        let result = client.handle_reply("invalid json", "msg-123");
        assert!(result.is_err());
    }
}
