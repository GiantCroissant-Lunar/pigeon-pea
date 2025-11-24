#!/usr/bin/env pwsh
<#
.SYNOPSIS
    RFC-059 Phase 1: Reorganize Contracts
.DESCRIPTION
    Adds src/ and tests/ subdirectories to all contracts/ folders
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Host "`nPhase 1: Reorganize Contracts" -ForegroundColor Cyan
Write-Host "===============================`n" -ForegroundColor Cyan

# App-essential contracts
Write-Host "Processing app-essential/contracts..." -ForegroundColor Yellow
Set-Location "dotnet\app-essential\contracts"

New-Item -ItemType Directory -Path "src" -Force | Out-Null
New-Item -ItemType Directory -Path "tests" -Force | Out-Null

$projects = @(
    "PigeonPea.Analytics.Contracts",
    "PigeonPea.Audio.Contracts",
    "PigeonPea.Config.Contracts",
    "PigeonPea.Contracts",
    "PigeonPea.Diagnostic.Contracts",
    "PigeonPea.Hud.Contracts",
    "PigeonPea.Input.Contracts",
    "PigeonPea.Profiling.Contracts",
    "PigeonPea.Recording.Contracts",
    "PigeonPea.Resource.Contracts"
)

foreach ($project in $projects) {
    if (Test-Path $project) {
        git mv $project "src\$project"
        Write-Host "  ✓ Moved $project to src/" -ForegroundColor Green
    }
}

Set-Location "..\..\.."

# Move misplaced test
Write-Host "`nMoving misplaced test..." -ForegroundColor Yellow
$testPath = "dotnet\app-essential\core\tests\PigeonPea.Contracts.Recording.Tests"
if (Test-Path $testPath) {
    git mv $testPath "dotnet\app-essential\contracts\tests\PigeonPea.Contracts.Recording.Tests"
    Write-Host "  ✓ Moved PigeonPea.Contracts.Recording.Tests" -ForegroundColor Green
}

# Game-essential contracts
Write-Host "`nProcessing game-essential/contracts..." -ForegroundColor Yellow
Set-Location "dotnet\game-essential\contracts"

New-Item -ItemType Directory -Path "src" -Force | Out-Null
New-Item -ItemType Directory -Path "tests" -Force | Out-Null

$projects = @(
    "PigeonPea.Compute.Contracts",
    "PigeonPea.Dungeon.Contracts",
    "PigeonPea.Game.Contracts",
    "PigeonPea.Language.Contracts",
    "PigeonPea.Map.Contracts",
    "PigeonPea.Navigation.Contracts",
    "PigeonPea.Rendering.Contracts",
    "PigeonPea.Scene.Contracts",
    "PigeonPea.Time.Contracts"
)

foreach ($project in $projects) {
    if (Test-Path $project) {
        git mv $project "src\$project"
        Write-Host "  ✓ Moved $project to src/" -ForegroundColor Green
    }
}

Set-Location "..\..\.."

Write-Host "`n✓ Phase 1 Complete!" -ForegroundColor Green
Write-Host "Run dotnet build to update project references.`n" -ForegroundColor Cyan
