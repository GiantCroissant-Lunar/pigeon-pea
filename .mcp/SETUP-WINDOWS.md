# Windows Setup Guide for MCP Project Launcher

Quick setup guide specifically for Windows users.

## Prerequisites

- Python 3.7+ or Node.js 14+ installed
- Git Bash or PowerShell
- GitHub Copilot, Windsurf, or Cline editor

## 🚀 Quick Setup (PowerShell)

### 1. Install the Launcher

Open PowerShell and run:

```powershell
# Create launcher directory
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.local\mcp-launcher"

# Navigate to your project
cd "D:\lunar-snake\personal-work\yokan-projects\pigeon-pea"

# Copy the launcher (Python version)
Copy-Item ".mcp\launcher\launcher.py" "$env:USERPROFILE\.local\mcp-launcher\"

# Verify it was copied
Get-ChildItem "$env:USERPROFILE\.local\mcp-launcher\"
```

### 2. Set Up Environment Variables

Create a PowerShell profile to set environment variables permanently:

```powershell
# Edit your PowerShell profile
notepad $PROFILE

# Add these lines (replace with your actual tokens):
$env:GITHUB_PERSONAL_ACCESS_TOKEN = "ghp_your_token_here"
$env:QDRANT_API_KEY = "your_qdrant_key_here"

# Save and reload
. $PROFILE
```

Alternatively, set them via System Properties:
1. Press `Win + X` → System
2. Click "Advanced system settings"
3. Click "Environment Variables"
4. Add `GITHUB_PERSONAL_ACCESS_TOKEN` and `QDRANT_API_KEY`

### 3. Configure GitHub Copilot

Edit your Copilot MCP config:

**Location:** `%USERPROFILE%\.config\github-copilot\mcp\servers.json`

**Full path example:** `C:\Users\YourUsername\.config\github-copilot\mcp\servers.json`

Create the directory if it doesn't exist:
```powershell
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.config\github-copilot\mcp"
```

Edit the file:
```powershell
notepad "$env:USERPROFILE\.config\github-copilot\mcp\servers.json"
```

Add this configuration (replace `YourUsername` with your actual Windows username):

```json
{
  "github": {
    "command": "python",
    "args": [
      "C:\\Users\\YourUsername\\.local\\mcp-launcher\\launcher.py",
      "github"
    ]
  },
  "memory": {
    "command": "python",
    "args": [
      "C:\\Users\\YourUsername\\.local\\mcp-launcher\\launcher.py",
      "memory-qdrant"
    ]
  },
  "sequential-thinking": {
    "command": "python",
    "args": [
      "C:\\Users\\YourUsername\\.local\\mcp-launcher\\launcher.py",
      "sequential-thinking"
    ]
  }
}
```

**Important:** Use double backslashes (`\\`) in JSON paths on Windows!

### 4. Verify Setup

```powershell
# Check if Python is available
python --version

# Check if launcher exists
Get-Content "$env:USERPROFILE\.local\mcp-launcher\launcher.py" | Select-Object -First 5

# Check project MCP configs
Get-ChildItem ".mcp\servers\" -Filter *.json

# Check environment variables
$env:GITHUB_PERSONAL_ACCESS_TOKEN
```

### 5. Test the Launcher

Test the launcher manually:

```powershell
# Navigate to project
cd "D:\lunar-snake\personal-work\yokan-projects\pigeon-pea"

# Test launching GitHub server
python "$env:USERPROFILE\.local\mcp-launcher\launcher.py" github
```

Press `Ctrl+C` to stop if it starts successfully.

## 🔧 Alternative: Node.js Launcher

If you prefer Node.js over Python:

```powershell
# Copy Node.js launcher instead
Copy-Item ".mcp\launcher\launcher.js" "$env:USERPROFILE\.local\mcp-launcher\"

# Update GitHub Copilot config to use Node:
# Change "python" to "node" and launcher.py to launcher.js
```

## 📝 Editor-Specific Setup

### GitHub Copilot
Already covered above in step 3.

### Windsurf
Windsurf may support `.mcp/` natively. Check their docs first.

### Cline (VS Code Extension)
Edit `.cline/mcp.json` in your project:
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

Cline has native per-project support, so no launcher needed!

## 🐛 Windows-Specific Troubleshooting

### Issue: Python not found

**Error:** `'python' is not recognized as an internal or external command`

**Solutions:**
1. Try `python3` instead of `python`
2. Install Python from [python.org](https://www.python.org/downloads/)
3. Ensure Python is in your PATH

### Issue: Path with spaces fails

**Error:** Launcher fails when project path has spaces

**Fix:** Ensure paths are properly quoted in your scripts:
```json
{
  "command": "python",
  "args": [
    "C:\\Users\\My Name\\.local\\mcp-launcher\\launcher.py",
    "github"
  ]
}
```

### Issue: Permission denied

**Error:** `Access denied` when copying files

**Fix:** Run PowerShell as Administrator:
1. Press `Win + X`
2. Select "Windows PowerShell (Admin)"
3. Run the setup commands again

### Issue: NPX not found (for Node.js MCP servers)

**Error:** `'npx' is not recognized`

**Fix:** Install Node.js from [nodejs.org](https://nodejs.org/)

### Issue: Environment variables not loaded

**Error:** Server starts but authentication fails

**Fix:** Restart your terminal/editor after setting environment variables:
```powershell
# Reload current session
. $PROFILE

# Or close and reopen PowerShell/editor
```

## 🔍 Debugging

Enable debug output by running the launcher manually:

```powershell
# Navigate to project
cd "D:\lunar-snake\personal-work\yokan-projects\pigeon-pea"

# Run with verbose output
python "$env:USERPROFILE\.local\mcp-launcher\launcher.py" github 2>&1 | Tee-Object -FilePath debug.log
```

Check `debug.log` for error messages.

## 📋 Checklist

Use this checklist to verify your setup:

- [ ] Python or Node.js installed and in PATH
- [ ] Launcher copied to `%USERPROFILE%\.local\mcp-launcher\`
- [ ] Environment variables set (GITHUB_PERSONAL_ACCESS_TOKEN, etc.)
- [ ] GitHub Copilot config created at `%USERPROFILE%\.config\github-copilot\mcp\servers.json`
- [ ] Paths use double backslashes in JSON
- [ ] Username in paths matches your Windows username
- [ ] Project has `.mcp\servers\` directory with config files
- [ ] Launcher runs successfully when tested manually
- [ ] Editor restarted after configuration changes

## 🎉 Success!

Once setup is complete:
1. Open this project in your editor
2. Restart the editor
3. You should see MCP servers connected
4. Test by asking the AI to interact with GitHub

## 📚 Next Steps

- Read the main [README.md](.mcp/README.md) for detailed docs
- Create custom server configs in `.mcp/servers/`
- Explore available MCP servers at https://github.com/modelcontextprotocol/servers

## 💡 Tips

1. **Use Git Bash** for Unix-like commands on Windows
2. **Avoid spaces** in installation paths when possible
3. **Use absolute paths** in editor configs (not relative)
4. **Check Windows Defender** if executables are blocked
5. **Keep tokens secure** - never commit them to Git!
