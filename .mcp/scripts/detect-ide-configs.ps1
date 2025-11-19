#!/usr/bin/env pwsh
<#
.SYNOPSIS
    IDE MCP Config Path Detection Script (PowerShell)

.DESCRIPTION
    This script detects which AI coding assistants/IDEs are installed and
    locates their global MCP server configuration paths.

    Supports:
    - GitHub Copilot (VS Code Extension)
    - Cursor
    - Windsurf
    - Cline (VS Code Extension)
    - Zed
    - Claude Desktop (for reference)

.PARAMETER Json
    Output results as JSON

.PARAMETER Verbose
    Show detailed detection info

.PARAMETER IDE
    Check specific IDE only

.EXAMPLE
    .\detect-ide-configs.ps1

.EXAMPLE
    .\detect-ide-configs.ps1 -Json

.EXAMPLE
    .\detect-ide-configs.ps1 -IDE "GitHub Copilot"
#>

param(
    [switch]$Json = $false,
    [switch]$VerboseOutput = $false,
    [ValidateSet("GitHub Copilot", "Cursor", "Windsurf", "Cline", "Zed", "Claude Desktop", "VS Code")]
    [string]$IDE = $null
)

function Write-DebugLog {
    param([string]$Message)
    if ($VerboseOutput) {
        Write-Host "[DEBUG] $Message" -ForegroundColor Gray
    }
}

function Test-PathExists {
    param([string]$Path)

    if ([string]::IsNullOrEmpty($Path)) {
        return @{
            ParentExists = $false
            FileExists = $false
        }
    }

    $pathObj = [System.IO.Path]::GetFullPath($Path)
    $parentPath = Split-Path -Parent $pathObj

    return @{
        ParentExists = Test-Path $parentPath
        FileExists = Test-Path $pathObj
    }
}

function Test-VSCodeInstalled {
    $codeCmd = Get-Command code -ErrorAction SilentlyContinue
    return $null -ne $codeCmd
}

function Get-GitHubCopilotConfig {
    Write-DebugLog "Detecting GitHub Copilot..."

    $configPath = Join-Path $env:USERPROFILE ".config\github-copilot\mcp\servers.json"
    $pathStatus = Test-PathExists $configPath
    $vscodeInstalled = Test-VSCodeInstalled

    return @{
        installed = $vscodeInstalled -or $pathStatus.ParentExists
        global_config_path = $configPath
        config_exists = $pathStatus.FileExists
        supports_project_config = $false
        project_config_path = $null
        notes = "Requires VS Code with GitHub Copilot extension"
    }
}

function Get-CursorConfig {
    Write-DebugLog "Detecting Cursor..."

    $configPath = Join-Path $env:APPDATA "Cursor\User\globalStorage\mcp\servers.json"
    $installPath = Join-Path $env:LOCALAPPDATA "Programs\Cursor"

    $pathStatus = Test-PathExists $configPath
    $installed = (Test-Path $installPath) -or $pathStatus.ParentExists

    return @{
        installed = $installed
        global_config_path = $configPath
        config_exists = $pathStatus.FileExists
        supports_project_config = $true
        project_config_path = ".cursor/mcp.json"
        notes = "Supports both global and project-level MCP configs"
    }
}

function Get-WindsurfConfig {
    Write-DebugLog "Detecting Windsurf..."

    $configPath = Join-Path $env:APPDATA "Windsurf\mcp\servers.json"
    $installPath = Join-Path $env:LOCALAPPDATA "Programs\Windsurf"

    $pathStatus = Test-PathExists $configPath
    $installed = (Test-Path $installPath) -or $pathStatus.ParentExists

    return @{
        installed = $installed
        global_config_path = $configPath
        config_exists = $pathStatus.FileExists
        supports_project_config = $true
        project_config_path = ".mcp/servers/"
        notes = "May support project-level configs (check Windsurf docs)"
    }
}

function Get-ClineConfig {
    Write-DebugLog "Detecting Cline..."

    $vscodeInstalled = Test-VSCodeInstalled

    return @{
        installed = $vscodeInstalled
        global_config_path = $null
        config_exists = $false
        supports_project_config = $true
        project_config_path = ".cline/mcp.json"
        notes = "Uses project-level config only (no global config)"
    }
}

