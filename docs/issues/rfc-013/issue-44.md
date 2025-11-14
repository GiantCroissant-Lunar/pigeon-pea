# Scaffold dev-tool crate (Foundation)

RFC Reference: [RFC-013: Yazi-integrated Rust CLI](../../rfcs/013-yazi-integrated-rust-cli.md)

## Summary
Create a Rust CLI at `tools/dev-tool` with basic CLI parsing and global options.

## Description
- Initialize new Rust crate using:
  - `clap` (CLI parsing)
  - `tokio` (async runtime)
  - `serde`, `serde_json` (serialization)
- Add global flags:
  - `--server` (default from env `DEV_TOOL_SERVER` → `ws://127.0.0.1:5007/gm`)
  - `--token` (from env `DEV_TOOL_TOKEN`)
  - `--output text|json` (default `text`)
  - `--timeout <ms>` (default `5000`)
  - Provide helpful `--help` with stubs for planned subcommands.
  - Optional: support configuration file at `%APPDATA%/dev-tool/config.toml` (Windows) or `~/.config/dev-tool/config.toml` (Unix). Precedence: CLI flags > ENV (`DEV_TOOL_*`) > config file.

## Acceptance Criteria
- [ ] Compiles on Windows, macOS, Linux
- [ ] `dev-tool --help` lists global options and subcommand stubs
- [ ] Env defaults (`DEV_TOOL_SERVER`, `DEV_TOOL_TOKEN`) honored when flags not provided
- [ ] Verbose/log output prints effective configuration
- [ ] (Optional) Configuration file parsed when present; precedence respected

## Dependencies
- Depends on: None
- Blocks: #175

## Files to Create
- `tools/dev-tool/Cargo.toml`
- `tools/dev-tool/src/main.rs`
- `tools/dev-tool/README.md`
