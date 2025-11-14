# Issue 5: Add Map Editor Commands to DevTools

Extend DevTools with map editing capabilities (place tiles, paint terrain, save/load maps).

## References

- RFC-013: DevTools System Architecture (Future Extensions)

## Acceptance Criteria

- [ ] New commands: `set-tile`, `fill-rect`, `save-map`, `load-map`, `regen-map`
- [ ] Protocol updated with new command DTOs
- [ ] Rust CLI updated with new commands
- [ ] Map serialization format defined (JSON)

## Example Usage

```bash
pp-dev set-tile 10 5 wall
pp-dev fill-rect 0 0 10 10 floor
pp-dev save-map custom-map-1.json
pp-dev load-map custom-map-1.json
pp-dev regen-map 12345
```

## Labels

- `enhancement`
- `dev-tools`
- `map-editor`

## Milestone

DevTools v2.0
