# Dungeon Protocol

This crate defines the shared communication protocol between the Pigeon Pea dungeon game and the development tools.

## Protocol Overview

The protocol uses versioned JSON messages exchanged over WebSocket connections. All messages follow a common envelope structure with specific payload types.

## Message Envelope

Every message is wrapped in this envelope:

```json
{
  "version": 1,
  "type": "gm.command|gm.reply|event.state|event.log",
  "id": "uuid-v4",
  "correlation_id": "uuid-v4",
  "payload": {
    /* type-specific data */
  }
}
```

### Fields

- **version**: Protocol version (currently 1)
- **type**: Message type identifier
- **id**: Unique message identifier (UUID v4)
- **correlation_id**: For replies, echoes the original command's ID
- **payload**: Message-specific data

## Message Types

### gm.command

Commands sent from dev-tool to game:

```json
{
  "type": "gm.command",
  "payload": {
    "cmd": "spawn|tp|reload|regen-map|load-map|custom",
    "args": {
      /* command-specific arguments */
    }
  }
}
```

#### Commands

**spawn**: Spawn an entity

```json
{
  "cmd": "spawn",
  "args": {
    "mob": "goblin",
    "x": 10,
    "y": 5
  }
}
```

**tp**: Teleport player

```json
{
  "cmd": "tp",
  "args": {
    "x": 20,
    "y": 3
  }
}
```

**reload**: Reload game data from disk

```json
{
  "cmd": "reload",
  "args": {}
}
```

**regen-map**: Regenerate current map

```json
{
  "cmd": "regen-map",
  "args": {
    "seed": 12345
  }
}
```

**load-map**: Load specific map file

```json
{
  "cmd": "load-map",
  "args": {
    "path": "./data/maps/dungeon1.json"
  }
}
```

### gm.reply

Replies from game to dev-tool:

```json
{
  "type": "gm.reply",
  "correlation_id": "original-command-id",
  "payload": {
    "ok": true,
    "data": {
      /* optional response data */
    },
    "error": {
      /* optional error info */
    }
  }
}
```

### event.state

Game state change events:

```json
{
  "type": "event.state",
  "payload": {
    "timestamp": "2025-01-15T10:30:00Z",
    "entity": "player",
    "component": "position",
    "value": { "x": 10, "y": 5 }
  }
}
```

### event.log

Game log events:

```json
{
  "type": "event.log",
  "payload": {
    "timestamp": "2025-01-15T10:30:00Z",
    "level": "info|warn|error|debug",
    "message": "Player moved north",
    "context": {
      /* optional context data */
    }
  }
}
```

## Error Handling

Errors in replies:

```json
{
  "ok": false,
  "error": {
    "code": "invalid_command|bad_args|internal_error",
    "message": "Invalid mob type: dragon"
  }
}
```

### Error Codes

- **invalid_command**: Unknown command
- **bad_args**: Invalid command arguments
- **internal_error**: Server-side error
- **unauthorized**: Missing/invalid auth token
- **timeout**: Command execution timeout

## Versioning

Protocol versioning policy:

- **Major version**: Breaking changes to envelope or core message types
- **Minor version**: Adding new commands or optional fields
- **Patch version**: Bug fixes, documentation updates

Compatibility:

- Server must support all minor versions within its major version
- Clients should negotiate highest compatible version
- Version 1 is current stable version

## Security

### Authentication

Development tokens via environment variable or first message:

```bash
# Environment variable
export DEV_TOOL_TOKEN="dev-secret-123"

# Or include in first message
{
  "type": "auth",
  "token": "dev-secret-123"
}
```

### Network Security

- Default: localhost only (127.0.0.1)
- Optional: LAN mode with explicit flag
- Never expose to public internet in development builds
- Use HTTPS/WSS in production if needed

## Implementation Notes

### Client Responsibilities

- Generate unique UUIDs for each command
- Handle connection failures and reconnection
- Process replies and correlate with commands
- Filter and format event streams as needed
- Respect rate limiting and timeouts

### Server Responsibilities

- Validate all incoming messages
- Correlate replies with command IDs
- Send state events for relevant changes
- Maintain connection state and authentication
- Handle graceful shutdowns

### Performance Considerations

- Batch state updates when possible
- Use efficient JSON serialization
- Implement backpressure for high-frequency events
- Consider binary protocol for high-performance scenarios

## Examples

See the `examples/` directory for complete message flows and test cases.

## Testing

Run protocol tests:

```bash
cargo test -p dungeon-protocol
```

Tests cover:

- Message serialization/deserialization
- Schema validation
- Error handling
- Version compatibility
