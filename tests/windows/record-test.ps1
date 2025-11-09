# FFmpeg Window Recording Script for Pigeon Pea Windows App
# This script starts the Windows app and records it with FFmpeg for visual testing
#
# Usage:
#   .\record-test.ps1 -TestName "my-test" -Duration 10
#   .\record-test.ps1 -TestName "particle-effect" -Duration 5 -OutputDir "custom-recordings"
#
# Requirements:
#   - FFmpeg must be installed and available in PATH
#   - .NET SDK 9.0+ must be installed
#   - Windows operating system (uses gdigrab)

param(
    [Parameter(Mandatory=$true)]
    [string]$TestName,

    [Parameter(Mandatory=$false)]
    [int]$Duration = 10,

    [Parameter(Mandatory=$false)]
    [string]$OutputDir = "recordings",

    [Parameter(Mandatory=$false)]
    [string]$WindowTitle = "Pigeon Pea - Dungeon Crawler",

    [Parameter(Mandatory=$false)]
    [string]$WindowsAppPath = "../../dotnet/windows-app",

    [Parameter(Mandatory=$false)]
    [int]$StartupWaitSeconds = 3
)

# Script configuration
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "FFmpeg Window Recording Test" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test Name:       $TestName" -ForegroundColor White
Write-Host "Duration:        $Duration seconds" -ForegroundColor White
Write-Host "Window Title:    $WindowTitle" -ForegroundColor White
Write-Host "Output Dir:      $OutputDir" -ForegroundColor White
Write-Host ""

# Validate FFmpeg installation
try {
    $ffmpegVersion = ffmpeg -version 2>&1 | Select-Object -First 1
    Write-Host "✓ FFmpeg is installed" -ForegroundColor Green
    Write-Host "  $ffmpegVersion" -ForegroundColor Gray
}
catch {
    Write-Host "ERROR: FFmpeg is not installed or not in PATH." -ForegroundColor Red
    Write-Host "Please install FFmpeg:" -ForegroundColor Yellow
    Write-Host "  - Download from: https://ffmpeg.org/download.html" -ForegroundColor Gray
    Write-Host "  - Or use chocolatey: choco install ffmpeg" -ForegroundColor Gray
    exit 1
}

# Validate .NET SDK installation
try {
    $dotnetVersion = dotnet --version 2>&1
    Write-Host "✓ .NET SDK is installed ($dotnetVersion)" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: .NET SDK is not installed or not in PATH." -ForegroundColor Red
    Write-Host "Please install .NET SDK 9.0+ from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Create output directory
$fullOutputDir = Join-Path $scriptDir $OutputDir
if (-not (Test-Path $fullOutputDir)) {
    New-Item -ItemType Directory -Path $fullOutputDir -Force | Out-Null
    Write-Host "✓ Created output directory: $fullOutputDir" -ForegroundColor Green
}
else {
    Write-Host "✓ Output directory exists: $fullOutputDir" -ForegroundColor Green
}

# Construct paths
$recordingPath = Join-Path $fullOutputDir "$TestName.mp4"
$windowsAppFullPath = Join-Path $scriptDir $WindowsAppPath
$windowsAppFullPath = Resolve-Path $windowsAppFullPath

Write-Host ""
Write-Host "Starting Windows application..." -ForegroundColor Yellow

# Start the Windows application
try {
    $appProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run" `
        -WorkingDirectory $windowsAppFullPath `
        -PassThru `
        -WindowStyle Normal

    Write-Host "✓ Windows app started (PID: $($appProcess.Id))" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Failed to start Windows application" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Wait for window to initialize
Write-Host "Waiting $StartupWaitSeconds seconds for window to appear..." -ForegroundColor Yellow
Start-Sleep -Seconds $StartupWaitSeconds

# Check if the app process is still running
if ($appProcess.HasExited) {
    Write-Host "ERROR: Windows application exited unexpectedly" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Window initialized" -ForegroundColor Green
Write-Host ""
Write-Host "Starting FFmpeg recording..." -ForegroundColor Yellow
Write-Host "Recording for $Duration seconds..." -ForegroundColor Yellow

# Start FFmpeg recording
try {
    $ffmpegArgs = @(
        "-f", "gdigrab",
        "-i", "title=$WindowTitle",
        "-t", "$Duration",
        "-framerate", "30",
        "-y",
        "`"$recordingPath`""
    )

    $ffmpegProcess = Start-Process -FilePath "ffmpeg" `
        -ArgumentList $ffmpegArgs `
        -PassThru `
        -NoNewWindow `
        -RedirectStandardError "$fullOutputDir\$TestName-ffmpeg-log.txt"

    Write-Host "✓ FFmpeg started (PID: $($ffmpegProcess.Id))" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Failed to start FFmpeg" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red

    # Clean up: stop the app
    if (-not $appProcess.HasExited) {
        Stop-Process -Id $appProcess.Id -Force
    }
    exit 1
}

# Wait for FFmpeg to complete recording
try {
    Wait-Process -Id $ffmpegProcess.Id -Timeout ($Duration + 10)
    Write-Host "✓ Recording completed" -ForegroundColor Green
}
catch {
    Write-Host "WARNING: FFmpeg did not complete within expected time" -ForegroundColor Yellow
}

# Stop the Windows application
Write-Host ""
Write-Host "Stopping Windows application..." -ForegroundColor Yellow
try {
    if (-not $appProcess.HasExited) {
        Stop-Process -Id $appProcess.Id -Force
        Write-Host "✓ Windows app stopped" -ForegroundColor Green
    }
    else {
        Write-Host "✓ Windows app already exited" -ForegroundColor Green
    }
}
catch {
    Write-Host "WARNING: Failed to stop Windows application gracefully" -ForegroundColor Yellow
}

# Verify recording file exists
Write-Host ""
if (Test-Path $recordingPath) {
    $fileSize = (Get-Item $recordingPath).Length
    $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "Recording Complete!" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Recording saved to: $recordingPath" -ForegroundColor White
    Write-Host "File size: $fileSizeMB MB" -ForegroundColor White
    Write-Host ""

    # Check if file size is reasonable (> 0)
    if ($fileSize -eq 0) {
        Write-Host "WARNING: Recording file is empty (0 bytes)" -ForegroundColor Yellow
        Write-Host "This may indicate FFmpeg failed to capture the window." -ForegroundColor Yellow
        Write-Host "Check the FFmpeg log: $fullOutputDir\$TestName-ffmpeg-log.txt" -ForegroundColor Gray
        exit 1
    }
}
else {
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "Recording Failed!" -ForegroundColor Red
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "ERROR: Recording file was not created: $recordingPath" -ForegroundColor Red
    Write-Host "Check the FFmpeg log: $fullOutputDir\$TestName-ffmpeg-log.txt" -ForegroundColor Gray
    exit 1
}

Write-Host "Test completed successfully!" -ForegroundColor Green
exit 0
