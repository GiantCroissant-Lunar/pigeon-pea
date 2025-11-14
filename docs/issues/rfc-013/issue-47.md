# Streaming: state --watch, log --watch

RFC Reference: [RFC-013: Yazi-integrated Rust CLI](../../rfcs/013-yazi-integrated-rust-cli.md)

## Summary
Add long-running watch commands to subscribe to events.

## Description
- `state --watch [--filter <expr>]` prints `event.state` stream.
- `log --watch [--level <L>]` prints `event.log` with level filter (Info/Warn/Error).
- Output formats: text or line-delimited JSON (for `--output json`).
- Clean shutdown on Ctrl+C.

## Acceptance Criteria
- [ ] Subscribes and prints events continuously
- [ ] Filter switches respected; clean shutdown on SIGINT
- [ ] Output format switch works (text to stdout, json as line-delimited to stdout; errors to stderr)

## Dependencies
- Depends on: #175
- Blocks: #48

## Files to Create
- `tools/dev-tool/src/commands/state.rs`
- `tools/dev-tool/src/commands/log.rs`
