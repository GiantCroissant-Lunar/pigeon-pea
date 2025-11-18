use anyhow::Result;
use dungeon_protocol::{Envelope, MessageType, PROTOCOL_VERSION};
use futures_util::{SinkExt, StreamExt};
use std::{
    net::SocketAddr,
    sync::{Arc, Mutex},
};
use tokio::{
    net::{TcpListener, TcpStream},
    task::JoinHandle,
};
use tokio_tungstenite::{
    accept_async,
    tungstenite::protocol::Message,
    WebSocketStream,
};
use tracing::{debug, error, info, warn};
use uuid::Uuid;

use crate::app::{AppState, ClientConnection};

pub struct WebSocketServer {
    bind_address: String,
    auth_token: String,
}

impl WebSocketServer {
    pub fn new(bind_address: String, auth_token: String) -> Self {
        Self {
            bind_address,
            auth_token,
        }
    }

    pub async fn start(
        &self,
        app_state: Arc<Mutex<AppState>>,
    ) -> Result<JoinHandle<()>> {
        let bind_addr = self.bind_address.clone();
        let auth_token = self.auth_token.clone();

        // Create TCP listener
        let listener = TcpListener::bind(&bind_addr).await?;
        info!("🚀 WebSocket server listening on: {}", bind_addr);

        let handle = tokio::spawn(async move {
            while let Ok((stream, addr)) = listener.accept().await {
                debug!("New TCP connection from: {}", addr);

                // Upgrade to WebSocket
                match accept_async(stream).await {
                    Ok(ws_stream) => {
                        info!("WebSocket connection established from: {}", addr);

                        // Spawn handler for this client
                        let app_state = app_state.clone();
                        let auth_token = auth_token.clone();

                        tokio::spawn(async move {
                            Self::handle_client(
                                ws_stream,
                                addr,
                                app_state,
                                auth_token,
                            ).await;
                        });
                    }
                    Err(e) => {
                        warn!("Failed to upgrade WebSocket connection from {}: {}", addr, e);
                    }
                }
            }
        });

        Ok(handle)
    }

    async fn handle_client(
        ws_stream: WebSocketStream<TcpStream>,
        addr: SocketAddr,
        app_state: Arc<Mutex<AppState>>,
        auth_token: String,
    ) {
        let client_id = Uuid::new_v4().to_string();
        let client = ClientConnection::new(addr);

        // Split WebSocket stream
        let (mut ws_sender, mut ws_receiver) = ws_stream.split();

        // Send welcome message
        let welcome_msg = Envelope {
            version: PROTOCOL_VERSION,
            msg_type: MessageType::GmCommand,
            id: Uuid::new_v4().to_string(),
            correlation_id: None,
            payload: serde_json::json!({
                "cmd": "welcome",
                "server_version": "0.1.0",
                "client_id": client_id.clone(),
                "auth_required": true
            }),
        };

        if let Err(e) = ws_sender.send(Message::Text(
            serde_json::to_string(&welcome_msg).unwrap_or_default()
        )).await {
            error!("Failed to send welcome message: {}", e);
            return;
        }

        // Wait for authentication
        if let Err(_) = Self::handle_authentication(
            &mut ws_receiver,
            &mut ws_sender,
            &auth_token,
        ).await {
            warn!("Authentication failed for client: {}", client_id);
            let _ = ws_sender.send(Message::Close(None)).await;
            return;
        }

        // Add client to state
        {
            let mut state = app_state.lock().unwrap();
            state.add_client(client_id.clone(), client.clone());
            drop(state);
        }

        // Handle client messages
        let mut ping_interval = tokio::time::interval(tokio::time::Duration::from_secs(30));

        loop {
            tokio::select! {
                // Handle incoming messages
                Some(msg_result) = ws_receiver.next() => {
                    match msg_result {
                        Ok(msg) => {
                            if let Err(e) = Self::handle_websocket_message(
                                msg,
                                &client_id,
                                &mut ws_sender,
                                &app_state,
                            ).await {
                                error!("Error handling WebSocket message: {}", e);
                            }
                        }
                        Err(e) => {
                            error!("WebSocket error: {}", e);
                            break;
                        }
                    }
                }

                // Send periodic ping
                _ = ping_interval.tick() => {
                    let ping_msg = Envelope {
                        version: PROTOCOL_VERSION,
                        msg_type: MessageType::GmCommand,
                        id: Uuid::new_v4().to_string(),
                        correlation_id: None,
                        payload: serde_json::json!({"cmd": "ping"}),
                    };

                    if let Err(e) = ws_sender.send(Message::Text(
                        serde_json::to_string(&ping_msg).unwrap_or_default()
                    )).await {
                        error!("Failed to send ping: {}", e);
                    }
                }
            }
        }

        // Client disconnected
        info!("Client {} disconnected", client_id);

        // Remove from state
        {
            let mut state = app_state.lock().unwrap();
            state.remove_client(&client_id);
            drop(state);
        }
    }

