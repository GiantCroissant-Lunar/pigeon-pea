# Issue 4: Create Web Dashboard Client for DevTools

Build a browser-based DevTools client with xterm.js for game visualization and controls.

## References

- RFC-013: DevTools System Architecture (Future Extensions)

## Acceptance Criteria

- [ ] React/Vue/Svelte app with WebSocket client
- [ ] xterm.js terminal showing game output (optional)
- [ ] Control panel with buttons for common commands
- [ ] Entity list view (from `query` command)
- [ ] Real-time event log (entity moved, died, etc.)
- [ ] Map visualization (ASCII/Braille rendering)
- [ ] Hosted at `http://localhost:5008` during development

## Tech Stack

- Frontend: React + TypeScript
- WebSocket: native WebSocket API
- Terminal: xterm.js
- Build: Vite

## Location

`dotnet/dev-tools/clients/web-dashboard/`

## Labels

- `enhancement`
- `dev-tools`
- `web`
- `good first issue`

## Milestone

DevTools v2.0
