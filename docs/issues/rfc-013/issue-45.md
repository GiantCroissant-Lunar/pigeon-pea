# WebSocket client and JSON envelope types

RFC Reference: [RFC-013: Yazi-integrated Rust CLI](../../rfcs/013-yazi-integrated-rust-cli.md)

## Summary

Add `tokio-tungstenite` WS client and a versioned JSON envelope to communicate with the game.

## Description

- Connect to `--server` URL (default `ws://127.0.0.1:5007/gm`).
- Define `Envelope<T>`:
  - `version` (start: 1)
  - `type` (`gm.command|gm.reply|event.state|event.log`)
  - `id`, `correlationId`
  - `payload`
- If token provided, include in first message payload (e.g., `{ auth: { token } }`).
- Implement a `noop` gm.command and print `gm.reply`.

## Acceptance Criteria

- [ ] Stable connection with clear error on failure
- [ ] Sends `version: 1` and `type: gm.command`; receives `gm.reply`
- [ ] Includes token when set

## Dependencies

- Depends on: #174
- Blocks: #176, #177, #179

## Protocol Versioning Strategy

- Start with `version: 1` in all envelopes
- Backwards compatible minor changes do not bump major
- Breaking changes bump major and support a server negotiation step
- Client prints clear error when server version is incompatible

## Files to Create

- `tools/dev-tool/src/ws.rs`
- `tools/dev-tool/src/protocol.rs`
