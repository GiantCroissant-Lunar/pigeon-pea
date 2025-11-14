# Issue 2: Add Event Broadcasting to DevTools

Implement event broadcasting so connected clients can subscribe to game events (entity moved, entity died, player damaged, etc.) in real-time.

## References

- RFC-013: DevTools System Architecture (Future Extensions)
- RFC-014: DevTools Protocol Specification (Event Specifications)

## Acceptance Criteria

- [ ] Event types: `EntityMovedEvent`, `EntityDiedEvent`, `PlayerDamagedEvent`, `MapChangedEvent`
- [ ] `BroadcastEventAsync()` method
- [ ] Hook into existing game events (MessagePipe publishers)
- [ ] Clients can subscribe/unsubscribe to specific event types (future)
- [ ] Update Rust CLI to display events in REPL
- [ ] Protocol documentation updated

## Implementation Notes

```csharp
// In GameWorld.cs
private void OnPlayerMoved(Point oldPos, Point newPos)
{
    _devToolsServer?.BroadcastEventAsync(new EntityMovedEvent
    {
        EntityId = PlayerEntity.Id,
        FromX = oldPos.X,
        FromY = oldPos.Y,
        ToX = newPos.X,
        ToY = newPos.Y
    });
}
```

## Labels

- `enhancement`
- `dev-tools`
- `real-time`

## Milestone

DevTools v1.1
