---
title: 'MCP Project Launcher System'
doc_type: guide
status: draft
created: 2025-01-15
---

# MCP Project Launcher System

## Overview

This document describes the MCP (Model Context Protocol) project launcher system implemented for the pigeon-pea project. The system enables per-project MCP server configurations for AI coding assistants that only support global configuration files.

## Problem Statement

Modern AI coding assistants (GitHub Copilot, Cursor, Windsurf, Cline, etc.) use MCP servers to extend their capabilities. However, most editors only support **global MCP configurations** stored in the user's home directory, making it difficult to:

1. Use different MCP servers for different projects
2. Share MCP configurations with team members via version control
3. Automatically configure MCP when cloning a project

## Solution

The MCP Project Launcher system uses an **indirection pattern** to enable per-project MCP configurations:

```
Editor (Global Config)
  → Launcher Script (Global Installation)
    → Project Config (.mcp/servers/*.json)
      → Actual MCP Server
```

### How It Works

1. **Global Launcher**: A universal launcher script is installed once in `~/.local/mcp-launcher/`
2. **Global IDE Config**: The IDE's global MCP config points to the launcher script
3. **Project Detection**: The launcher runs in the project's working directory
4. **Dynamic Loading**: The launcher reads project-specific configs from `.mcp/servers/`
5. **Server Execution**: The launcher starts the appropriate MCP server

## Implementation

### Directory Structure

```
.mcp/
├── servers/              # Project-specific MCP server configs
│   ├── github.json
│   ├── sequential-thinking.json
│   ├── memory-qdrant.json
│   └── _template.json
├── launcher/            # Universal launcher scripts
│   ├── launcher.py      # Python version
│   └── launcher.js      # Node.js version
├── scripts/             # Helper scripts
│   ├── detect-ide-configs.py
│   └── detect-ide-configs.ps1
├── setup-windows.ps1    # Automated setup (Windows)
├── setup-unix.sh        # Automated setup (Linux/Mac)
├── README.md
├── IDE-CONFIG-PATHS.md
└── SETUP-WINDOWS.md
```

### Components

#### 1. Launcher Scripts

**Python Launcher** (`.mcp/launcher/launcher.py`):

- Universal launcher supporting all platforms
- Environment variable expansion (`${VAR}`, `${PROJECT_ROOT}`)
- Dynamic config loading
- Process replacement via `os.execvpe()`

**Node.js Launcher** (`.mcp/launcher/launcher.js`):

- Alternative for Node.js environments
- Same features as Python version

#### 2. IDE Detection Scripts

**Purpose**: Automatically detect installed IDEs and locate their MCP config paths.

**Python Version** (`.mcp/scripts/detect-ide-configs.py`):

- Cross-platform detection
- JSON output support
- Checks for:
  - GitHub Copilot
  - Cursor
  - Windsurf
  - Cline
  - Zed
  - Claude Desktop
  - VS Code

**PowerShell Version** (`.mcp/scripts/detect-ide-configs.ps1`):

- Windows-native implementation
- Same detection capabilities

#### 3. Setup Scripts

**Windows Setup** (`.mcp/setup-windows.ps1`):

- Interactive setup wizard
- Launcher installation
- Environment variable configuration
- IDE config generation
- Validation

**Unix Setup** (`.mcp/setup-unix.sh`):

- Bash-based setup for Linux/Mac
- Shell config integration (.bashrc, .zshrc)
- Same features as Windows version

#### 4. Server Configurations

**Format** (`.mcp/servers/*.json`):

```json
{
  "name": "server-name",
  "description": "Server description",
  "command": "npx",
  "args": ["-y", "@org/mcp-server"],
  "env": {
    "API_KEY": "${API_KEY}"
  },
  "cwd": "${PROJECT_ROOT}"
}
```

**Pre-configured Servers**:

- `github.json` - GitHub integration (repos, issues, PRs)
- `sequential-thinking.json` - Step-by-step reasoning
- `memory-qdrant.json` - Long-term memory with Qdrant
- `_template.json` - Template for new servers

### Taskfile Integration

Added tasks to `Taskfile.yml`:

```yaml
# Detection
mcp:detect-ides          # Python version
mcp:detect-ides:win      # PowerShell version
mcp:detect-ides:json     # JSON output

# Setup
mcp:setup:win            # Interactive setup (Windows)
mcp:setup:unix           # Interactive setup (Linux/Mac)
mcp:install-launcher:win # Non-interactive install
mcp:install-launcher:unix

# Testing
mcp:test-launcher        # Test launcher
mcp:docs                 # Open documentation
```

