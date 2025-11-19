param(
    [string]$RioDir = "D:\lunar-snake\tools\rio",
    [switch]$RestoreConfig
)

$RioExe = Join-Path $RioDir "rio-portable-x86_64.exe"
$RioConfig = Join-Path $RioDir "config.toml"
$RioConfigBackup = Join-Path $RioDir "config.toml.backup"
$ProjectConfig = Join-Path $PSScriptRoot "rio-config.toml"

if (-Not (Test-Path $RioExe)) {
    Write-Error "Rio executable not found at: $RioExe"
    exit 1
}

if (-Not (Test-Path $ProjectConfig)) {
    Write-Error "Project rio-config.toml not found at: $ProjectConfig"
    exit 1
}

Write-Host "Preparing Rio config..." -ForegroundColor Cyan

# Backup existing config if present
if (Test-Path $RioConfig) {
    Copy-Item $RioConfig $RioConfigBackup -Force
    Write-Host "Backed up existing Rio config to config.toml.backup" -ForegroundColor Yellow
}

# Copy project config into Rio directory as active config
Copy-Item $ProjectConfig $RioConfig -Force
Write-Host "Using project-specific rio-config.toml in Rio directory" -ForegroundColor Cyan
Write-Host "RioDir: $RioDir" -ForegroundColor Gray
Write-Host "Config: $RioConfig" -ForegroundColor Gray

# Launch Rio
Write-Host "Launching Rio Terminal with Dungeon Dev Server..." -ForegroundColor Green
& $RioExe

# Optionally restore original config after Rio exits
if ($RestoreConfig -and (Test-Path $RioConfigBackup)) {
    Copy-Item $RioConfigBackup $RioConfig -Force
    Write-Host "Restored original Rio config from backup" -ForegroundColor Green
}
