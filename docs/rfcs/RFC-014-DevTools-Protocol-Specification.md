# RFC-014: DevTools Protocol Specification

Status: Implemented
Date: 2025-11-13
Author: Claude (Anthropic)

## Summary

Define the JSON-based WebSocket protocol for PigeonPea DevTools, including message formats, command types, event types, error handling, and versioning strategy.

## Motivation

A well-defined protocol is critical for:
- **Multiple client implementations** - Rust CLI, Python scripts, web dashboards, etc.
- **Forward compatibility** - Adding new commands without breaking existing clients
- **Error handling** - Clear error messages for debugging
- **Documentation** - Clients know exactly what to send/expect

## Goals

1. **Simple and predictable** - JSON-based, human-readable
2. **Extensible** - Easy to add new commands without breaking changes
3. **Type-safe** - Clear DTOs for all message types
4. **Self-documenting** - Messages include enough context to understand them
5. **Error-friendly** - All operations return success/failure with messages

## Non-Goals

1. **Binary protocols** - JSON is sufficient for dev tools (not performance-critical)
2. **GraphQL/gRPC** - Over-engineered for this use case
3. **Backwards compatibility** - Dev tools can version-lock with game

## Protocol Overview

### Transport Layer

- **Protocol:** WebSocket (RFC 6455)
- **Endpoint:** `ws://127.0.0.1:5007/` (configurable port)
- **Message Format:** UTF-8 JSON text frames
- **Encoding:** UTF-8

### Message Types

All messages are JSON objects with a `type` field:

| Type | Direction | Purpose |
|------|-----------|---------|
| `command` | Client → Server | Execute a command on the game |
| `event` | Server → Client | Result of command or game event |

## Message Formats

### 1. Command Message (Client → Server)

**Base Format:**
```json
{
  "type": "command",
  "cmd": "spawn",
  "args": {
    // Command-specific arguments
  }
}
```

**Fields:**
- `type` (string, required): Always `"command"`
- `cmd` (string, required): Command name (lowercase, kebab-case)
- `args` (object, optional): Command-specific arguments

**Example:**
```json
{
  "type": "command",
  "cmd": "spawn",
  "args": {
    "entity": "goblin",
    "x": 10,
    "y": 5
  }
}
```

### 2. Event Message (Server → Client)

**Base Format:**
```json
{
  "type": "event",
  "event": "command_result",
  "timestamp": 1700000000000,
  "success": true,
  "message": "Operation completed",
  "result": {
    // Event-specific data
  }
}
```

**Fields:**
- `type` (string, required): Always `"event"`
- `event` (string, required): Event name (snake_case)
- `timestamp` (number, required): Unix timestamp in milliseconds
- `success` (boolean, optional): For `command_result` events
- `message` (string, optional): Human-readable message
- `result` (object, optional): Event-specific data

**Example:**
```json
{
  "type": "event",
  "event": "command_result",
  "timestamp": 1700000000123,
  "success": true,
  "message": "Spawned goblin at (10, 5)",
  "result": {
    "entityId": 42,
    "x": 10,
    "y": 5
  }
}
```

## Command Specifications

### spawn

**Purpose:** Spawn an entity at a specific position

**Arguments:**
```json
{
  "entity": "goblin",  // Entity type (string)
  "x": 10,             // X coordinate (integer)
  "y": 5               // Y coordinate (integer)
}
```

**Supported Entity Types:**
- `"goblin"` or `"enemy"` - Spawns a goblin enemy
- `"potion"` or `"health_potion"` - Spawns a health potion

**Success Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": true,
  "message": "Spawned goblin at (10, 5)",
  "result": {
    "entityId": 42,
    "x": 10,
    "y": 5
  }
}
```

**Error Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "Position out of bounds: (10, 5)"
}
```

**Validation:**
- Position must be within map bounds
- Position must be walkable
- Entity type must be recognized

---

### tp / teleport

**Purpose:** Teleport the player to a specific position

**Arguments:**
```json
{
  "x": 20,       // X coordinate (integer)
  "y": 10,       // Y coordinate (integer)
  "entity": null // Optional: target entity (null = player)
}
```

**Success Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": true,
  "message": "Teleported player from (5, 5) to (20, 10)"
}
```

**Error Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "Position out of bounds: (20, 10)"
}
```

**Validation:**
- Position must be within map bounds
- Player must be alive

---

### query

**Purpose:** Query all entities in the game world

**Arguments:** None (or optional filter in future)

