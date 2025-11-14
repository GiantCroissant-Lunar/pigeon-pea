# MCP Project Configuration

This directory contains per-project MCP (Model Context Protocol) server configurations that work with GitHub Copilot, Windsurf, Cline, and other AI coding assistants.

## 📁 Directory Structure

```
.mcp/
├── servers/              # Project-specific MCP server configs
│   ├── github.json      # GitHub MCP server
│   ├── sequential-thinking.json
│   ├── memory-qdrant.json
│   └── _template.json   # Template for new configs
├── launcher/            # Universal launcher scripts
│   ├── launcher.py      # Python version
│   └── launcher.js      # Node.js version
├── scripts/             # Helper scripts
│   ├── detect-ide-configs.py   # IDE detection (Python)
│   └── detect-ide-configs.ps1  # IDE detection (PowerShell)
├── setup-windows.ps1    # Automated setup for Windows
├── setup-unix.sh        # Automated setup for Linux/Mac
├── README.md            # This file
├── IDE-CONFIG-PATHS.md  # Reference for IDE config paths
└── SETUP-WINDOWS.md     # Detailed Windows setup guide
```

## 🎯 Why Use This?

**Problem:** Most AI editors (GitHub Copilot, Windsurf, Cline) only support **global** MCP configs in your user home directory. This makes it hard to have different MCP servers for different projects.

**Solution:** This launcher acts as a **proxy** that:
1. Gets installed once globally in your home directory
2. Dynamically reads **project-specific** configs from `.mcp/servers/`
3. Launches the appropriate MCP server for each project

## 🔍 Find Your IDE's Config Path (Start Here!)

**Before setting up, detect which IDEs are installed and find their MCP config paths:**

```bash
# Using Task (recommended)
task mcp:detect-ides          # Python version
task mcp:detect-ides:win      # PowerShell version (Windows)

# Or run directly
python .mcp/scripts/detect-ide-configs.py
pwsh -File .mcp/scripts/detect-ide-configs.ps1  # Windows
```

This will show you:
- ✅ Which IDEs are installed
- 📁 Where each IDE stores its global MCP config
- ✓ Whether the config file already exists
- 📋 Whether the IDE supports project-level configs

**For detailed IDE config path reference, see:** [IDE-CONFIG-PATHS.md](./IDE-CONFIG-PATHS.md)

---

## 🚀 Quick Setup (Automated)

**Windows:**
```powershell
task mcp:setup:win
# Or: .\.mcp\setup-windows.ps1
```

**Linux/Mac:**
```bash
task mcp:setup:unix
# Or: bash .mcp/setup-unix.sh
```

This automated setup will:
1. Detect your system and installed tools
2. Install the launcher globally
3. Prompt for environment variables
4. Configure your IDE's global MCP config
5. Verify the setup

---

## 🛠️ Manual Setup Instructions

If you prefer to set up manually or need more control:

### Step 1: Install the Launcher Globally

Choose either Python or Node.js version:

#### Option A: Python Launcher (Recommended)

**Linux/Mac:**
```bash
# Create launcher directory
mkdir -p ~/.local/mcp-launcher

# Copy the launcher script
cp .mcp/launcher/launcher.py ~/.local/mcp-launcher/

# Make it executable
chmod +x ~/.local/mcp-launcher/launcher.py
```

**Windows:**
```powershell
# Create launcher directory
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.local\mcp-launcher"

# Copy the launcher script
Copy-Item .mcp\launcher\launcher.py "$env:USERPROFILE\.local\mcp-launcher\"
```

#### Option B: Node.js Launcher

**Linux/Mac:**
```bash
mkdir -p ~/.local/mcp-launcher
cp .mcp/launcher/launcher.js ~/.local/mcp-launcher/
chmod +x ~/.local/mcp-launcher/launcher.js
```

**Windows:**
```powershell
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.local\mcp-launcher"
Copy-Item .mcp\launcher\launcher.js "$env:USERPROFILE\.local\mcp-launcher\"
```

### Step 2: Configure Your Editor

#### GitHub Copilot

Edit your global MCP config:

**Linux/Mac:** `~/.config/github-copilot/mcp/servers.json`
**Windows:** `%USERPROFILE%\.config\github-copilot\mcp\servers.json`

```json
{
  "github": {
    "command": "python3",
    "args": [
      "~/.local/mcp-launcher/launcher.py",
      "github"
    ]
  },
  "memory": {
    "command": "python3",
    "args": [
      "~/.local/mcp-launcher/launcher.py",
      "memory-qdrant"
    ]
  }
}
```

**Windows:** Use full paths:
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

#### Cline / Windsurf

These editors natively support project-level MCP configs!

**Cline:** Edit `.cline/mcp.json`:
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

**Windsurf:** Uses the same `.mcp/servers/` format automatically.

### Step 3: Verify Setup

