# Launch Rio Terminal with PigeonPea Console App
# Uses the specified config file and launches the console app from latest artifacts
param(
    [string]$RioDir = "D:\lunar-snake\tools\rio",
    [string]$ConfigFile = "D:\lunar-snake\tools\rio\config-console-app.toml"
)

$ErrorActionPreference = "Stop"

# Resolve paths
$scriptDir = $PSScriptRoot
$nukeDir = Split-Path $scriptDir -Parent
$buildDir = Split-Path $nukeDir -Parent
$projectRoot = Split-Path $buildDir -Parent
$artifactsDir = Join-Path $buildDir "_artifacts"
$latestDir = Join-Path $artifactsDir "latest"
$consoleDir = Join-Path $latestDir "PigeonPea.Console"
$consoleBinary = Join-Path $consoleDir "PigeonPea.Console.exe"

# Verify console app exists
if (-Not (Test-Path $consoleBinary)) {
    Write-Error "Console app not found at: $consoleBinary"
    Write-Host "Run 'task game:build-console' first to build the console app" -ForegroundColor Yellow
    exit 1
}

Write-Host "Console app location: $consoleBinary" -ForegroundColor Cyan

# Verify Rio executable exists
$RioExe = Join-Path $RioDir "rio-portable-x86_64.exe"
if (-Not (Test-Path $RioExe)) {
    Write-Error "Rio executable not found at: $RioExe"
    exit 1
}

# Verify source config exists
if (-Not (Test-Path $ConfigFile)) {
    Write-Error "Config file not found at: $ConfigFile"
    exit 1
}

Write-Host "Using config template: $ConfigFile" -ForegroundColor Cyan

# Create temporary config directory in artifacts
$tempConfigDir = Join-Path $latestDir "PigeonPea.Console\.rio-config"
if (-Not (Test-Path $tempConfigDir)) {
    New-Item -ItemType Directory -Path $tempConfigDir -Force | Out-Null
}

$tempConfig = Join-Path $tempConfigDir "config.toml"

Write-Host "Creating Rio config in artifact directory..." -ForegroundColor Cyan

# Read the source config
$configContent = Get-Content $ConfigFile -Raw

# Update the window title
$configContent = $configContent -replace '(title = ")[^"]*(")', "`$1PigeonPea Console - Latest Build`$2"

# Update the shell args to run the console binary without explicit renderer/dungeon-gen args
# Add -NoProfile to suppress PowerShell profile errors
$binaryPathEscaped = $consoleBinary -replace '\\', '\\'
$configContent = $configContent -replace 'args = \[[\s\S]*?\]', @"
args = [
    "-NoProfile",
    "-NoExit",
    "-Command",
    "& '$binaryPathEscaped'"
]
"@

# Update working directory to the console app directory
$consoleDirEscaped = $consoleDir -replace '\\', '\\'
$configContent = $configContent -replace 'working_directory = "[^"]*"', "working_directory = `"$consoleDirEscaped`""

# Save config to temp location
$configContent | Out-File -FilePath $tempConfig -Encoding UTF8

Write-Host "Config created at: $tempConfig" -ForegroundColor Gray

# Set RIO_CONFIG_HOME to temp config directory so Rio reads config from there
$env:RIO_CONFIG_HOME = $tempConfigDir

# Launch Rio with the custom config location
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Launching Rio Terminal..." -ForegroundColor Green
Write-Host "Console App: $consoleBinary" -ForegroundColor Green
Write-Host "Config: $tempConfig" -ForegroundColor Cyan
Write-Host "RIO_CONFIG_HOME: $tempConfigDir" -ForegroundColor Cyan
Write-Host "Arguments: (none, using config)" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

& $RioExe

Write-Host "Done!" -ForegroundColor Green