**Success Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": true,
  "message": "Found 153 entities",
  "result": {
    "entities": [
      {
        "id": 1,
        "x": 10,
        "y": 5,
        "glyph": "@",
        "type": "player",
        "health": "100/100"
      },
      {
        "id": 42,
        "x": 15,
        "y": 8,
        "glyph": "g",
        "type": "enemy",
        "health": "20/20"
      },
      {
        "id": 100,
        "x": 20,
        "y": 10,
        "glyph": "!",
        "type": "item",
        "name": "Health Potion"
      },
      {
        "id": 200,
        "x": 5,
        "y": 5,
        "glyph": ".",
        "type": "floor"
      }
    ]
  }
}
```

**Entity Types:**
- `"player"` - Player character (has `health` field)
- `"enemy"` - Enemy NPCs (has `health` field)
- `"item"` - Items on ground (has `name` field)
- `"floor"` / `"wall"` - Terrain tiles

---

### give

**Purpose:** Give an item to the player

**Arguments:**
```json
{
  "item": "potion",   // Item type (string)
  "quantity": 1       // Optional: quantity (default 1)
}
```

**Supported Item Types:**
- `"potion"` or `"health_potion"` - Health potion

**Success Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": true,
  "message": "Gave potion to player"
}
```

**Error Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "Player inventory is full"
}
```

**Validation:**
- Player must be alive
- Player must have inventory space
- Item type must be recognized

---

### heal

**Purpose:** Heal the player

**Arguments:**
```json
{
  "amount": 100,      // Heal amount (integer)
  "entity": null      // Optional: target entity (null = player)
}
```

**Success Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": true,
  "message": "Healed player for 20 HP (80 → 100)"
}
```

**Error Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "Player is not alive"
}
```

**Validation:**
- Player must be alive
- Healing capped at maximum health

---

### kill

**Purpose:** Kill enemies

**Arguments:**
```json
{
  "entity": "nearest"  // Target: "nearest", "all", "enemies"
}
```

**Target Options:**
- `"nearest"` (default) - Kill nearest enemy to player
- `"all"` or `"enemies"` - Kill all living enemies

**Success Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": true,
  "message": "Killed nearest enemy at distance 5.2"
}
```

**Error Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "No living enemies found"
}
```

---

### ping

**Purpose:** Test connection to server

**Arguments:** None

**Success Response:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": true,
  "message": "pong"
}
```

## Event Specifications

### command_result

**Purpose:** Result of a command execution

**Fields:**
- `success` (boolean, required): Whether command succeeded
- `message` (string, required): Human-readable result message
- `result` (object, optional): Command-specific result data

**Example Success:**
```json
{
  "type": "event",
  "event": "command_result",
  "timestamp": 1700000000123,
  "success": true,
  "message": "Operation completed",
  "result": { "entityId": 42 }
}
```

**Example Failure:**
```json
{
  "type": "event",
  "event": "command_result",
  "timestamp": 1700000000123,
  "success": false,
  "message": "Invalid arguments: missing 'x' field"
}
```

### entity_created (Future)

**Purpose:** Broadcast when an entity is created

**Fields:**
```json
{
  "type": "event",
  "event": "entity_created",
  "timestamp": 1700000000123,
  "entityId": 42,
  "entityType": "enemy",
  "x": 10,
  "y": 5
}
```

### entity_moved (Future)

**Purpose:** Broadcast when an entity moves

**Fields:**
```json
{
  "type": "event",
  "event": "entity_moved",
  "timestamp": 1700000000123,
  "entityId": 42,
  "fromX": 10,
  "fromY": 5,
  "toX": 11,
  "toY": 5
}
```

### entity_died (Future)

**Purpose:** Broadcast when an entity dies

**Fields:**
```json
{
  "type": "event",
  "event": "entity_died",
  "timestamp": 1700000000123,
  "entityId": 42,
  "entityType": "enemy"
}
```

## Error Handling

### Protocol Errors

**Invalid JSON:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "Invalid JSON: Unexpected token at position 10"
}
```

**Missing Required Field:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "Invalid command format: missing 'cmd' field"
}
```

**Unknown Command:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "Unknown command: invalid_cmd"
}
```

### Validation Errors

**Invalid Arguments:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "Invalid spawn command arguments: missing 'entity' field"
}
```