function Get-ZedConfig {
    Write-DebugLog "Detecting Zed..."

    $configPath = Join-Path $env:APPDATA "Zed\settings.json"
    $installPath = Join-Path $env:LOCALAPPDATA "Programs\Zed"

    $pathStatus = Test-PathExists $configPath
    $installed = (Test-Path $installPath) -or $pathStatus.ParentExists

    return @{
        installed = $installed
        global_config_path = $configPath
        config_exists = $pathStatus.FileExists
        supports_project_config = $false
        project_config_path = $null
        notes = "MCP support may vary - check Zed documentation"
    }
}

function Get-ClaudeDesktopConfig {
    Write-DebugLog "Detecting Claude Desktop..."

    $configPath = Join-Path $env:APPDATA "Claude\claude_desktop_config.json"
    $installPath = Join-Path $env:LOCALAPPDATA "Programs\Claude"

    $pathStatus = Test-PathExists $configPath
    $installed = (Test-Path $installPath) -or $pathStatus.ParentExists

    return @{
        installed = $installed
        global_config_path = $configPath
        config_exists = $pathStatus.FileExists
        supports_project_config = $false
        project_config_path = $null
        notes = "Not a coding IDE, included for reference"
    }
}

function Get-VSCodeConfig {
    Write-DebugLog "Detecting VS Code..."

    $configPath = Join-Path $env:APPDATA "Code\User\settings.json"
    $installPath = Join-Path $env:LOCALAPPDATA "Programs\Microsoft VS Code"

    $pathStatus = Test-PathExists $configPath
    $vscodeInstalled = Test-VSCodeInstalled
    $installed = (Test-Path $installPath) -or $pathStatus.ParentExists -or $vscodeInstalled

    return @{
        installed = $installed
        global_config_path = $configPath
        config_exists = $pathStatus.FileExists
        supports_project_config = $true
        project_config_path = ".vscode/settings.json"
        notes = "Base editor - install extensions like Copilot or Cline for MCP support"
    }
}

function Get-AllIDEConfigs {
    $results = @{}

    $detectors = @{
        "GitHub Copilot" = { Get-GitHubCopilotConfig }
        "Cursor" = { Get-CursorConfig }
        "Windsurf" = { Get-WindsurfConfig }
        "Cline" = { Get-ClineConfig }
        "Zed" = { Get-ZedConfig }
        "Claude Desktop" = { Get-ClaudeDesktopConfig }
        "VS Code" = { Get-VSCodeConfig }
    }

    foreach ($ideName in $detectors.Keys) {
        $results[$ideName] = & $detectors[$ideName]
    }

    return $results
}

function Show-ResultsTable {
    param([hashtable]$Results)

    Write-Host ""
    Write-Host ("=" * 80)
    Write-Host "IDE MCP Config Detection Results"
    Write-Host ("=" * 80)
    Write-Host ""

    foreach ($ideName in $Results.Keys) {
        $info = $Results[$ideName]
        $status = if ($info.installed) { "✓ INSTALLED" } else { "✗ Not Found" }
        $color = if ($info.installed) { "Green" } else { "Gray" }

        Write-Host "$ideName: $status" -ForegroundColor $color

        if ($info.installed) {
            if ($info.global_config_path) {
                $configStatus = if ($info.config_exists) { "✓ EXISTS" } else { "⚠ Not Created" }
                Write-Host "  Global Config: $($info.global_config_path)"
                Write-Host "  Config Status: $configStatus"
            }

            if ($info.supports_project_config) {
                Write-Host "  Project Config: $($info.project_config_path) (supported)"
            }

            if ($info.notes) {
                Write-Host "  Notes: $($info.notes)"
            }
        }

        Write-Host ""
    }
}

# Main execution
$results = Get-AllIDEConfigs

# Filter to specific IDE if requested
if ($IDE) {
    $results = @{ $IDE = $results[$IDE] }
}

if ($Json) {
    $results | ConvertTo-Json -Depth 10
} else {
    Show-ResultsTable $results

    # Summary
    $installedCount = ($results.Values | Where-Object { $_.installed }).Count
    Write-Host "Summary: $installedCount/$($results.Count) IDE(s) detected" -ForegroundColor Cyan
    Write-Host ""

    if ($installedCount -gt 0) {
        Write-Host "Next Steps:" -ForegroundColor Cyan
        Write-Host "  1. Run the MCP setup script for your IDE:"
        Write-Host "     .\.mcp\setup-windows.ps1"
        Write-Host "  2. Or manually configure using the paths above"
        Write-Host "  3. See .mcp\README.md for detailed instructions"
        Write-Host ""
    }
}
