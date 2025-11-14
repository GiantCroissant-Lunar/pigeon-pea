# Yazi integration doc + packaging pipeline

RFC Reference: [RFC-013: Yazi-integrated Rust CLI](../../rfcs/013-yazi-integrated-rust-cli.md)

## Summary

Document Yazi actions/integration and add CI to build/publish `dev-tool` binaries.

## Description

- Write `docs/integrations/yazi-dev-tool.md` with at least 3 actionable examples.
- Clarify invocation mechanism: Yazi runs shell commands to call `dev-tool` with the currently selected file or directory (e.g., via key bindings/actions that expand placeholders like current path).
- Provide example key bindings and actions that call:
  - Reload after saving a map
  - Load-map using selected path
  - Spawn-from-file
- Add GitHub Actions workflow to build for Windows/Linux/macOS and publish artifacts with checksums.
- (Optional) Generate man pages and shell completions as part of releases.

## Acceptance Criteria

- [ ] Doc validated on Windows with Rio terminal
- [ ] CI builds for: `x86_64-pc-windows-msvc`, `x86_64-unknown-linux-gnu`, `aarch64-unknown-linux-gnu`, `aarch64-apple-darwin`, `x86_64-apple-darwin`
- [ ] Artifacts uploaded per OS; release notes template added
- [ ] Doc includes concrete Yazi action and key binding examples that call `dev-tool`
- [ ] `man` pages generated using `clap_mangen` (or similar) and attached to releases
- [ ] Shell completions generated (bash, zsh, fish, powershell) and attached to releases

## Files to Create

- `docs/integrations/yazi-dev-tool.md`
- `.github/workflows/ci-dev-tool.yml`

## Dependencies

- Depends on: #176, #177, #48, #49