**Business Logic Error:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "Position is not walkable: (10, 5)"
}
```

### Server Errors

**Internal Error:**
```json
{
  "type": "event",
  "event": "command_result",
  "success": false,
  "message": "Internal error: NullReferenceException at CommandHandler.cs:123"
}
```

## Versioning Strategy

### Current Approach: No Versioning

**Rationale:**
- DevTools is a development tool, not a production API
- Client and server can be version-locked (same repo)
- Breaking changes are acceptable during development

### Future Versioning (if needed)

**Option 1: Protocol Version Field**
```json
{
  "type": "command",
  "version": "1.0",
  "cmd": "spawn",
  "args": { ... }
}
```

**Option 2: URL Path Versioning**
```
ws://127.0.0.1:5007/v1/
ws://127.0.0.1:5007/v2/
```

**Option 3: Capability Negotiation**
```json
// Client sends on connect
{ "type": "hello", "supportedVersions": ["1.0", "1.1"] }

// Server responds
{ "type": "hello", "version": "1.0", "capabilities": ["spawn", "tp", ...] }
```

**Recommendation:** Start with no versioning, add protocol version field if needed.

## Extension Points

### Adding New Commands

1. Define command arguments DTO
2. Add handler method
3. Add to command switch
4. Update client implementations
5. Document in this RFC

**Backwards Compatibility:**
- New commands are always backwards-compatible (clients ignore unknown events)
- Changing existing command signatures is a breaking change
- Use optional fields for non-breaking additions

### Adding New Events

1. Define event DTO
2. Emit event from game logic
3. Broadcast via `DevToolsServer.BroadcastEventAsync()`
4. Update client implementations
5. Document in this RFC

**Backwards Compatibility:**
- Clients should ignore unknown event types
- New fields in existing events are backwards-compatible

## Client Implementation Guidelines

### Connection Lifecycle

1. **Connect** - `WebSocket.connect("ws://127.0.0.1:5007")`
2. **Receive welcome** - Server sends initial `command_result` event
3. **Send commands** - Client sends `command` messages
4. **Receive responses** - Server sends `event` messages
5. **Disconnect** - Client or server closes WebSocket

### Best Practices

**1. Handle Connection Failures:**
```rust
match connect_async(url).await {
    Ok((ws, _)) => { /* connected */ },
    Err(e) => eprintln!("Failed to connect: {}", e),
}
```

**2. Parse JSON Robustly:**
```rust
let event: DevEvent = match serde_json::from_str(&text) {
    Ok(e) => e,
    Err(e) => {
        eprintln!("Failed to parse response: {}", e);
        continue;
    }
};
```

**3. Check Success Flag:**
```rust
if event.success {
    println!("✓ {}", event.message);
} else {
    eprintln!("✗ {}", event.message);
}
```

**4. Display Result Data:**
```rust
if let Some(result) = event.result {
    println!("{}", serde_json::to_string_pretty(&result)?);
}
```

## Testing

### Protocol Conformance Tests

**1. Valid Command:**
```
Input:  {"type":"command","cmd":"ping"}
Output: {"type":"event","event":"command_result","success":true,"message":"pong"}
```

**2. Invalid JSON:**
```
Input:  {invalid json}
Output: {"type":"event","event":"command_result","success":false,"message":"Invalid JSON: ..."}
```

**3. Unknown Command:**
```
Input:  {"type":"command","cmd":"unknown"}
Output: {"type":"event","event":"command_result","success":false,"message":"Unknown command: unknown"}
```

**4. Missing Arguments:**
```
Input:  {"type":"command","cmd":"spawn","args":{}}
Output: {"type":"event","event":"command_result","success":false,"message":"Invalid spawn command arguments"}
```

### Integration Tests

See RFC-013 for integration testing strategy.

## C# DTO Definitions

**Location:** `dotnet/dev-tools/core/PigeonPea.DevTools/Protocol/`

**DevCommand.cs:**
```csharp
public class DevCommand
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "command";

    [JsonPropertyName("cmd")]
    public string Cmd { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public Dictionary<string, object>? Args { get; set; }
}
```

**DevEvent.cs:**
```csharp
public class DevEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "event";

    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}

public class CommandResultEvent : DevEvent
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public object? Result { get; set; }
}
```

## References

- RFC-013: DevTools System Architecture
- RFC-015: DevTools Security and Deployment
- RFC 6455: The WebSocket Protocol
- JSON Specification: https://www.json.org/

## Conclusion

The DevTools protocol is simple, extensible, and human-readable. The JSON-based approach enables easy client implementation in any language while maintaining type safety through C# DTOs. Future extensions (event broadcasting, filtering, subscriptions) can be added without breaking existing clients.
