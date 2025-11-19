# Dungeon Dev Server Launcher for Rio Terminal
# PowerShell script to launch Rio terminal with optimal configuration

param(
    [string]$ConfigPath = "$PSScriptRoot\rio-config.toml",
    [string]$BindAddress = "0.0.0.0:5007",
    [string]$Profile = "development",
    [switch]$Debug = $false,
    [switch]$Headless = $false
)

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Check if Rio exists
$RioPath = "D:\lunar-snake\tools\rio\rio-portable-x86_64.exe"
if (-not (Test-Path $RioPath)) {
    Write-Error "❌ Rio terminal not found at: $RioPath"
    Write-Error "Please install Rio terminal or update path in this script"
    exit 1
}

# Check if Rust/Cargo is available
try {
    $cargoVersion = cargo --version 2>$null
    if (-not $cargoVersion) {
        throw "Cargo not found"
    }
} catch {
    Write-Error "❌ Rust/Cargo not found. Please install Rust toolchain."
    Write-Error "Visit: https://rustup.rs/"
    exit 1
}

# Build server first
Write-Host "🔨 Building Dungeon Dev Server..." -ForegroundColor Cyan
Set-Location $ScriptDir

$buildArgs = @("run", "--release", "--bin", "dev-tool-server")
if ($Debug) {
    $buildArgs = @("run", "--bin", "dev-tool-server")
}

try {
    & cargo check $buildArgs 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
} catch {
    Write-Error "❌ Build failed. Please check the code for errors."
    exit 1
}

# Prepare Rio command
$RioArgs = @(
    "--config", $ConfigPath,
    "--title", "Dungeon Dev Server",
    "--profile", $Profile
)

# Add custom arguments for the server
$ServerArgs = @()
if ($BindAddress -ne "0.0.0.0:5007") {
    $ServerArgs += @("--bind", $BindAddress)
}

if ($Debug) {
    $ServerArgs += @("--debug")
}

if ($Headless) {
    $ServerArgs += @("--headless")
}

# Display startup message
Write-Host "🎮 Starting Dungeon Dev Server..." -ForegroundColor Green
Write-Host "📁 Config: $ConfigPath" -ForegroundColor Gray
Write-Host "🌐 Bind: $BindAddress" -ForegroundColor Gray
Write-Host "👤 Profile: $Profile" -ForegroundColor Gray
Write-Host "🖥️  Terminal: Rio" -ForegroundColor Gray

# Launch Rio with the server
try {
    Write-Host "🚀 Launching Rio terminal..." -ForegroundColor Yellow

    # Construct the full command
    $CargoCommand = "cargo"
    $CargoArgs = @("run", "--release", "--bin", "dev-tool-server") + $ServerArgs

    # Set environment variables for Rio
    $env:RIO_CONFIG = $ConfigPath
    $env:RIO_PROFILE = $Profile

    # Start Rio
    & $RioPath @RioArgs --command "$CargoCommand $($CargoArgs -join ' ')"

} catch {
    Write-Error "❌ Failed to launch Rio terminal: $_"
    exit 1
}

Write-Host "✅ Server stopped." -ForegroundColor Green
