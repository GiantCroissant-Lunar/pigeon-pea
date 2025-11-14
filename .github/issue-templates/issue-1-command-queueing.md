# Issue 1: Implement Thread-Safe Command Queueing for DevTools

Currently, DevTools commands execute immediately on the WebSocket thread, which works but isn't thread-safe for future concurrent scenarios. Implement a command queue that's processed during the game loop.

## References

- RFC-013: DevTools System Architecture (Performance Considerations section)
- Current implementation: `dotnet/dev-tools/core/PigeonPea.DevTools/`

## Acceptance Criteria

- [ ] `CommandQueue` class using `ConcurrentQueue<T>`
- [ ] Commands enqueued on WebSocket thread, processed on game thread
- [ ] `ProcessDevToolsCommands()` called during `GameWorld.Update()`
- [ ] Thread-safety tests (spawn 100 entities from multiple clients)
- [ ] No breaking changes to existing protocol

## Technical Details

```csharp
// In GameWorld.cs
private readonly ConcurrentQueue<(DevCommand, TaskCompletionSource<CommandResultEvent>)> _devToolsQueue = new();

public void ProcessDevToolsCommands()
{
    while (_devToolsQueue.TryDequeue(out var item))
    {
        var (command, tcs) = item;
        var result = _commandHandler.Execute(command);
        tcs.SetResult(result);
    }
}
```

## Labels
- `enhancement`
- `dev-tools`
- `architecture`

## Milestone
DevTools v1.1
