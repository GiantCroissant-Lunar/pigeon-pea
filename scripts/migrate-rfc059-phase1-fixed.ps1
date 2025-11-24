#!/usr/bin/env pwsh
<#
.SYNOPSIS
    RFC-059 Phase 1: Reorganize Contracts (Fixed)
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

# Create temporary directory and move all contracts into it
$contracts = @(
    "dotnet/app-essential/contracts/PigeonPea.Analytics.Contracts",
    "dotnet/app-essential/contracts/PigeonPea.Audio.Contracts",
    "dotnet/app-essential/contracts/PigeonPea.Config.Contracts",
    "dotnet/app-essential/contracts/PigeonPea.Contracts",
    "dotnet/app-essential/contracts/PigeonPea.Diagnostic.Contracts",
    "dotnet/app-essential/contracts/PigeonPea.Hud.Contracts",
    "dotnet/app-essential/contracts/PigeonPea.Input.Contracts",
    "dotnet/app-essential/contracts/PigeonPea.Profiling.Contracts",
    "dotnet/app-essential/contracts/PigeonPea.Recording.Contracts",
    "dotnet/app-essential/contracts/PigeonPea.Resource.Contracts"
)

New-Item -ItemType Directory -Path "dotnet/app-essential/contracts/_temp_src" -Force | Out-Null
foreach ($contract in $contracts) {
    if (Test-Path $contract) {
        $name = Split-Path -Leaf $contract
        git mv $contract "dotnet/app-essential/contracts/_temp_src/$name"
        Write-Host "  ✓ Staged $name" -ForegroundColor Green
    }
}

git mv "dotnet/app-essential/contracts/_temp_src" "dotnet/app-essential/contracts/src"
Write-Host "  ✓ Created src/ directory" -ForegroundColor Green

# Create tests directory and move misplaced test
New-Item -ItemType Directory -Path "dotnet/app-essential/contracts/tests" -Force | Out-Null
if (Test-Path "dotnet/app-essential/core/tests/PigeonPea.Contracts.Recording.Tests") {
    git mv "dotnet/app-essential/core/tests/PigeonPea.Contracts.Recording.Tests" "dotnet/app-essential/contracts/tests/PigeonPea.Contracts.Recording.Tests"
    Write-Host "  ✓ Moved PigeonPea.Contracts.Recording.Tests" -ForegroundColor Green
}

# Game-essential contracts
Write-Host "`nProcessing game-essential/contracts..." -ForegroundColor Yellow

$contracts = @(
    "dotnet/game-essential/contracts/PigeonPea.Compute.Contracts",
    "dotnet/game-essential/contracts/PigeonPea.Dungeon.Contracts",
    "dotnet/game-essential/contracts/PigeonPea.Game.Contracts",
    "dotnet/game-essential/contracts/PigeonPea.Language.Contracts",
    "dotnet/game-essential/contracts/PigeonPea.Map.Contracts",
    "dotnet/game-essential/contracts/PigeonPea.Navigation.Contracts",
    "dotnet/game-essential/contracts/PigeonPea.Rendering.Contracts",
    "dotnet/game-essential/contracts/PigeonPea.Scene.Contracts",
    "dotnet/game-essential/contracts/PigeonPea.Time.Contracts"
)

New-Item -ItemType Directory -Path "dotnet/game-essential/contracts/_temp_src" -Force | Out-Null
foreach ($contract in $contracts) {
    if (Test-Path $contract) {
        $name = Split-Path -Leaf $contract
        git mv $contract "dotnet/game-essential/contracts/_temp_src/$name"
        Write-Host "  ✓ Staged $name" -ForegroundColor Green
    }
}

git mv "dotnet/game-essential/contracts/_temp_src" "dotnet/game-essential/contracts/src"
Write-Host "  ✓ Created src/ directory" -ForegroundColor Green

# Create tests directory
New-Item -ItemType Directory -Path "dotnet/game-essential/contracts/tests" -Force | Out-Null

Write-Host "`n✓ Phase 1 Complete!" -ForegroundColor Green
Write-Host "Run update-sln-phase1.ps1 to update the solution file.`n" -ForegroundColor Cyan
