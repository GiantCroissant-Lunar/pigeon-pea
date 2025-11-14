# Resilience & configuration (reconnect/backoff, timeouts, exit codes)

RFC Reference: [RFC-013: Yazi-integrated Rust CLI](../../rfcs/013-yazi-integrated-rust-cli.md)

## Summary

Improve robustness and user experience for long-running sessions and one-shots.

## Description

- Add exponential backoff with jitter for reconnects in watch commands.
- Add per-command timeouts for one-shot commands.
- Define and document standardized exit codes and improved error messages.
- Add logging configuration and levels, with environment variable and CLI flag.
- Add security notes for token handling and masking.

## Acceptance Criteria

- [ ] Backoff with jitter for reconnects
- [ ] Per-command timeout enforced
- [ ] Exit codes documented and consistent (see table below)
- [ ] `--log-level` flag (trace|debug|info|warn|error) and `DEV_TOOL_LOG` env var
- [ ] Sensitive values (token) never printed; logs redact secrets
- [ ] Errors printed to stderr; informational output to stdout

## Exit Codes

| Code | Meaning                       |
| ---- | ----------------------------- |
| 0    | Success                       |
| 1    | General error                 |
| 2    | Invalid arguments             |
| 3    | Connection failure            |
| 4    | Timeout                       |
| 5    | Authentication failure        |
| 6    | Incompatible protocol version |
| 7    | Configuration error           |

## Logging Configuration

- Flag: `--log-level <level>` (trace|debug|info|warn|error), default `info`
- Env: `DEV_TOOL_LOG` mirrors the flag when flag absent
- Implementation may use `tracing` + `tracing-subscriber`

## Security Best Practices

- Prefer tokens via env var `DEV_TOOL_TOKEN`; avoid writing tokens to disk
- Mask tokens in logs and error messages
- (Optional) Future: OS keychain integration for token storage

## Dependencies

- Depends on: #175, #177
- Blocks: #179, #180

## Files to Modify

- `tools/dev-tool/src/ws.rs`
- `tools/dev-tool/src/main.rs`
