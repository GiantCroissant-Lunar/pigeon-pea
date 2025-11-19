# Dungeon Development Tools

A Rust-based development toolkit for the Pigeon Pea dungeon game, providing WebSocket-based communication with the game server for development and debugging.

## Overview

This workspace contains:

- **dungeon-protocol**: Shared protocol definitions for game communication
- **dungeon-dev-tool**: CLI tool for game development and debugging

## Quick Start

### Prerequisites

- Rust 1.70+ with the 2024 edition
- Windows 10+ (for Rio terminal integration)
- Access to the game's WebSocket server

### Building

```bash
# Build all workspace members
cargo build --release

# Build specific component
cargo build -p dungeon-dev-tool --release
cargo build -p dungeon-protocol --release
```

### Running the CLI

```bash
# Run with default settings
cargo run --bin dev-tool

# Show help
cargo run --bin dev-tool -- --help

# Connect to specific server
cargo run --bin dev-tool -- --server ws://localhost:8080

# Use authentication token
cargo run --bin dev-tool -- --token your-token-here
```

## Architecture

### Protocol (`dungeon-protocol`)

The protocol crate defines:
- Message types for game communication
- Error handling and validation
- Serialization/deserialization helpers
- WebSocket envelope structure

### CLI Tool (`dungeon-dev-tool`)

The development tool provides:
- WebSocket client with authentication
- Command execution framework
- Multiple output formats (text, JSON)
- Structured error handling
- Event streaming capabilities

## Integration with Development Workflow

### Rio Terminal Setup

This tool is designed to work with Rio terminal in a multi-window setup:

```
┌─────────────────────────────────────────────────────────────┐
│ Rio Window 1: Game (Terminal.Gui v2)              │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ $ dotnet run --project Dungeon.Console        │   │
│ │ > Player at (10, 5)                         │   │
│ │ > HP: 15/20                                 │   │
│ └─────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│ Rio Window 2: Development Tools                     │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ $ cargo run --bin dev-tool --spawn goblin      │   │
│ │ Entity Type: goblin                             │   │
│ │ Position: (15, 8)                             │   │
│ │ Spawned successfully                            │   │
│ └─────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│ Rio Window 3: Asset Browser (Yazi)                  │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ 📁 data/maps/                               │   │
│ │ 📄 dungeon1.json                            │   │
│ │ 📄 dungeon2.json                            │   │
│ │ 📄 enemies.json                              │   │
│ └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Yazi Integration

The CLI tool can be launched from Yazi for seamless asset editing:

```bash
# In Yazi, select a map file and press ':'
:open %f --with dev-tool --load-map
```

## Development Status

### ✅ Completed

- [x] Workspace structure with protocol and CLI components
- [x] Basic WebSocket client functionality
- [x] Command-line argument parsing with clap
- [x] Error handling and logging
- [x] Cross-platform build configuration
- [x] Rio terminal integration documentation

### 🚧 In Progress

- [ ] Full WebSocket command implementation
- [ ] Event streaming and real-time updates
- [ ] Configuration file support
- [ ] Shell completions

### 📋 Planned

- [ ] Advanced debugging features
- [ ] Map visualization tools
- [ ] Asset validation and linting
- [ ] Integration tests
- [ ] Performance profiling tools

## Configuration

The CLI tool supports configuration via:
- Command-line arguments
- Environment variables (`DUNGEON_SERVER_URL`, `DUNGEON_TOKEN`)
- Future: Configuration files

### Environment Variables

```bash
export DUNGEON_SERVER_URL="ws://localhost:5007"
export DUNGEON_TOKEN="your-auth-token"
```

## WebSocket Protocol

### Message Format

All messages use the following envelope:

```json
{
  "id": "uuid-v4",
  "timestamp": "2024-01-01T00:00:00Z",
  "msg_type": "gm_command|gm_reply|event_state|event_log",
  "correlation_id": "uuid-v4",
  "payload": { ... }
}
```

### Command Examples

```json
// Spawn entity
{
  "cmd": "spawn",
  "args": {
    "entity_type": "goblin",
    "position": { "x": 10, "y": 5 }
  }
}

// Teleport player
{
  "cmd": "teleport",
  "args": {
    "target": "player",
    "position": { "x": 15, "y": 8 }
  }
}

// Reload data
{
  "cmd": "reload",
  "args": {
    "types": ["maps", "entities"]
  }
}
```

## Error Handling

The tool provides structured error handling:

```bash
# Network errors
Error: Failed to connect to ws://localhost:5007: Connection refused

# Protocol errors
Error: Invalid message format: Missing required field 'id'

# Authentication errors
Error: Authentication failed: Invalid or expired token
```

## Contributing

1. Follow Rust best practices and conventions
2. Ensure all workspace members compile with `cargo build`
3. Add tests for new functionality
4. Update documentation as needed

## License

MIT License - see LICENSE file for details.

## Related Projects

- [Pigeon Pea](https://github.com/GiantCroissant-Lunar/pigeon-pea) - Main project
- [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) - Terminal UI framework
- [Rio Terminal](https://github.com/raphamorim/rio) - Terminal multiplexer
- [Yazi](https://github.com/sxyazi/yazi) - Terminal file manager
