# Issue 10: Add Replay/Recording System to DevTools

Allow recording command sequences and replaying them for testing/demonstration.

## Acceptance Criteria

- [ ] New commands: `record start <name>`, `record stop`, `replay <name>`
- [ ] Recording format: JSON array of commands + timestamps
- [ ] Replay supports speed control (1x, 2x, 5x, 10x)
- [ ] Can save/load recordings from disk

## Example Usage

```bash
# Record a test scenario
pp-dev record start boss-fight-test
pp-dev spawn goblin 10 5
pp-dev spawn goblin 15 5
pp-dev spawn goblin 20 5
pp-dev record stop

# Replay it later
pp-dev replay boss-fight-test --speed 2x
```

## Location
`dotnet/dev-tools/core/PigeonPea.DevTools/Recording/`

## Labels
- `enhancement`
- `dev-tools`
- `testing`

## Milestone
DevTools v2.0
