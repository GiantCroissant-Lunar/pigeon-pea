#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automated setup script for MCP Project Launcher on Windows

.DESCRIPTION
    This script automates the installation and configuration of the MCP Project Launcher
    for use with GitHub Copilot and other AI coding assistants on Windows.

.PARAMETER LauncherType
    Type of launcher to install: 'python' or 'node' (default: python)

.PARAMETER SkipEnvVars
    Skip environment variable configuration prompt

.PARAMETER SkipCopilotConfig
    Skip GitHub Copilot configuration

.EXAMPLE
    .\setup-windows.ps1

.EXAMPLE
    .\setup-windows.ps1 -LauncherType node -SkipCopilotConfig

.NOTES
    Author: MCP Project Launcher
    Requires: PowerShell 5.0+, Python 3.7+ or Node.js 14+
#>

param(
    [ValidateSet('python', 'node')]
    [string]$LauncherType = 'python',

    [switch]$SkipEnvVars = $false,

    [switch]$SkipCopilotConfig = $false
)

# Color output functions
function Write-Success { param([string]$Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Info { param([string]$Message) Write-Host "ℹ $Message" -ForegroundColor Cyan }
function Write-Warning { param([string]$Message) Write-Host "⚠ $Message" -ForegroundColor Yellow }
function Write-Error { param([string]$Message) Write-Host "✗ $Message" -ForegroundColor Red }

# Banner
Write-Host @"

╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║      MCP Project Launcher - Windows Setup Script         ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

# Check prerequisites
Write-Info "Checking prerequisites..."

$pythonAvailable = $false
$nodeAvailable = $false

try {
    $pythonVersion = python --version 2>&1
    $pythonAvailable = $true
    Write-Success "Python found: $pythonVersion"
} catch {
    try {
        $pythonVersion = python3 --version 2>&1
        $pythonAvailable = $true
        $LauncherType = 'python'
        Write-Success "Python3 found: $pythonVersion"
    } catch {
        Write-Warning "Python not found"
    }
}

try {
    $nodeVersion = node --version 2>&1
    $nodeAvailable = $true
    Write-Success "Node.js found: $nodeVersion"
} catch {
    Write-Warning "Node.js not found"
}

if (-not $pythonAvailable -and -not $nodeAvailable) {
    Write-Error "Neither Python nor Node.js found!"
    Write-Info "Please install Python (https://python.org) or Node.js (https://nodejs.org)"
    exit 1
}

# Validate launcher type
if ($LauncherType -eq 'python' -and -not $pythonAvailable) {
    if ($nodeAvailable) {
        Write-Warning "Python not found, switching to Node.js launcher"
        $LauncherType = 'node'
    } else {
        Write-Error "Python launcher requested but Python not found"
        exit 1
    }
}

if ($LauncherType -eq 'node' -and -not $nodeAvailable) {
    if ($pythonAvailable) {
        Write-Warning "Node.js not found, switching to Python launcher"
        $LauncherType = 'python'
    } else {
        Write-Error "Node.js launcher requested but Node.js not found"
        exit 1
    }
}

Write-Info "Using $LauncherType launcher"

# Step 1: Create launcher directory
Write-Host ""
Write-Info "Step 1: Installing launcher globally..."

$launcherDir = Join-Path $env:USERPROFILE ".local\mcp-launcher"
New-Item -ItemType Directory -Force -Path $launcherDir | Out-Null
Write-Success "Created directory: $launcherDir"

# Copy launcher file
$launcherExt = if ($LauncherType -eq 'python') { 'py' } else { 'js' }
$launcherFile = "launcher.$launcherExt"
$sourcePath = Join-Path $PSScriptRoot "launcher\$launcherFile"
$destPath = Join-Path $launcherDir $launcherFile

if (-not (Test-Path $sourcePath)) {
    Write-Error "Launcher file not found: $sourcePath"
    exit 1
}

Copy-Item $sourcePath $destPath -Force
Write-Success "Installed launcher: $destPath"

# Step 2: Configure environment variables (optional)
if (-not $SkipEnvVars) {
    Write-Host ""
    Write-Info "Step 2: Environment Variables Setup"
    Write-Info "Some MCP servers require environment variables (e.g., API tokens)"
    Write-Info "You can set them now or skip and set them later"
    Write-Host ""

    $setupEnvVars = Read-Host "Do you want to configure environment variables now? (y/N)"

    if ($setupEnvVars -eq 'y' -or $setupEnvVars -eq 'Y') {
        Write-Host ""
        Write-Info "Common environment variables:"
        Write-Host "  - GITHUB_PERSONAL_ACCESS_TOKEN (for GitHub MCP server)"
        Write-Host "  - QDRANT_API_KEY (for Memory/Qdrant MCP server)"
        Write-Host ""

        $githubToken = Read-Host "Enter GitHub Personal Access Token (or press Enter to skip)"
        if ($githubToken) {
            [System.Environment]::SetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN", $githubToken, "User")
            Write-Success "Set GITHUB_PERSONAL_ACCESS_TOKEN"
        }

        $qdrantKey = Read-Host "Enter Qdrant API Key (or press Enter to skip)"
        if ($qdrantKey) {
            [System.Environment]::SetEnvironmentVariable("QDRANT_API_KEY", $qdrantKey, "User")
            Write-Success "Set QDRANT_API_KEY"
        }

        Write-Warning "Note: You may need to restart your editor for env vars to take effect"
    } else {
        Write-Info "Skipped environment variable setup"
        Write-Info "You can set them later via System Properties or PowerShell profile"
    }
}

# Step 3: Configure GitHub Copilot (optional)
if (-not $SkipCopilotConfig) {
    Write-Host ""
    Write-Info "Step 3: GitHub Copilot Configuration"
    Write-Info "This will create/update your GitHub Copilot MCP config"
    Write-Host ""

    $setupCopilot = Read-Host "Do you want to configure GitHub Copilot now? (y/N)"

    if ($setupCopilot -eq 'y' -or $setupCopilot -eq 'Y') {
        $copilotConfigDir = Join-Path $env:USERPROFILE ".config\github-copilot\mcp"
        New-Item -ItemType Directory -Force -Path $copilotConfigDir | Out-Null

        $copilotConfigPath = Join-Path $copilotConfigDir "servers.json"

        # Escape backslashes for JSON
        $launcherPathJson = $destPath -replace '\\', '\\'

        $command = if ($LauncherType -eq 'python') { 'python' } else { 'node' }

        $copilotConfig = @{
            github = @{
                command = $command
                args = @($launcherPathJson, "github")
            }
            "sequential-thinking" = @{
                command = $command
                args = @($launcherPathJson, "sequential-thinking")
            }
            memory = @{
                command = $command
                args = @($launcherPathJson, "memory-qdrant")
            }
        }

        $copilotConfig | ConvertTo-Json -Depth 10 | Set-Content $copilotConfigPath -Encoding UTF8
        Write-Success "Created GitHub Copilot config: $copilotConfigPath"
        Write-Info "Restart GitHub Copilot for changes to take effect"
    } else {
        Write-Info "Skipped GitHub Copilot configuration"
        Write-Info "You can configure it later manually"
    }
}

# Step 4: Verify setup
Write-Host ""
Write-Info "Step 4: Verifying setup..."

if (Test-Path $destPath) {
    Write-Success "Launcher installed successfully"
}

$projectMcpDir = Join-Path $PSScriptRoot "servers"
if (Test-Path $projectMcpDir) {
    $serverConfigs = Get-ChildItem $projectMcpDir -Filter "*.json" | Where-Object { -not $_.Name.StartsWith('_') }
    Write-Success "Found $($serverConfigs.Count) MCP server configs in project"
    foreach ($config in $serverConfigs) {
        Write-Host "  - $($config.BaseName)" -ForegroundColor Gray
    }
}

# Summary
Write-Host ""
Write-Host @"

╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║                  Setup Complete! 🎉                       ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝

"@ -ForegroundColor Green

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Restart your editor (GitHub Copilot, Windsurf, etc.)"
Write-Host "  2. Open this project"
Write-Host "  3. Check MCP connection status in your editor"
Write-Host "  4. Test by asking AI to use GitHub integration"
Write-Host ""
Write-Host "Documentation:" -ForegroundColor Cyan
Write-Host "  • General: .mcp\README.md"
Write-Host "  • Windows: .mcp\SETUP-WINDOWS.md"
Write-Host ""
Write-Host "Troubleshooting:" -ForegroundColor Cyan
Write-Host "  • Test launcher manually:"
Write-Host "    cd `"$PSScriptRoot`""
Write-Host "    $command `"$destPath`" github"
Write-Host ""

exit 0
