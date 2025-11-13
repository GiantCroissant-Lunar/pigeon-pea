# RFC-013: DevTools System Architecture

Status: Implemented
Date: 2025-11-13
Author: Claude (Anthropic)

## Summary

Define a WebSocket-based developer tools infrastructure that enables runtime inspection and control of PigeonPea game instances from external clients. The system provides a unified architecture for both Console (Terminal.Gui) and Windows (Avalonia) applications.

## Motivation

Modern game development requires robust debugging and testing tools. Traditional roguelike development often involves:
- Restarting the game to test specific scenarios
- Manually navigating to specific map locations
- Difficulty reproducing edge cases
- No way to inspect runtime ECS state without debugger attachment

DevTools solves these problems by providing:
- **Live entity spawning** - Test combat/AI without playing through entire game
- **Player teleportation** - Instantly navigate to any map location
- **Runtime state inspection** - Query ECS entities, components, health, etc.
- **Automated testing** - External scripts can drive game state
- **Future AI integration** - AI agents can control the game via WebSocket

## Goals

1. **Platform-agnostic protocol** - Works with Console, Windows, and future platforms
2. **External client support** - Any language can implement a client (Rust, Python, JS, etc.)
3. **Non-intrusive integration** - DevTools disabled by default, opt-in only
4. **Multi-client capable** - Multiple tools can connect simultaneously
5. **Command extensibility** - Easy to add new commands without protocol breaking changes
6. **Event broadcasting** - Clients can subscribe to game events (future)

## Non-Goals

1. **Production deployment** - DevTools is strictly for development/testing
2. **Authentication/authorization** - Localhost-only, no security layer needed
3. **Persistence** - Commands execute immediately, no undo/replay (yet)
4. **GUI client** - Initial focus on CLI; web dashboard is future work

## Architecture Overview

### Component Diagram

```text
┌─────────────────────────────────────────────────────────┐
│              External DevTools Clients                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │  Rust CLI    │  │  Python      │  │  Web         │ │
│  │  (pp-dev)    │  │  Scripts     │  │  Dashboard   │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└───────────────┬─────────────────────────────────────────┘
                │ WebSocket (ws://127.0.0.1:5007)
                │ JSON Protocol (RFC-014)
                ▼
┌─────────────────────────────────────────────────────────┐
│           PigeonPea.DevTools (C# Library)               │
│  ┌───────────────────────────────────────────────────┐ │
│  │ DevToolsServer (WebSocket Server)                 │ │
│  │ - HttpListener (ws:// endpoint)                   │ │
│  │ - Client connection management                    │ │
│  │ - Message routing                                 │ │
│  └───────────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────────┐ │
│  │ CommandHandler                                    │ │
│  │ - Parses JSON commands                            │ │
│  │ - Executes operations on GameWorld (Arch ECS)     │ │
│  │ - Returns CommandResultEvent                      │ │
│  └───────────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────────┐ │
│  │ Protocol (DTOs)                                   │ │
│  │ - DevCommand (client → server)                    │ │
│  │ - DevEvent (server → client)                      │ │
│  │ - Specific command/event types                    │ │
│  └───────────────────────────────────────────────────┘ │
└───────────────┬─────────────────────────────────────────┘
                │ Direct access to GameWorld
                ▼
┌─────────────────────────────────────────────────────────┐
│              Game Applications                          │
│  ┌──────────────────┐      ┌──────────────────┐       │
│  │  Console App     │      │  Windows App     │       │
│  │  (Terminal.Gui)  │      │  (Avalonia)      │       │
│  │                  │      │                  │       │
│  │  GameApplication │      │  MainWindow      │       │
│  │  └─GameWorld     │      │  └─GameWorld     │       │
│  └──────────────────┘      └──────────────────┘       │
└─────────────────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────┐
│              Shared Game Logic                          │
│  - GameWorld (Arch ECS)                                 │
│  - Components (Position, Health, Renderable, etc.)      │
│  - Systems (FOV, AI, Combat, etc.)                      │
└─────────────────────────────────────────────────────────┘
```

### Data Flow

**Command Execution:**
```text
1. Client sends JSON command
   → WebSocket message

2. DevToolsServer receives message
   → Parses DevCommand JSON

3. CommandHandler.ExecuteCommandAsync()
   → Deserializes specific command type
   → Validates arguments
   → Executes on GameWorld (ECS operations)
   → Returns CommandResultEvent

4. DevToolsServer sends response
   → Serializes event to JSON
   → WebSocket.SendAsync()

5. Client receives response
   → Displays result to user
```

