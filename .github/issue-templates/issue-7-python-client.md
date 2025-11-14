# Issue 7: Add Python DevTools Client

Create a Python client library for DevTools, making it easy to write scripts and automation.

## Acceptance Criteria

- [ ] Python package `pigeonpea-devtools`
- [ ] Async WebSocket client (using `websockets` library)
- [ ] Pythonic API wrapping protocol commands
- [ ] Type hints for all methods
- [ ] Documentation and examples
- [ ] Published to PyPI (optional)

## Example Usage

```python
from pigeonpea_devtools import DevToolsClient

async with DevToolsClient("ws://127.0.0.1:5007") as client:
    result = await client.spawn("goblin", x=10, y=5)
    print(f"Spawned entity {result.entity_id}")
    
    entities = await client.query()
    print(f"Found {len(entities)} entities")
```

## Location
`dotnet/dev-tools/clients/python/`

## Labels
- `enhancement`
- `dev-tools`
- `python`
- `good first issue`

## Milestone
DevTools v2.0