## IDE Compatibility

| IDE            | Global Config Support | Project Config Support | Launcher Needed?     |
| -------------- | --------------------- | ---------------------- | -------------------- |
| GitHub Copilot | ✅                    | ❌                     | ✅ Required          |
| Cursor         | ✅                    | ✅                     | ⚠️ Optional          |
| Windsurf       | ✅                    | ⚠️ Check docs          | ⚠️ Optional          |
| Cline          | ❌                    | ✅                     | ❌ Not needed        |
| Zed            | ✅                    | ⚠️ Varies              | ⚠️ Optional          |
| VS Code        | ✅                    | ✅                     | Depends on extension |

### IDE Config Paths

**GitHub Copilot**:

- Windows: `%USERPROFILE%\.config\github-copilot\mcp\servers.json`
- Linux/Mac: `~/.config/github-copilot/mcp/servers.json`

**Cursor**:

- Windows: `%APPDATA%\Cursor\User\globalStorage\mcp\servers.json`
- Mac: `~/Library/Application Support/Cursor/User/globalStorage/mcp/servers.json`
- Linux: `~/.config/Cursor/User/globalStorage/mcp/servers.json`

**Windsurf**:

- Windows: `%APPDATA%\Windsurf\mcp\servers.json`
- Mac: `~/Library/Application Support/Windsurf/mcp/servers.json`
- Linux: `~/.config/Windsurf/mcp/servers.json`

**Cline**: Project-level only (`.cline/mcp.json`)

See `.mcp/IDE-CONFIG-PATHS.md` for complete reference.

## Usage

### Quick Setup

**Windows**:

```powershell
task mcp:setup:win
```

**Linux/Mac**:

```bash
task mcp:setup:unix
```

### Manual Setup

1. **Detect IDEs**:

   ```bash
   task mcp:detect-ides
   ```

2. **Install Launcher**:

   ```bash
   task mcp:install-launcher:win  # Windows
   task mcp:install-launcher:unix # Linux/Mac
   ```

3. **Configure IDE**: Edit the IDE's global config to point to launcher

4. **Restart Editor**

### Adding New MCP Servers

1. Copy template:

   ```bash
   cp .mcp/servers/_template.json .mcp/servers/my-server.json
   ```

2. Edit configuration

3. Add to IDE's global config (if using launcher pattern)

## Benefits

1. **Per-Project Configs**: Different MCP servers for different projects
2. **Version Control**: Commit `.mcp/` to share configs with team
3. **Portability**: Automatic setup when cloning project
4. **IDE Agnostic**: Works with multiple editors
5. **Environment Variables**: Secure credential management
6. **Easy Discovery**: Detection scripts find IDE configs automatically

## Security Considerations

1. **Never commit secrets**: Use environment variables for API keys
2. **Gitignore sensitive files**: `.mcp/.gitignore` excludes logs and local configs
3. **Environment variable expansion**: Launcher supports `${VAR}` syntax
4. **Pre-commit hooks**: Validate configs before commit

## Testing

**Test IDE Detection**:

```bash
task mcp:detect-ides
```

**Test Launcher**:

```bash
task mcp:test-launcher
```

**Manual Test**:

```bash
python ~/.local/mcp-launcher/launcher.py github
```

## Documentation

- **`.mcp/README.md`**: Main setup guide
- **`.mcp/IDE-CONFIG-PATHS.md`**: IDE path reference
- **`.mcp/SETUP-WINDOWS.md`**: Windows-specific guide
- This document: System architecture and design

## Future Enhancements

1. **Auto-detection of project changes**: Watch `.mcp/servers/` for changes
2. **MCP server marketplace**: Curated list of useful servers
3. **Config validation**: Schema validation for server configs
4. **Multi-server support**: Launch multiple servers simultaneously
5. **IDE plugins**: Native integration for popular editors

## Related Documentation

- RFC-012: Documentation Organization Management
- MCP Official Docs: https://modelcontextprotocol.io/
- Available MCP Servers: https://github.com/modelcontextprotocol/servers

## References

- Model Context Protocol: https://modelcontextprotocol.io/
- GitHub Copilot MCP Support: (check latest docs)
- Cursor MCP Integration: (check latest docs)
- Windsurf Documentation: (check latest docs)
