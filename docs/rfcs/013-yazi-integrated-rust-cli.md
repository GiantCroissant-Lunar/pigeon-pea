# RFC-013: Yazi-integrated Rust GM CLI

## Status

**Status**: Proposed
**Created**: 2025-11-13
**Author**: Development Team

## Summary

Define and implement a Rust command-line tool that integrates with Yazi-based workflows to control and inspect the running game via a lightweight IPC protocol. The CLI focuses on GM (game master) commands and editor-side actions, enabling a smooth "Play / Edit / GM" workflow across one or multiple machines. Communication with the game uses a local-first transport (WebSocket), with optional HTTP fallback.

Deliverables:

- A Rust CLI (`dev-tool`) that connects to the game’s control endpoint
- A small, versioned JSON envelope for commands and events
- Guidance for integrating the CLI with Yazi (invoke external commands on selected files/items)
- Minimal security (dev token), connection ergonomics, and clear failure modes

## Motivation

- Unify GM and editor actions in terminal-first workflows (Rio/WezTerm/Yazi)
- Keep tooling fast, portable, and scriptable (Rust CLI with single static binary)
- Allow remote control (across machines) while staying simple for local dev
- Leverage Yazi as an effective “asset and task browser” that can trigger CLI actions

## Design

### Components

- CLI process (`dev-tool`):
  - Connects to game via WebSocket (default: `ws://127.0.0.1:5007/gm`)
  - Sends commands; optionally subscribes to events
  - Prints replies and selected events to stdout (line-delimited JSON or human-readable)
  - Supports non-interactive (one-shot) and interactive modes

- Message protocol (JSON, versioned):
  - Envelope fields: `version`, `type`, `id`, `correlationId`, `payload`
  - Types: `gm.command`, `gm.reply`, `event.state`, `event.log`
  - Example command: `{ "type": "gm.command", "payload": { "cmd": "spawn", "args": { "mob": "goblin", "x": 10, "y": 5 } } }`

- Yazi integration surface:
  - Yazi invokes external commands for the selected file(s) or current directory
  - Map to `dev-tool` commands (e.g., reload, load-map, spawn-from-file, teleport-to-file-marker)
  - Provide example command lines that Yazi can bind to hotkeys or menus

- Transport and security:
  - Default transport: WebSocket
  - Optional HTTP fallback for environments without WS
  - Dev token via environment variable or first message (simple header-equivalent)
  - Local-only by default (bind to loopback); LAN opt-in

### CLI UX

- Subcommands (initial set):
  - `spawn --mob <name> --x <int> --y <int>`
  - `tp --x <int> --y <int>`
  - `regen-map [--seed <int>]`
  - `reload` (signals the game to reload world data from disk)
  - `state [--watch] [--filter <expr>]` (subscribe to state events)
  - `log --watch [--level <L>]` (stream log events)
  - `send --json '<json>'` (escape hatch for power users)

- Global options:
  - `--server <ws-url>` (default `ws://127.0.0.1:5007/gm`)
  - `--token <str>` or `DEV_TOOL_TOKEN`
  - `--output json|text` (default `text`)
  - `--timeout <ms>`

- Examples:
  - `dev-tool spawn --mob goblin --x 10 --y 5`
  - `dev-tool tp --x 20 --y 3`
  - `DEV_TOOL_TOKEN=dev dev-tool state --watch --output json | jq '.payload.player'`

### Yazi Usage Examples (conceptual)

- Run `dev-tool reload` after saving a map file in the editor
- Run `dev-tool spawn --mob skeleton --x 5 --y 9` from a Yazi action
- Run `dev-tool send --json '{"type":"gm.command","payload":{"cmd":"load-map","args":{"path":"%CURRENT%"}}}'` to load a selected file path

Note: Bindings and plugin syntax vary by Yazi version. We will provide a short guide describing how to run external commands from Yazi and pass the selected file path(s) to `dev-tool`.

### Protocol Details

- Envelope:
  - `version`: integer; start at 1
  - `type`: string; one of `gm.command`, `gm.reply`, `event.state`, `event.log`
  - `id`: client-generated unique id (for commands)
  - `correlationId`: echo of `id` in replies
  - `payload`: command args, reply data, or event data

- Error replies:
  - `gm.reply` with `{ ok: false, error: { code, message } }`

- Heartbeats:
  - Server pings or periodic `event.state` lightweight updates; client auto-reconnect

### Dependencies (Rust)

- `clap` (CLI parsing)
- `tokio` (async runtime)
- `tokio-tungstenite` (WebSocket client)
- `serde`, `serde_json` (serialization)
- `anyhow` or `thiserror` (error handling)
- `tracing` + `tracing-subscriber` (optional logging)

### Configuration

- Environment variables:
  - `DEV_TOOL_SERVER` (e.g., `ws://127.0.0.1:5007/gm`)
  - `DEV_TOOL_TOKEN` (shared secret for dev)

- Command-line flags override env vars.

## Implementation Plan

- Phase 1: Skeleton & connectivity
  - Create Rust crate `tools/dev-tool`
  - Connect to server; send a no-op command; print reply
  - Implement `--server`, `--token`, `--output`

- Phase 2: Core commands
  - Implement `spawn`, `tp`, `reload`, `regen-map`
  - Implement `state --watch` and `log --watch`
  - Graceful reconnects and timeouts

- Phase 3: Yazi integration notes
  - Add `docs/integrations/yazi-dev-tool.md` with examples for invoking `dev-tool` from Yazi
  - Provide sample command lines for file-context actions

- Phase 4: Packaging & release
  - Cross-platform builds (Windows/macOS/Linux)
  - GitHub Actions to publish binaries (dev channel)

## Testing Strategy

- Unit tests for CLI argument parsing
- Integration tests with a mock WS server validating JSON envelopes
- E2E manual test against a local dev game instance
- Snapshot tests for text output (normalized)

## Alternatives Considered

- HTTP-only control (simpler, but no streaming)
- Named pipes / Unix sockets (great locally, not cross-machine)
- Yazi plugin in Lua calling HTTP directly (possible, but CLI offers broader reuse)

## Risks and Mitigations

- Protocol churn → version field; backwards-compat policy
- Security in LAN mode → dev-only token; bind to loopback by default; env-guard for 0.0.0.0
- Tool sprawl → keep CLI cohesive; document usage in docs

## Success Criteria

- Can spawn, teleport, reload from CLI on local dev machine
- Can stream state/log events and filter to useful subsets
- Can invoke `dev-tool` from Yazi to operate on selected files (reload/load-map)

## Timeline

- Week 1: Phase 1 + Phase 2 (core commands)
- Week 2: Phase 3 + Phase 4 (Yazi guide + packaging)

## Open Questions

- Final name for the CLI: `dev-tool`
- Do we want an interactive TUI mode inside the CLI as a later enhancement?
- Which subset of events are most useful to watch by default?

## References

- Project RFC directory and architecture docs
- Yazi documentation (external command invocation, keymaps)
- WebSocket client libraries (tokio-tungstenite)
