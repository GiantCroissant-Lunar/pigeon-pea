# Launch Rio Terminal with the latest built dev-tool-server binary
# This script finds the latest version in build/_artifacts/ and runs it in Rio
param(
    [string]$RioDir = "D:\lunar-snake\tools\rio",
    [string]$Version = "",  # Optional: specify a version, otherwise use latest
    [switch]$RestoreConfig
)

$ErrorActionPreference = "Stop"

# Resolve paths
$scriptDir = $PSScriptRoot
$buildRustDir = Split-Path $scriptDir -Parent
$buildDir = Split-Path $buildRustDir -Parent
$projectRoot = Split-Path $buildDir -Parent
$artifactsDir = Join-Path $buildDir "_artifacts"

# Find the latest version or use specified version
if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-Host "Finding latest version in artifacts..." -ForegroundColor Cyan

    if (-Not (Test-Path $artifactsDir)) {
        Write-Error "Artifacts directory not found: $artifactsDir"
        Write-Host "Run 'task rust:publish' first to build artifacts" -ForegroundColor Yellow
        exit 1
    }

    # Get all version directories, sort by name (semantic versioning), and pick the latest
    $versionDirs = Get-ChildItem -Path $artifactsDir -Directory |
                   Where-Object { Test-Path (Join-Path $_.FullName "dev-tool-server\dev-tool-server.exe") } |
                   Sort-Object Name -Descending

    if ($versionDirs.Count -eq 0) {
        Write-Error "No built artifacts found in $artifactsDir"
        Write-Host "Run 'task rust:publish' first to build artifacts" -ForegroundColor Yellow
        exit 1
    }

    $Version = $versionDirs[0].Name
    Write-Host "Latest version found: $Version" -ForegroundColor Green
} else {
    Write-Host "Using specified version: $Version" -ForegroundColor Cyan
}

# Construct path to the binary
$binaryPath = Join-Path $artifactsDir "$Version\dev-tool-server\dev-tool-server.exe"

if (-Not (Test-Path $binaryPath)) {
    Write-Error "Binary not found at: $binaryPath"
    exit 1
}

Write-Host "Binary location: $binaryPath" -ForegroundColor Cyan

# Verify Rio executable exists
$RioExe = Join-Path $RioDir "rio-portable-x86_64.exe"
if (-Not (Test-Path $RioExe)) {
    Write-Error "Rio executable not found at: $RioExe"
    exit 1
}

# Setup Rio config using $RIO_CONFIG_HOME
$artifactConfigDir = Join-Path $artifactsDir "$Version\dev-tool-server"
$artifactConfig = Join-Path $artifactConfigDir "config.toml"
$ProjectConfigTemplate = Join-Path $projectRoot "projects\dungeon\rust\rio-config.toml"

if (-Not (Test-Path $ProjectConfigTemplate)) {
    Write-Error "Project rio-config.toml template not found at: $ProjectConfigTemplate"
    exit 1
}

Write-Host "Creating Rio config for version $Version in artifact directory..." -ForegroundColor Cyan

# Read the template config
$configContent = Get-Content $ProjectConfigTemplate -Raw

# Update the window title to include version
$configContent = $configContent -replace '(title = ")[^"]*(")', "`$1Dungeon Dev Server - v$Version`$2"

# Update the shell args to run the binary directly instead of cargo run
# Also add -NoProfile to suppress PowerShell profile errors (like posh-git)
$binaryPathEscaped = $binaryPath -replace '\\', '\\'
$configContent = $configContent -replace 'args = \[[\s\S]*?\]', @"
args = [
    "-NoProfile",
    "-NoExit",
    "-Command",
    "& '$binaryPathEscaped'"
]
"@

# Update working directory to the artifacts directory so relative paths work
$artifactWorkingDir = Join-Path $artifactsDir "$Version\dev-tool-server"
$artifactWorkingDirEscaped = $artifactWorkingDir -replace '\\', '\\'
$configContent = $configContent -replace 'working_directory = "[^"]*"', "working_directory = `"$artifactWorkingDirEscaped`""

# Fix font paths: convert relative paths to absolute paths pointing to Rio tools directory
# This fixes the "Font(s) not found" error when working directory changes
$rioFontsPath1 = Join-Path $RioDir "JetBrainsMono"
$rioFontsPath2 = Join-Path $RioDir "Microsoft JhengHei Regular"

# Use platform's directory separator and escape for TOML if needed
$separator = [System.IO.Path]::DirectorySeparatorChar
if ($separator -eq '\') {
    # Windows: Escape backslashes for TOML (single escape: \ becomes \\)
    $rioFontsPath1Escaped = $rioFontsPath1 -replace '\\', '\\'
    $rioFontsPath2Escaped = $rioFontsPath2 -replace '\\', '\\'
} else {
    # Linux/macOS: Forward slashes need no escaping in TOML
    $rioFontsPath1Escaped = $rioFontsPath1
    $rioFontsPath2Escaped = $rioFontsPath2
}

# Replace the relative font paths with absolute paths
$configContent = $configContent -replace 'additional-dirs = \[[\s\S]*?\]', @"
additional-dirs = [
    "$rioFontsPath1Escaped",
    "$rioFontsPath2Escaped"
]
"@

# Remove problematic fonts from extras array that cause "Font(s) not found" errors
# Keep only fonts that are guaranteed to work on Windows
$configContent = $configContent -replace 'extras = \[[\s\S]*?\]', @"
extras = [
    { family = "Consolas" }
]
"@

# Save config directly to artifact directory
$configContent | Out-File -FilePath $artifactConfig -Encoding UTF8

Write-Host "Config created at: $artifactConfig" -ForegroundColor Gray

# Set RIO_CONFIG_HOME to artifact directory so Rio reads config from there
$env:RIO_CONFIG_HOME = $artifactConfigDir

# Launch Rio with the custom config location
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Launching Rio Terminal..." -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Green
Write-Host "Binary: $binaryPath" -ForegroundColor Green
Write-Host "Config: $artifactConfig" -ForegroundColor Cyan
Write-Host "RIO_CONFIG_HOME: $artifactConfigDir" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

& $RioExe

Write-Host "Done!" -ForegroundColor Green
