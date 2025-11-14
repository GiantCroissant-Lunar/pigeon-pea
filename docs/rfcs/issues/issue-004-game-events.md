---
doc_id: 'PLAN-2025-00006'
title: 'Issue #4: [RFC-006] Phase 2: Integrate game events with plugin system'
doc_type: 'plan'
status: 'active'
canonical: true
created: '2025-11-10'
tags: ['issue', 'plugin-system', 'rfc-006', 'phase-2', 'game-logic']
summary: 'Integrate game events with plugin system as defined in RFC-006 Phase 2'
supersedes: []
related: ['RFC-2025-00006', 'PLAN-2025-00001', 'PLAN-2025-00005']
---

# Issue #4: [RFC-006] Phase 2: Integrate game events with plugin system

**Labels:** `plugin-system`, `rfc-006`, `phase-2`, `game-logic`

## Related RFC

RFC-006 Phase 2: Plugin System Architecture

## Summary

Define game events in contracts and integrate EventBus into GameWorld to publish events for plugins.

## Depends On

Issue #3 (plugin system must exist)

## Scope

- Define game event contracts
- Integrate `IEventBus` into `GameWorld`
- Publish events from game actions
- Create example plugin that subscribes to events
- Verify events flow correctly

## Acceptance Criteria

### Game Events Defined

- [ ] Game events defined in `PigeonPea.Game.Contracts/Events/`:
  - [ ] `EntitySpawnedEvent.cs`
  - [ ] `EntityMovedEvent.cs`
  - [ ] `CombatEvent.cs`
  - [ ] Additional events as needed (damage, heal, pickup, etc.)
- [ ] Events are immutable records
- [ ] Events contain all relevant data (no database/state lookups needed)

### GameWorld Integration

- [ ] `GameWorld.cs` updated:
  - [ ] Constructor accepts `IEventBus` parameter
  - [ ] `SpawnEntity()` publishes `EntitySpawnedEvent`
  - [ ] `MoveEntity()` publishes `EntityMovedEvent`
  - [ ] Combat actions publish `CombatEvent`
  - [ ] Events published after state changes (not before)

### Example Plugin

- [ ] Example plugin created: `PigeonPea.Plugins.EventLogger`
  - [ ] Located in `game-essential/plugins/PigeonPea.Plugins.EventLogger/`
  - [ ] Subscribes to all game events
  - [ ] Logs events to console/logger
  - [ ] Has `plugin.json` manifest
  - [ ] Can be enabled/disabled via config

### Application Integration

- [ ] Both console and Windows apps updated:
  - [ ] Add `IEventBus` to dependency injection
  - [ ] Pass `IEventBus` to `GameWorld` constructor
  - [ ] Verify events are published during gameplay

### Testing

- [ ] Integration test verifies event flow:
  - [ ] GameWorld publishes event
  - [ ] EventLogger plugin receives event
  - [ ] Event data is correct
  - [ ] Multiple subscribers work correctly
- [ ] No functional regressions

### Documentation

- [ ] XML documentation for all event classes
- [ ] README in EventLogger plugin explaining purpose
- [ ] Updated ARCHITECTURE.md with event flow diagram

## Implementation Notes

- Use C# records for immutable events
- Consider synchronous vs asynchronous event publishing (perf implications)
- Ensure events are published _after_ state changes (not before)

## Event Examples

### EntitySpawnedEvent

```csharp
namespace PigeonPea.Game.Contracts.Events;

public record EntitySpawnedEvent
{
    public required int EntityId { get; init; }
    public required string EntityType { get; init; }
    public required Point Position { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

### EntityMovedEvent

```csharp
public record EntityMovedEvent
{
    public required int EntityId { get; init; }
    public required Point OldPosition { get; init; }
    public required Point NewPosition { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

### CombatEvent

```csharp
public record CombatEvent
{
    public required int AttackerId { get; init; }
    public required int TargetId { get; init; }
    public required int DamageDealt { get; init; }
    public required bool IsHit { get; init; }
    public required bool IsKill { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

## GameWorld Integration Example

```csharp
    public class GameWorld
    {
        private readonly IEventBus _eventBus;

        public GameWorld(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task SpawnEntityAsync(Entity entity, CancellationToken ct = default)
        {
            // ... ECS entity creation ...

            // Publish event for plugins
            await _eventBus.PublishAsync(
                new EntitySpawnedEvent
                {
                    EntityId = entity.Id,
                    EntityType = GetEntityType(entity),
                    Position = entity.Get<Position>().Point
                },
                ct);
        }

        public async Task MoveEntityAsync(Entity entity, Point newPosition, CancellationToken ct = default)
        {
            var oldPosition = entity.Get<Position>().Point;
            entity.Get<Position>().Point = newPosition;

            // Publish event
            await _eventBus.PublishAsync(
                new EntityMovedEvent
                {
                    EntityId = entity.Id,
                    OldPosition = oldPosition,
                    NewPosition = newPosition
                },
                ct);
        }
    }
```

## EventLogger Plugin Example

```csharp
public class EventLoggerPlugin : IPlugin
{
    private ILogger? _logger;
    private IEventBus? _eventBus;

    public string Id => "event-logger";
    public string Name => "Event Logger";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        _logger = context.Logger;
        _eventBus = context.Registry.Get<IEventBus>();

        // Subscribe to all events
        _eventBus.Subscribe<EntitySpawnedEvent>(OnEntitySpawned);
        _eventBus.Subscribe<EntityMovedEvent>(OnEntityMoved);
        _eventBus.Subscribe<CombatEvent>(OnCombat);

        _logger.LogInformation("Event logger initialized");
        return Task.CompletedTask;
    }

    private Task OnEntitySpawned(EntitySpawnedEvent evt)
    {
        _logger?.LogInformation(
            "Entity spawned: {Type} at ({X},{Y})",
            evt.EntityType, evt.Position.X, evt.Position.Y);
        return Task.CompletedTask;
    }

    private Task OnEntityMoved(EntityMovedEvent evt)
    {
        _logger?.LogInformation(
            "Entity moved: {Id} from ({OX},{OY}) to ({NX},{NY})",
            evt.EntityId, evt.OldPosition.X, evt.OldPosition.Y, evt.NewPosition.X, evt.NewPosition.Y);
        return Task.CompletedTask;
    }

    private Task OnCombat(CombatEvent evt)
    {
        _logger?.LogInformation(
            "Combat: {A} -> {T} dmg={D} hit={H} kill={K}",
            evt.AttackerId, evt.TargetId, evt.DamageDealt, evt.IsHit, evt.IsKill);
        return Task.CompletedTask;
    }
}
```

## Dependencies

- Issue #3 must be completed (plugin system must exist)

## See Also

- [RFC-006: Plugin System Architecture](../006-plugin-system-architecture.md)
- [PLUGIN_SYSTEM_ANALYSIS.md](../../PLUGIN_SYSTEM_ANALYSIS.md)
