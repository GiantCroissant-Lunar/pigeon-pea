# Core commands: spawn, tp, reload, regen-map

RFC Reference: [RFC-013: Yazi-integrated Rust CLI](../../rfcs/013-yazi-integrated-rust-cli.md)

## Summary

Implement one-shot GM commands with proper envelopes and exit codes.

## Description

- Commands:
  - `spawn --mob <name> --x <int> --y <int>` → `{ cmd: "spawn", args: { mob, x, y } }`
  - `tp --x <int> --y <int>` → `{ cmd: "tp", args: { x, y } }`
  - `reload` → `{ cmd: "reload" }`
  - `regen-map [--seed <int>]` → `{ cmd: "regen-map", args: { seed? } }`
- Respect `--output text|json` for replies.
- Exit codes: 0 on success; non-zero on error.

## Acceptance Criteria

- [ ] Argument validation with friendly errors
- [ ] Correct envelopes; success and error handling
- [ ] Consistent exit codes (align with #178 "Exit Codes" table)
- [ ] Errors printed to stderr; successful replies printed to stdout

## Dependencies

- Depends on: #175
- Blocks: #179

## Files to Create/Modify

- `tools/dev-tool/src/cli.rs`
- `tools/dev-tool/src/commands/spawn.rs`
- `tools/dev-tool/src/commands/tp.rs`
- `tools/dev-tool/src/commands/reload.rs`
- `tools/dev-tool/src/commands/regen_map.rs`