**Event Broadcasting (future):**
```text
GameWorld.Update()
  → Entity moved / created / died
  → DevToolsServer.BroadcastEventAsync()
  → All connected clients receive event
```

## Implementation Details

### 1. PigeonPea.DevTools Library

**Location:** `dotnet/dev-tools/core/PigeonPea.DevTools/`

**Key Classes:**

**`DevToolsServer`**
- WebSocket server using `HttpListener`
- Binds to `127.0.0.1:PORT` (localhost only)
- Manages connected clients (`ConcurrentBag<WebSocket>`)
- Routes messages to `CommandHandler`
- Broadcasts events to all clients

**`CommandHandler`**
- Executes commands on `GameWorld` instance
- Command dispatch via switch expression
- Returns `CommandResultEvent` for all operations
- Handles errors gracefully (logs + error response)

**Protocol DTOs:**
- `DevCommand` - Base command class
- `DevEvent` - Base event class
- Specific types (see RFC-014)

### 2. Integration Pattern

Both Console and Windows apps follow this pattern:

**Console App:**
```csharp
// Program.cs
var enableDevToolsOption = new Option<bool>("--enable-dev-tools");
var devToolsPortOption = new Option<int>("--dev-tools-port", () => 5007);

// ...

DevToolsServer? devToolsServer = null;
if (enableDevTools)
{
    devToolsServer = new DevToolsServer(gameApp.GameWorld, devToolsPort);
    await devToolsServer.StartAsync();
}

Application.Run(gameApp);

// Cleanup
devToolsServer?.StopAsync().Wait();
devToolsServer?.Dispose();
```

**Windows App:**
```csharp
// MainWindow.axaml.cs
var enableDevTools = Environment.GetEnvironmentVariable("PIGEONPEA_DEV_TOOLS") == "1";
if (enableDevTools)
{
    var port = int.Parse(Environment.GetEnvironmentVariable("PIGEONPEA_DEV_TOOLS_PORT") ?? "5007");
    _devToolsServer = new DevToolsServer(_gameWorld, port);
    await _devToolsServer.StartAsync();
}

// Dispose() cleanup
_devToolsServer?.StopAsync().Wait();
_devToolsServer?.Dispose();
```

**Key Requirements:**
- Expose `GameWorld` property from main window/application class
- Start server **after** game initialization (GameWorld created)
- Stop server **before** disposing GameWorld
- Handle async operations properly (no fire-and-forget without error handling)

### 3. Client Implementation (Rust CLI)

**Location:** `dotnet/dev-tools/clients/rust-cli/`

**Binary name:** `pp-dev`

**Key Features:**
- Interactive REPL using `tokio::io::AsyncBufReadExt`
- Single-command mode via `clap` subcommands
- Colored terminal output (`colored` crate)
- Async WebSocket client (`tokio-tungstenite`)

**Architecture:**
```rust
main()
  → parse CLI args (clap)
  → connect to WebSocket server
  → REPL loop or single command
      → parse user input
      → build DevCommand JSON
      → send via WebSocket
      → receive DevEvent response
      → print formatted result
```

## Extensibility

### Adding New Commands

1. **Define DTO in `Protocol/DevCommand.cs`:**
```csharp
public class NewCommand
{
    [JsonPropertyName("arg1")]
    public string Arg1 { get; set; } = string.Empty;
}
```

2. **Add handler in `CommandHandler.cs`:**
```csharp
private async Task<CommandResultEvent> HandleNewCommandAsync(DevCommand command)
{
    var newCmd = JsonSerializer.Deserialize<NewCommand>(...);
    // Execute on _gameWorld
    return CommandResult(true, "Success");
}
```

3. **Add to dispatch switch:**
```csharp
return command.Cmd.ToLowerInvariant() switch
{
    "new" => await HandleNewCommandAsync(command),
    // ...
};
```

4. **Update Rust CLI (optional):**
```rust
"new" => Some(DevCommand {
    cmd: "new".to_string(),
    args: Some(json!({ "arg1": parts[1] })),
}),
```

### Multi-Client Support

Current implementation supports multiple simultaneous clients:
- Each client gets a separate WebSocket connection
- Commands execute sequentially (no concurrency issues)
- Broadcast events go to all connected clients
- No client authentication or session management