1. **Open this project** in your editor
2. **Restart the editor** (or reload the MCP connection)
3. **Check MCP status** - you should see the configured servers connected
4. **Test functionality** - try asking the AI to use GitHub integration

## 📝 Creating New Server Configs

1. Copy `_template.json` to a new file:
   ```bash
   cp .mcp/servers/_template.json .mcp/servers/my-server.json
   ```

2. Edit the config:
   ```json
   {
     "name": "my-server",
     "description": "My custom MCP server",
     "command": "npx",
     "args": ["-y", "@org/my-mcp-server"],
     "env": {
       "API_KEY": "${MY_API_KEY}"
     }
   }
   ```

3. Add to your editor's global config:
   ```json
   {
     "my-server": {
       "command": "python3",
       "args": ["~/.local/mcp-launcher/launcher.py", "my-server"]
     }
   }
   ```

## 🔧 Configuration Schema

Each server config in `.mcp/servers/*.json` follows this schema:

```json
{
  "name": "server-name",          // Required: Server identifier
  "description": "...",            // Optional: Human-readable description
  "command": "npx",                // Required: Executable command
  "args": ["-y", "..."],          // Optional: Command arguments
  "env": {                         // Optional: Environment variables
    "API_KEY": "${MY_API_KEY}"    // Supports ${VAR} expansion
  },
  "cwd": "${PROJECT_ROOT}"        // Optional: Working directory
}
```

### Environment Variable Expansion

The launcher supports these variable formats:

- `${VAR_NAME}` - Expands to environment variable
- `$VAR_NAME` - Also supported
- `${PROJECT_ROOT}` - Special variable for project root path

Example:
```json
{
  "command": "node",
  "args": ["${PROJECT_ROOT}/scripts/mcp-server.js"],
  "env": {
    "API_URL": "https://api.example.com",
    "TOKEN": "${MY_API_TOKEN}"
  }
}
```

## 📚 Available Server Configs

This project includes these pre-configured servers:

### `github.json`
GitHub integration - manage repos, issues, PRs
```bash
# Requires: GITHUB_PERSONAL_ACCESS_TOKEN environment variable
export GITHUB_PERSONAL_ACCESS_TOKEN="ghp_..."
```

### `sequential-thinking.json`
Enable step-by-step reasoning for complex problems
- No configuration needed

### `memory-qdrant.json`
Long-term memory storage with Qdrant vector database
```bash
# Requires: Qdrant running locally or remotely
docker run -p 6333:6333 qdrant/qdrant
export QDRANT_API_KEY="your-api-key"  # Optional for local
```

## 🐛 Troubleshooting

### Launcher not found

**Error:** `Command not found: ~/.local/mcp-launcher/launcher.py`

**Fix:** Use absolute path in editor config:
```json
{
  "github": {
    "command": "python3",
    "args": ["/home/username/.local/mcp-launcher/launcher.py", "github"]
  }
}
```

### Server config not found

**Error:** `No .mcp/servers directory found`

**Fix:** Ensure you're in the project directory and `.mcp/servers/` exists:
```bash
pwd  # Should be project root
ls -la .mcp/servers/  # Should show config files
```

### Environment variables not expanding

**Error:** Server starts but can't connect due to missing credentials

**Fix:** Ensure environment variables are set in your shell:
```bash
# Add to ~/.bashrc or ~/.zshrc
export GITHUB_PERSONAL_ACCESS_TOKEN="ghp_..."
export QDRANT_API_KEY="..."
```

### Wrong server starting

**Error:** Different server than expected starts

**Fix:** Check the server name in your editor's global config matches the filename:
- Config file: `.mcp/servers/github.json`
- Editor config arg: `"github"` (not `"github.json"`)

## 🔍 How It Works (Technical Details)

### The Indirection Pattern

```
Editor
  └─→ Global Config: ~/.config/editor/mcp/servers.json
       └─→ Launcher: ~/.local/mcp-launcher/launcher.py
            └─→ Project Config: .mcp/servers/github.json
                 └─→ Actual Server: npx @modelcontextprotocol/server-github
```

### Key Insight

The launcher runs in the **current working directory** of the editor, which is your project root. This allows it to find project-specific configs using `process.cwd()` or `Path.cwd()`.

### Process Replacement

The launcher uses `os.execvpe()` (Python) or `spawn()` (Node.js) to replace itself with the actual MCP server process. This ensures:
- No extra process overhead
- Direct stdio communication between editor and server
- Clean process tree

## 📖 Additional Resources

- [MCP Documentation](https://modelcontextprotocol.io/)
- [Available MCP Servers](https://github.com/modelcontextprotocol/servers)
- [Creating Custom MCP Servers](https://modelcontextprotocol.io/docs/guides/creating-servers)

## 🤝 Contributing

To add new server configs to this project:

1. Create config in `.mcp/servers/your-server.json`
2. Test with the launcher
3. Document any required environment variables
4. Update this README's "Available Server Configs" section

## 📄 License

Same license as the parent project.
