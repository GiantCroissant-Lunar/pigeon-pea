# PigeonPea DevTools

Development tools for debugging and controlling PigeonPea game instances at runtime.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│              DevTools Clients                           │
│  ┌──────────────┐                                       │
│  │  Rust CLI    │ (pp-dev)                              │
│  └──────────────┘                                       │
└───────────────┬─────────────────────────────────────────┘
                │ WebSocket (ws://localhost:5007)
                ▼
┌─────────────────────────────────────────────────────────┐
│          PigeonPea.DevTools Server                      │
│  - WebSocket Server (DevToolsServer)                    │
│  - Command Handler                                      │
│  - Event Broadcaster                                    │
└───────────────┬─────────────────────────────────────────┘
                │ Operates on GameWorld (Arch ECS)
                ▼
┌─────────────────────────────────────────────────────────┐
│              Game Applications                          │
│  ┌──────────────┐      ┌──────────────┐               │
│  │ Console App  │      │ Windows App  │               │
│  └──────────────┘      └──────────────┘               │
└─────────────────────────────────────────────────────────┘
```

## Components

### PigeonPea.DevTools (C# Library)

Shared library that provides WebSocket server and command handling for dev tools.

**Location:** `dotnet/dev-tools/core/PigeonPea.DevTools/`

**Features:**
- WebSocket server for external clients
- Command execution on game world (spawn, teleport, query, etc.)
- Event broadcasting to connected clients
- Thread-safe operation with running game

### Rust CLI Client

Fast, lightweight terminal client for sending commands to running game instances.

**Location:** `dotnet/dev-tools/clients/rust-cli/`

**Binary name:** `pp-dev`

## Usage

### 1. Start the game with dev tools enabled

```bash
# Console app
dotnet run --project dotnet/console-app/core/PigeonPea.Console -- --enable-dev-tools

# Windows app
dotnet run --project dotnet/windows-app/core/PigeonPea.Windows -- --enable-dev-tools
```

The WebSocket server will start on `ws://127.0.0.1:5007` by default.

### 2. Connect with the Rust CLI

**Build the CLI:**

```bash
cd dotnet/dev-tools/clients/rust-cli
cargo build --release
```

**Run in REPL mode:**

```bash
./target/release/pp-dev
# or just
cargo run
```

**Run single commands:**

```bash
# Spawn a goblin at (10, 5)
pp-dev spawn goblin 10 5

# Teleport player to (20, 10)
pp-dev tp 20 10

# Query all entities
pp-dev query

# Give health potion to player
pp-dev give potion

# Heal player
pp-dev heal 50

# Kill all enemies
pp-dev kill all
```

### 3. Available Commands

| Command | Description | Example |
|---------|-------------|---------|
| `spawn <entity> <x> <y>` | Spawn entity at position | `spawn goblin 10 5` |
| `tp <x> <y>` | Teleport player | `tp 20 10` |
| `query` | List all entities | `query` |
| `give <item>` | Give item to player | `give potion` |
| `heal [amount]` | Heal player | `heal 100` |
| `kill [target]` | Kill enemies | `kill all` |
| `ping` | Test connection | `ping` |

**Supported entity types:**
- `goblin` or `enemy` - Spawns a goblin enemy
- `potion` or `health_potion` - Spawns a health potion

**Kill targets:**
- `nearest` (default) - Kill nearest enemy to player
- `all` or `enemies` - Kill all living enemies

## WebSocket Protocol

### Command Format (Client → Server)

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

### Response Format (Server → Client)

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
  },
  "timestamp": 1234567890
}
```

## Development

### Adding New Commands

1. **Add command DTO in `Protocol/DevCommand.cs`:**

```csharp
public class MyNewCommand
{
    [JsonPropertyName("someArg")]
    public string SomeArg { get; set; } = string.Empty;
}
```

2. **Add handler in `Handlers/CommandHandler.cs`:**

```csharp
private async Task<CommandResultEvent> HandleMyNewCommandAsync(DevCommand command)
{
    var myCmd = JsonSerializer.Deserialize<MyNewCommand>(
        JsonSerializer.Serialize(command.Args));

    // Execute on _gameWorld...

    return CommandResult(true, "Command executed");
}
```

3. **Add to command switch in `ExecuteCommandAsync`:**

```csharp
return command.Cmd.ToLowerInvariant() switch
{
    "mynew" => await HandleMyNewCommandAsync(command),
    // ... existing commands
};
```

4. **Add to Rust CLI in `src/main.rs`:**

```rust
"mynew" => Some(DevCommand {
    cmd_type: "command".to_string(),
    cmd: "mynew".to_string(),
    args: Some(json!({ "someArg": parts[1] })),
}),
```

## Security

⚠️ **Important:** DevTools server binds to `127.0.0.1` only (localhost). It should **never** be exposed to external networks.

- Only enable dev tools in development builds
- Never run with `--enable-dev-tools` in production
- The server has no authentication (by design for development ease)

## Troubleshooting

**"Failed to connect" error:**
- Make sure the game is running with `--enable-dev-tools` flag
- Check that port 5007 is not already in use
- Verify the game successfully started the WebSocket server (check logs)

**Commands not working:**
- Check the command format (use `help` in REPL mode)
- Verify entity/item names are correct (case-insensitive)
- Check game logs for detailed error messages

**Connection drops:**
- The server automatically closes when the game exits
- Network issues or game crashes will disconnect clients
- Simply restart the connection after fixing the game
