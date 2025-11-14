# IDE MCP Config Path Reference

This document lists the global MCP server configuration paths for various AI coding assistants and IDEs.

**Use the detection tool to find these automatically:**
```bash
# Python version
python .mcp/scripts/detect-ide-configs.py

# PowerShell version (Windows)
pwsh -File .mcp/scripts/detect-ide-configs.ps1

# Or use Task
task mcp:detect-ides        # Python
task mcp:detect-ides:win    # PowerShell
```

---

## GitHub Copilot (VS Code Extension)

### Global Config Path

| Platform | Path |
|----------|------|
| **Windows** | `%USERPROFILE%\.config\github-copilot\mcp\servers.json` |
| **macOS** | `~/.config/github-copilot/mcp/servers.json` |
| **Linux** | `~/.config/github-copilot/mcp/servers.json` |

### Example Path (Windows)
```
C:\Users\YourUsername\.config\github-copilot\mcp\servers.json
```

### Config Format
```json
{
  "server-name": {
    "command": "python",
    "args": ["path/to/launcher.py", "server-config-name"]
  }
}
```

### Project-Level Config
❌ **Not Supported** - Must use global config with launcher pattern

---

## Cursor

### Global Config Path

| Platform | Path |
|----------|------|
| **Windows** | `%APPDATA%\Cursor\User\globalStorage\mcp\servers.json` |
| **macOS** | `~/Library/Application Support/Cursor/User/globalStorage/mcp/servers.json` |
| **Linux** | `~/.config/Cursor/User/globalStorage/mcp/servers.json` |

### Example Path (Windows)
```
C:\Users\YourUsername\AppData\Roaming\Cursor\User\globalStorage\mcp\servers.json
```

### Project-Level Config
✅ **Supported** - `.cursor/mcp.json`

### Config Format
```json
{
  "mcpServers": {
    "server-name": {
      "command": "npx",
      "args": ["-y", "@org/mcp-server"],
      "env": {
        "API_KEY": "${API_KEY}"
      }
    }
  }
}
```

---

## Windsurf

### Global Config Path

| Platform | Path |
|----------|------|
| **Windows** | `%APPDATA%\Windsurf\mcp\servers.json` |
| **macOS** | `~/Library/Application Support/Windsurf/mcp/servers.json` |
| **Linux** | `~/.config/Windsurf/mcp/servers.json` |

### Example Path (Windows)
```
C:\Users\YourUsername\AppData\Roaming\Windsurf\mcp\servers.json
```

### Project-Level Config
✅ **Supported** (Check Windsurf documentation) - May use `.mcp/servers/`

### Config Format
Check Windsurf documentation for specific format.

---

## Cline (VS Code Extension)

### Global Config Path
❌ **Not Supported** - Cline uses **project-level config only**

### Project-Level Config
✅ **Required** - `.cline/mcp.json`

### Config Format
```json
{
  "mcpServers": {
    "server-name": {
      "command": "npx",
      "args": ["-y", "@org/mcp-server"],
      "env": {
        "API_KEY": "${API_KEY}"
      }
    }
  }
}
```

### Example
```json
{
  "mcpServers": {
    "github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "${GITHUB_PERSONAL_ACCESS_TOKEN}"
      }
    }
  }
}
```

---

## Zed Editor

### Global Config Path

| Platform | Path |
|----------|------|
| **Windows** | `%APPDATA%\Zed\settings.json` |
| **macOS** | `~/Library/Application Support/Zed/settings.json` |
| **Linux** | `~/.config/zed/settings.json` |

### Example Path (Windows)
```
C:\Users\YourUsername\AppData\Roaming\Zed\settings.json
```

### Project-Level Config
Check Zed documentation - MCP support may vary.

### Config Format
MCP configuration in Zed may be part of the main `settings.json` file. Check Zed documentation for specifics.

---

## Claude Desktop (Reference)

**Note:** Claude Desktop is not a coding IDE, but included for reference.

### Global Config Path

| Platform | Path |
|----------|------|
| **Windows** | `%APPDATA%\Claude\claude_desktop_config.json` |
| **macOS** | `~/Library/Application Support/Claude/claude_desktop_config.json` |
| **Linux** | `~/.config/Claude/claude_desktop_config.json` |

### Example Path (Windows)
```
C:\Users\YourUsername\AppData\Roaming\Claude\claude_desktop_config.json
```

### Config Format
```json
{
  "mcpServers": {
    "server-name": {
      "command": "npx",
      "args": ["-y", "@org/mcp-server"],
      "env": {
        "API_KEY": "your-key-here"
      }
    }
  }
}
```

---

## VS Code (Base Editor)

### Global Config Path

| Platform | Path |
|----------|------|
| **Windows** | `%APPDATA%\Code\User\settings.json` |
| **macOS** | `~/Library/Application Support/Code/User/settings.json` |
| **Linux** | `~/.config/Code/User/settings.json` |

### Example Path (Windows)
```
C:\Users\YourUsername\AppData\Roaming\Code\User\settings.json
```

### Project-Level Config
✅ **Supported** - `.vscode/settings.json`

### Notes
VS Code itself doesn't have built-in MCP support. You need extensions like:
- **GitHub Copilot** - See GitHub Copilot section above
- **Cline** - See Cline section above
- Other MCP-enabled extensions

