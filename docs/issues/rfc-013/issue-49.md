# Integration tests with mock WebSocket server

RFC Reference: [RFC-013: Yazi-integrated Rust CLI](../../rfcs/013-yazi-integrated-rust-cli.md)

## Summary

Add cross-platform integration tests validating envelopes and round-trips.

## Description

- Lightweight mock WS server used in tests.
- Cover: `spawn`, `tp`, `reload`, `regen-map`, and watch command start/stop.
- CI runs on Windows/Linux/macOS.

## Acceptance Criteria

- [ ] Mock server accepts connections and echoes `gm.reply`
- [ ] Tests cover success and error paths
- [ ] CI executes tests on all platforms (Windows, Linux, macOS) with same matrix used in packaging

## Dependencies

- Depends on: #175, #176, #177, #48
- Blocks: #180

## Files to Create

- `tools/dev-tool/tests/ws_integration.rs`
- `tools/dev-tool/Cargo.toml` (dev-deps)
