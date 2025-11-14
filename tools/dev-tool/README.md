# Dev Tool

Development tool for Pigeon Pea game management commands.

## Installation

Build the tool:

```bash
cd tools/dev-tool
cargo build --release
```

The compiled binary will be available at `target/release/dev-tool`.

## Commands

### spawn

Spawn a mob at specified coordinates.

```bash
dev-tool spawn --mob <name> --x <int> --y <int>
```

**Examples:**

```bash
# Text output (default)
dev-tool spawn --mob goblin --x 10 --y 20

# JSON output
dev-tool spawn --mob goblin --x 10 --y 20 --output json
```

**JSON Output:**

```json
{
  "cmd": "spawn",
  "args": {
    "mob": "goblin",
    "x": 10,
    "y": 20
  },
  "status": "success"
}
```

### tp

Teleport to specified coordinates.

```bash
dev-tool tp --x <int> --y <int>
```

**Examples:**

```bash
# Text output (default)
dev-tool tp --x 5 --y 15

# JSON output
dev-tool tp --x 5 --y 15 --output json
```

**JSON Output:**

```json
{
  "cmd": "tp",
  "args": {
    "x": 5,
    "y": 15
  },
  "status": "success"
}
```

### reload

Reload the game configuration.

```bash
dev-tool reload
```

**Examples:**

```bash
# Text output (default)
dev-tool reload

# JSON output
dev-tool reload --output json
```

**JSON Output:**

```json
{
  "cmd": "reload",
  "status": "success"
}
```

### regen-map

Regenerate the map with an optional seed.

```bash
dev-tool regen-map [--seed <int>]
```

**Examples:**

```bash
# With seed
dev-tool regen-map --seed 12345

# Without seed (random)
dev-tool regen-map

# JSON output
dev-tool regen-map --seed 12345 --output json
```

**JSON Output:**

```json
{
  "cmd": "regen-map",
  "args": {
    "seed": 12345
  },
  "status": "success"
}
```

## Output Formats

All commands support two output formats via the `--output` flag:

- `text` (default): Human-readable text output
- `json`: JSON-formatted output for programmatic use

## Exit Codes

- `0`: Success
- `1`: Error (validation failure, invalid arguments, etc.)

## Error Handling

Errors are written to stderr, while successful output goes to stdout.

Example of an error:

```bash
$ dev-tool spawn --mob "" --x 10 --y 20
Error: Mob name cannot be empty
$ echo $?
1
```

## Testing

Run unit tests:

```bash
cargo test
```

Run integration tests:

```bash
cargo test --test integration_tests
```

## Development

This tool is built with:

- [clap](https://docs.rs/clap/) - Command-line argument parsing
- [serde](https://serde.rs/) - JSON serialization
- [anyhow](https://docs.rs/anyhow/) - Error handling
