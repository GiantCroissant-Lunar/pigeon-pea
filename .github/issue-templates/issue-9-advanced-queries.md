# Issue 9: Add DevTools Command: Advanced Entity Queries

Extend query command with filtering, pagination, and sorting.

## Acceptance Criteria

- [ ] Query by entity type: `query --type enemy`
- [ ] Query by health range: `query --health-below 30`
- [ ] Query in area: `query --area 0,0,20,20`
- [ ] Sort results: `query --sort health-asc`
- [ ] Pagination: `query --limit 10 --offset 20`
- [ ] Rust CLI updated with new query options

## Example Usage

```bash
# Find all enemies with low health
pp-dev query --type enemy --health-below 30

# Find all items in specific area
pp-dev query --type item --area 0,0,20,20

# Get first 10 entities sorted by health
pp-dev query --sort health-asc --limit 10
```

## Labels

- `enhancement`
- `dev-tools`
- `query`

## Milestone

DevTools v1.2
