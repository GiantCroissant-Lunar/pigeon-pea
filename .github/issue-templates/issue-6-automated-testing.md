# Issue 6: Add Automated Testing via DevTools Protocol

Build a test suite that uses DevTools protocol to automate game scenarios and verify behavior.

## References

- RFC-013: DevTools System Architecture (Testing Strategy)

## Acceptance Criteria

- [ ] Python test framework (pytest + websockets)
- [ ] Test scenarios: spawn entities, query state, move player, test combat
- [ ] CI integration (GitHub Actions)
- [ ] Test report generation

## Example Test

```python
async def test_spawn_goblin():
    async with websockets.connect("ws://127.0.0.1:5007") as ws:
        await ws.send(json.dumps({
            "type": "command",
            "cmd": "spawn",
            "args": {"entity": "goblin", "x": 10, "y": 5}
        }))
        response = json.loads(await ws.recv())
        assert response["success"] == True
        assert response["result"]["x"] == 10
```

## Location

`dotnet/dev-tools/tests/integration/`

## Labels

- `testing`
- `dev-tools`
- `ci`

## Milestone

DevTools v1.2
