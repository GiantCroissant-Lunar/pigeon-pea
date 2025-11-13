# dev-tool

Yazi-integrated Rust CLI for pigeon-pea development.

## Overview

`dev-tool` is a command-line utility designed to integrate with Yazi and provide WebSocket-based communication for development workflows. This is the foundation implementation with basic CLI parsing and configuration management.

## Installation

### Prerequisites

- Rust 1.70 or later
- Cargo

### Building

```bash
cd tools/dev-tool
cargo build --release
```

The compiled binary will be available at `target/release/dev-tool`.

## Usage

### Basic Commands

```bash
# Show help
dev-tool --help

# Show current configuration
dev-tool config

# Show configuration in JSON format
dev-tool --output json config

# Connect to server (stub)
dev-tool connect

# Send a message (stub)
dev-tool send "Hello, World!"

# List available operations (stub)
dev-tool list
```

### Global Options

- `--server <URL>`: WebSocket server URL (default: `ws://127.0.0.1:5007/gm`)
- `--token <TOKEN>`: Authentication token
- `--output <FORMAT>`: Output format: `text` or `json` (default: `text`)
- `--timeout <MS>`: Request timeout in milliseconds (default: `5000`)
- `--verbose, -v`: Enable verbose logging

### Environment Variables

Configuration can be provided via environment variables:

- `DEV_TOOL_SERVER`: WebSocket server URL
- `DEV_TOOL_TOKEN`: Authentication token

Example:

```bash
export DEV_TOOL_SERVER="ws://localhost:8080/gm"
export DEV_TOOL_TOKEN="my-secret-token"
dev-tool config
```

### Configuration File

The tool supports optional configuration files:

- **Windows**: `%APPDATA%\dev-tool\config.toml`
- **Unix/Linux/macOS**: `~/.config/dev-tool/config.toml`

Example `config.toml`:

```toml
server = "ws://localhost:8080/gm"
token = "my-token"
output = "json"
timeout = 10000
```

### Configuration Precedence

Configuration values are resolved in the following order (highest to lowest priority):

1. Command-line flags
2. Environment variables (`DEV_TOOL_*`)
3. Configuration file (`config.toml`)
4. Default values

## Subcommands

### `connect`

Connect to the WebSocket server.

```bash
dev-tool connect
dev-tool connect --url ws://custom-server:9000
```

**Status**: Stub implementation

### `send`

Send a message to the server.

```bash
dev-tool send "Hello, server!"
```

**Status**: Stub implementation

### `list`

List available operations.

```bash
dev-tool list
```

**Status**: Stub implementation

### `config`

Display the current effective configuration.

```bash
dev-tool config
dev-tool --output json config
```

**Status**: Fully implemented

## Development

### Running Tests

```bash
cargo test
```

### Running with Verbose Output

```bash
dev-tool -v config
```

This will display the effective configuration before executing the command.

## RFC Reference

This tool is implemented according to RFC-013: Yazi-integrated Rust CLI.

## Future Enhancements

The following features are planned for future releases:

- WebSocket client implementation
- Message protocol handlers
- Yazi integration
- Additional subcommands for development workflows

## License

MIT License - see the [LICENSE](../../LICENSE) file for details.