---

## Quick Reference Table

| IDE | Global Config | Project Config | Launcher Needed? |
|-----|---------------|----------------|------------------|
| **GitHub Copilot** | `~/.config/github-copilot/mcp/servers.json` | ❌ | ✅ Yes |
| **Cursor** | `~/AppData/Roaming/Cursor/.../mcp/servers.json` | ✅ `.cursor/mcp.json` | ⚠️ Optional |
| **Windsurf** | `~/AppData/Roaming/Windsurf/mcp/servers.json` | ✅ Maybe | ⚠️ Optional |
| **Cline** | ❌ | ✅ `.cline/mcp.json` | ❌ No |
| **Zed** | `~/AppData/Roaming/Zed/settings.json` | ⚠️ Check docs | ⚠️ Optional |
| **Claude Desktop** | `~/AppData/Roaming/Claude/claude_desktop_config.json` | ❌ | N/A |

**Legend:**
- ✅ Supported
- ❌ Not supported
- ⚠️ Varies / Check documentation

---

## Using the MCP Project Launcher

For IDEs that **don't support project-level configs** (like GitHub Copilot), use the launcher pattern:

### 1. Install Launcher Globally

**Windows:**
```powershell
task mcp:install-launcher:win
```

**Linux/Mac:**
```bash
task mcp:install-launcher:unix
```

### 2. Configure IDE to Use Launcher

Edit the IDE's **global config** file (paths above) to point to the launcher:

**Example (GitHub Copilot - Windows):**
```json
{
  "github": {
    "command": "python",
    "args": [
      "C:\\Users\\YourUsername\\.local\\mcp-launcher\\launcher.py",
      "github"
    ]
  }
}
```

**Example (GitHub Copilot - Linux/Mac):**
```json
{
  "github": {
    "command": "python3",
    "args": [
      "~/.local/mcp-launcher/launcher.py",
      "github"
    ]
  }
}
```

### 3. Create Project-Specific Configs

Each project has its own MCP configs in `.mcp/servers/`:

```
.mcp/
  servers/
    github.json
    memory-qdrant.json
    sequential-thinking.json
```

The launcher reads these configs dynamically based on the current project!

---

## Finding Your IDE's Config Path

### Method 1: Use Detection Tool (Recommended)

```bash
# See all detected IDEs
task mcp:detect-ides

# Get JSON output
task mcp:detect-ides:json
```

### Method 2: Manual Search

**Windows (PowerShell):**
```powershell
# Search for config files
Get-ChildItem -Path $env:APPDATA, $env:LOCALAPPDATA, "$env:USERPROFILE\.config" -Recurse -Filter "*mcp*.json" -ErrorAction SilentlyContinue
```

**Linux/Mac (Bash):**
```bash
# Search for config files
find ~/ -name "*mcp*.json" 2>/dev/null
find ~/.config -name "*mcp*.json" 2>/dev/null
```

### Method 3: Check IDE Documentation

Each IDE's official documentation should list the config file location.

---

## Environment Variables in Configs

Most MCP servers require environment variables (API keys, tokens, etc.).

### Setting Environment Variables

**Windows (PowerShell - Persistent):**
```powershell
[System.Environment]::SetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN", "ghp_...", "User")
```

**Windows (PowerShell - Session Only):**
```powershell
$env:GITHUB_PERSONAL_ACCESS_TOKEN = "ghp_..."
```

**Linux/Mac (Bash):**
```bash
# Add to ~/.bashrc or ~/.zshrc
export GITHUB_PERSONAL_ACCESS_TOKEN="ghp_..."
export QDRANT_API_KEY="your-key"
```

### Using in Configs

```json
{
  "env": {
    "GITHUB_PERSONAL_ACCESS_TOKEN": "${GITHUB_PERSONAL_ACCESS_TOKEN}",
    "API_URL": "https://api.example.com"
  }
}
```

---

## Troubleshooting

### Config File Not Found

**Problem:** IDE can't find the config file.

**Solution:**
1. Run detection tool: `task mcp:detect-ides`
2. Create the directory if it doesn't exist
3. Create the config file with correct format

### IDE Doesn't Recognize MCP Servers

**Problem:** MCP servers don't appear in the IDE.

**Solution:**
1. Verify config path is correct for your IDE
2. Check config file syntax (use JSON validator)
3. Restart the IDE completely
4. Check IDE logs for errors

### Launcher Not Working

**Problem:** Launcher script fails to start.

**Solution:**
1. Verify launcher is installed: `ls ~/.local/mcp-launcher/`
2. Test launcher manually: `python ~/.local/mcp-launcher/launcher.py github`
3. Check project has `.mcp/servers/` directory
4. Verify environment variables are set

---

## Additional Resources

- [Main MCP Setup README](.mcp/README.md)
- [Windows Setup Guide](.mcp/SETUP-WINDOWS.md)
- [MCP Official Documentation](https://modelcontextprotocol.io/)
- [Available MCP Servers](https://github.com/modelcontextprotocol/servers)

---

## Contributing

Found a new IDE or config path? Please update this document!

1. Test the config path on your system
2. Verify the config format
3. Update this document
4. Update the detection scripts if needed
5. Submit a PR