## Performance Considerations

**Command Execution:**
- Commands execute on game thread (ECS not thread-safe)
- WebSocket server runs on separate thread
- Commands queued and processed during game loop (future)

**Current Implementation:**
- Commands execute **immediately** (synchronous on WebSocket thread)
- Works because:
  - Arch ECS supports concurrent queries (structural changes require exclusive access)
  - Game loop and DevTools don't overlap in current design
  - Only one game instance per process

**Future Improvements:**
- Command queue processed during `GameWorld.Update()`
- Thread-safe command queueing (`ConcurrentQueue<DevCommand>`)
- Event broadcasting after each update tick

## Testing Strategy

**Manual Testing:**
1. Start game with `--enable-dev-tools`
2. Connect `pp-dev` client
3. Execute commands (spawn, tp, query, etc.)
4. Verify results in game window

**Automated Testing (future):**
```python
import asyncio
import websockets
import json

async def test_spawn():
    async with websockets.connect("ws://127.0.0.1:5007") as ws:
        await ws.send(json.dumps({
            "type": "command",
            "cmd": "spawn",
            "args": {"entity": "goblin", "x": 10, "y": 5}
        }))
        response = await ws.recv()
        assert json.loads(response)["success"] == True
```

## Migration Path

**Current State:**
- ✅ WebSocket server implemented
- ✅ Rust CLI client implemented
- ✅ Basic commands (spawn, tp, query, heal, kill, give)
- ✅ Console app integration
- ✅ Windows app integration

**Future Enhancements (separate RFCs):**
- Event broadcasting (entity moved, died, etc.)
- Command queueing (thread-safe execution)
- Web dashboard client (xterm.js + React)
- Map editor tools (tile placement, room generation)
- AI agent integration (LLM-controlled gameplay)

## Security Considerations

See RFC-015 for detailed security model.

**Key Points:**
- Localhost only (`127.0.0.1` binding)
- No authentication (development tool only)
- Disabled by default
- Never enabled in production builds

## Dependencies

**C# Library:**
- `System.Net.WebSockets` (built-in)
- `System.Net.HttpListener` (built-in)
- `Serilog` (logging)
- `Arch.Core` (ECS access)
- `SadRogue.Primitives` (Point types)

**Rust CLI:**
- `tokio` (async runtime)
- `tokio-tungstenite` (WebSocket client)
- `serde` / `serde_json` (JSON serialization)
- `clap` (CLI argument parsing)
- `colored` (terminal colors)

## References

- RFC-014: DevTools Protocol Specification
- RFC-015: DevTools Security and Deployment
- Implementation: `dotnet/dev-tools/`
- Documentation: `dotnet/dev-tools/README.md`

## Alternatives Considered

### 1. Named Pipes / IPC
**Pros:** Faster than WebSocket, OS-native
**Cons:** Platform-specific, harder cross-language support
**Decision:** WebSocket chosen for language-agnostic clients

### 2. HTTP REST API
**Pros:** Simpler than WebSocket, widely understood
**Cons:** No real-time events, more overhead per request
**Decision:** WebSocket chosen for future event streaming

### 3. Embedded Scripting (Lua/Python)
**Pros:** No separate process, direct ECS access
**Cons:** Couples game to scripting runtime, security concerns
**Decision:** External clients preferred for isolation

### 4. In-Game Console (Press `~` key)
**Pros:** No external tool needed, traditional approach
**Cons:** Conflicts with gameplay, limited UI, hard to automate
**Decision:** External tools better for automation and multi-monitor workflows

## Open Questions

1. **Command queueing vs immediate execution?**
   - Current: Immediate (works for now)
   - Future: Queue and process during game loop?

2. **Event subscription model?**
   - Subscribe to specific events?
   - Filter by entity type?
   - Client-side filtering?

3. **State snapshots?**
   - Should clients be able to request full world state?
   - Serialization format (JSON vs binary)?

4. **Replay / recording?**
   - Record command sequences?
   - Replay for testing?
   - Save/load scenarios?

## Conclusion

The DevTools system provides a solid foundation for runtime game control and inspection. The WebSocket-based architecture enables:
- Multi-platform support (Console + Windows)
- Multi-language clients (Rust, Python, JS, etc.)
- Future extensibility (web dashboards, AI agents, automated testing)

The implementation is non-intrusive (opt-in), secure (localhost-only), and follows established patterns (WebSocket + JSON protocol).
