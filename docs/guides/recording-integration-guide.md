---
canonical: true
created: '2025-11-22'
doc_id: GUIDE-00003
doc_type: guide
status: active
title: Recording Service Integration Guide
summary: How to integrate game systems with the recording service for deterministic replay
tags:
  - recording
  - integration
  - events
  - replay
related: ['RFC-00052', 'RFC-00053']
---

# Recording Service Integration Guide

## Responsibility Model

### Key Principle

> **The recording service is PASSIVE** - it only provides infrastructure.
> **Game systems are ACTIVE** - they decide what events to record.

This is a critical design decision: the recorder doesn't know about your game logic, so it can't decide what's important. Each service/system must call `RecordEvent()` when something noteworthy happens.

##Diagram

```
Game Systems (Decide WHAT)          Recording Service (Provides HOW)
┌────────────────────────┐          ┌──────────────────────────┐
│ Input Service          │──────────│ Event Recorder           │
│ - HandleKeyPress()     │ Record   │ - RecordEvent()          │
│                        │          │ - GetEvents()            │
├────────────────────────┤          │ - SaveAsync()            │
│ Map Generation         │──────────│                          │
│ - GenerateMap()        │ Record   │                          │
│                        │          │                          │
├────────────────────────┤          │                          │
│ ECS World              │──────────│                          │
│ - OnEntityCreated      │ Record   │                          │
│                        │          │                          │
├────────────────────────┤          │                          │
│ AI System              │──────────│                          │
│ - MakeDecision()       │ Record   │                          │
└────────────────────────┘          └──────────────────────────┘
```

## Integration Examples

### 1. Input Service

```csharp
public class InputService
{
    private readonly IEventRecorder _recorder;

    public void HandleKeyPress(KeyPress key)
    {
        // Input service DECIDES to record player input
        if (_recorder.IsRecording)
            _recorder.RecordEvent(new GameEvent("KeyPress", "Input", new { key }));

        // Then process the input
        ProcessInput(key);
    }
}
```

### 2. Map Generation Service

```csharp
public class MapGenerationService
{
    private readonly IEventRecorder _recorder;

    public MapData GenerateMap(int seed, BiomeType biome)
    {
        // DECIDE to record generation start
        _recorder.RecordEvent(new GameEvent("MapGenStart", "MapGen",
            new { seed, biome }));

        var map = Generate(seed, biome);

        // DECIDE to record generation complete
        _recorder.RecordEvent(new GameEvent("MapGenComplete", "MapGen",
            new { tiles = map.TileCount, rivers = map.RiverCount }));

        return map;
    }
}
```

### 3. ECS World

```csharp
public class EcsWorld
{
    private readonly IEventRecorder _recorder;

    public void OnEntityCreated(Entity entity)
    {
        // ECS DECIDES to record entity creation
        if (_recorder.IsRecording)
        {
            _recorder.RecordEvent(new GameEvent("EntityCreate", "ECS",
                new { entity.Id, components = entity.Components.Select(c => c.GetType().Name) }));
        }
    }

    public void OnComponentAdded(Entity entity, IComponent component)
    {
        // ECS DECIDES to record component addition
        if (_recorder.IsRecording)
        {
            _recorder.RecordEvent(new GameEvent("ComponentAdd", "ECS",
                new { entity.Id, component = component.GetType().Name }));
        }
    }
}
```

### 4. AI Decision System

```csharp
public class AIDecisionSystem
{
    private readonly IEventRecorder _recorder;

    public AIDecision MakeDecision(Entity entity, GameState state)
    {
        var decision = Evaluate(entity, state);

        // AI system DECIDES to record decisions for debugging
        _recorder.RecordEvent(new GameEvent("AIDecision", "AI", new
        {
            entity = entity.Id,
            decision = decision.Type,
            reason = decision.Reason,
            score = decision.Score
        }));

        return decision;
    }
}
```

## Helper Extensions (Optional)

To make integration cleaner, you can create helper extensions:

```csharp
public static class RecordingExtensions
{
    public static void RecordInput(this IEventRecorder recorder, string inputType, object data)
    {
        if (recorder.IsRecording)
            recorder.RecordEvent(new GameEvent(inputType, "Input", data));
    }

    public static void RecordEntity(this IEventRecorder recorder, string eventType, Entity entity)
    {
        if (recorder.IsRecording)
            recorder.RecordEvent(new GameEvent(eventType, "ECS", new { entity.Id }));
    }

    public static void RecordAI(this IEventRecorder recorder, string decision, object reasoning)
    {
        if (recorder.IsRecording)
            recorder.RecordEvent(new GameEvent(decision, "AI", reasoning));
    }
}

// Usage becomes cleaner:
public class InputService
{
    public void HandleInput(KeyPress key)
    {
        _recorder.RecordInput("KeyPress", new { key });
        ProcessInput(key);
    }
}
```

## What Should You Record?

Good events to record:

- ✅ **Player actions**: Keypresses, mouse clicks, menu selections
- ✅ **State changes**: Map generated, entity spawned, level up
- ✅ **Important decisions**: AI choices, pathfinding results
- ✅ **External inputs**: RNG seed, loaded data, network messages

Events to avoid recording:

- ❌ **Every frame update**: Too much data, not useful
- ❌ **UI rendering**: Not part of game logic
- ❌ **Mouse movement**: Too granular unless crucial

## Integration Checklist

For each game system, ask:

1. **Does this system make important decisions?** → Record them
2. **Does this system process user input?** → Record the inputs
3. **Does this system change game state?** → Record the changes
4. **Would I need to know this happened to replay the game?** → Record it

---

**Summary**: The recording service is a tool. Your game systems use the tool by calling `RecordEvent()` when important things happen. The recorder doesn't know what's important - only your systems do.