    async fn handle_authentication(
        ws_receiver: &mut futures_util::stream::SplitStream<WebSocketStream<TcpStream>>,
        ws_sender: &mut futures_util::stream::SplitSink<WebSocketStream<TcpStream>, Message>,
        auth_token: &str,
    ) -> Result<()> {
        // Wait for auth message with timeout
        let timeout = tokio::time::sleep(tokio::time::Duration::from_secs(10));

        tokio::select! {
            Some(msg_result) = ws_receiver.next() => {
                match msg_result? {
                    Message::Text(text) => {
                        if let Ok(envelope) = serde_json::from_str::<Envelope>(&text) {
                            if let Some(payload) = envelope.payload.get("auth") {
                                if let Some(token) = payload.get("token") {
                                    if let Some(token_str) = token.as_str() {
                                        if token_str == auth_token {
                                            // Send auth success
                                            let auth_reply = Envelope {
                                                version: PROTOCOL_VERSION,
                                                msg_type: MessageType::GmReply,
                                                id: Uuid::new_v4().to_string(),
                                                correlation_id: Some(envelope.id),
                                                payload: serde_json::json!({
                                                    "status": "success",
                                                    "message": "Authentication successful"
                                                }),
                                            };

                                            ws_sender.send(Message::Text(
                                                serde_json::to_string(&auth_reply).unwrap_or_default()
                                            )).await?;

                                            return Ok(());
                                        }
                                    }
                                }
                            }
                        }

                        // Auth failed
                        let auth_reply = Envelope {
                            version: PROTOCOL_VERSION,
                            msg_type: MessageType::GmReply,
                            id: Uuid::new_v4().to_string(),
                            correlation_id: None,
                            payload: serde_json::json!({
                                "status": "error",
                                "message": "Invalid authentication token"
                            }),
                        };

                        ws_sender.send(Message::Text(
                            serde_json::to_string(&auth_reply).unwrap_or_default()
                        )).await?;
                    }
                    _ => {}
                }
            }
            _ = timeout => {
                // Auth timeout
                let auth_reply = Envelope {
                    version: PROTOCOL_VERSION,
                    msg_type: MessageType::GmReply,
                    id: Uuid::new_v4().to_string(),
                    correlation_id: None,
                    payload: serde_json::json!({
                        "status": "error",
                        "message": "Authentication timeout"
                    }),
                };

                ws_sender.send(Message::Text(
                    serde_json::to_string(&auth_reply).unwrap_or_default()
                )).await?;
            }
        }

        Err(anyhow::anyhow!("Authentication failed"))
    }

    async fn handle_websocket_message(
        msg: Message,
        client_id: &str,
        ws_sender: &mut futures_util::stream::SplitSink<WebSocketStream<TcpStream>, Message>,
        app_state: &Arc<Mutex<AppState>>,
    ) -> Result<()> {
        match msg {
            Message::Text(text) => {
                debug!("Received text message from {}: {}", client_id, text);

                // Parse envelope
                if let Ok(envelope) = serde_json::from_str::<Envelope>(&text) {
                    // Handle specific message types
                    match envelope.msg_type {
                        MessageType::EventState => {
                            // Update client state
                            if let Some(state_update) = envelope.payload.get("state") {
                                if let Some(player_data) = state_update.get("player") {
                                    if let (Some(x), Some(y)) = (
                                        player_data.get("x").and_then(|v| v.as_i64()),
                                        player_data.get("y").and_then(|v| v.as_i64())
                                    ) {
                                        let mut state = app_state.lock().unwrap();
                                        if let Some(client) = state.clients.get_mut(client_id) {
                                            client.player_pos = (x as i32, y as i32);
                                            client.last_ping = chrono::Utc::now();
                                        }
                                        drop(state);
                                    }
                                }
                            }
                        }
                        MessageType::GmCommand => {
                            // Echo GM command reply
                            let reply = Envelope {
                                version: PROTOCOL_VERSION,
                                msg_type: MessageType::GmReply,
                                id: Uuid::new_v4().to_string(),
                                correlation_id: Some(envelope.id),
                                payload: serde_json::json!({
                                    "status": "received",
                                    "message": "Command received by server"
                                }),
                            };

                            ws_sender.send(Message::Text(
                                serde_json::to_string(&reply).unwrap_or_default()
                            )).await?;
                        }
                        _ => {
                            debug!("Unhandled message type: {:?}", envelope.msg_type);
                        }
                    }
                } else {
                    warn!("Invalid JSON message from {}: {}", client_id, text);
                }
            }
            Message::Binary(data) => {
                debug!("Received binary message from {}: {} bytes", client_id, data.len());
            }
            Message::Ping(_) => {
                debug!("Received ping from {}", client_id);
            }
            Message::Pong(_) => {
                debug!("Received pong from {}", client_id);
            }
            Message::Close(_) => {
                info!("Client {} sent close message", client_id);
            }
            Message::Frame(_) => {
                // Raw frame - usually handled internally by tungstenite
                debug!("Received raw frame from {}", client_id);
            }
        }

        Ok(())
    }

    #[allow(dead_code)]
    pub async fn broadcast_to_all_clients(
        app_state: &Arc<Mutex<AppState>>,
        _message: Envelope,
    ) -> Result<()> {
        let client_ids: Vec<String> = {
            let state = app_state.lock().unwrap();
            state.get_client_ids()
        };

        // TODO: Implement actual broadcasting to WebSocket connections
        // This would require storing WebSocket senders in the client state

        info!("Broadcasting message to {} clients", client_ids.len());
        for client_id in &client_ids {
            debug!("Broadcasting to client: {}", client_id);
        }

        Ok(())
    }

    #[allow(dead_code)]
    pub async fn send_to_client(
        _app_state: &Arc<Mutex<AppState>>,
        _client_id: &str,
        _message: Envelope,
    ) -> Result<()> {
        // TODO: Implement actual sending to specific WebSocket connection
        // This would require storing WebSocket senders in the client state

        info!("Sending message to client");
        debug!("Message received");

        Ok(())
    }
}
