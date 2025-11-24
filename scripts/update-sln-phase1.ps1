#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Update solution file for Phase 1 changes
#>

$ErrorActionPreference = "Stop"

$slnPath = "dotnet\PigeonPea.sln"
$content = Get-Content $slnPath -Raw

# Update app-essential contract paths
$content = $content -replace 'app-essential\\contracts\\PigeonPea\.Analytics\.Contracts\\', 'app-essential\contracts\src\PigeonPea.Analytics.Contracts\'
$content = $content -replace 'app-essential\\contracts\\PigeonPea\.Audio\.Contracts\\', 'app-essential\contracts\src\PigeonPea.Audio.Contracts\'
$content = $content -replace 'app-essential\\contracts\\PigeonPea\.Config\.Contracts\\', 'app-essential\contracts\src\PigeonPea.Config.Contracts\'
$content = $content -replace 'app-essential\\contracts\\PigeonPea\.Contracts\\', 'app-essential\contracts\src\PigeonPea.Contracts\'
$content = $content -replace 'app-essential\\contracts\\PigeonPea\.Diagnostic\.Contracts\\', 'app-essential\contracts\src\PigeonPea.Diagnostic.Contracts\'
$content = $content -replace 'app-essential\\contracts\\PigeonPea\.Hud\.Contracts\\', 'app-essential\contracts\src\PigeonPea.Hud.Contracts\'
$content = $content -replace 'app-essential\\contracts\\PigeonPea\.Input\.Contracts\\', 'app-essential\contracts\src\PigeonPea.Input.Contracts\'
$content = $content -replace 'app-essential\\contracts\\PigeonPea\.Profiling\.Contracts\\', 'app-essential\contracts\src\PigeonPea.Profiling.Contracts\'
$content = $content -replace 'app-essential\\contracts\\PigeonPea\.Recording\.Contracts\\', 'app-essential\contracts\src\PigeonPea.Recording.Contracts\'
$content = $content -replace 'app-essential\\contracts\\PigeonPea\.Resource\.Contracts\\', 'app-essential\contracts\src\PigeonPea.Resource.Contracts\'

# Update misplaced test
$content = $content -replace 'app-essential\\core\\tests\\PigeonPea\.Contracts\.Recording\.Tests\\', 'app-essential\contracts\tests\PigeonPea.Contracts.Recording.Tests\'

# Update game-essential contract paths
$content = $content -replace 'game-essential\\contracts\\PigeonPea\.Compute\.Contracts\\', 'game-essential\contracts\src\PigeonPea.Compute.Contracts\'
$content = $content -replace 'game-essential\\contracts\\PigeonPea\.Dungeon\.Contracts\\', 'game-essential\contracts\src\PigeonPea.Dungeon.Contracts\'
$content = $content -replace 'game-essential\\contracts\\PigeonPea\.Game\.Contracts\\', 'game-essential\contracts\src\PigeonPea.Game.Contracts\'
$content = $content -replace 'game-essential\\contracts\\PigeonPea\.Language\.Contracts\\', 'game-essential\contracts\src\PigeonPea.Language.Contracts\'
$content = $content -replace 'game-essential\\contracts\\PigeonPea\.Map\.Contracts\\', 'game-essential\contracts\src\PigeonPea.Map.Contracts\'
$content = $content -replace 'game-essential\\contracts\\PigeonPea\.Navigation\.Contracts\\', 'game-essential\contracts\src\PigeonPea.Navigation.Contracts\'
$content = $content -replace 'game-essential\\contracts\\PigeonPea\.Rendering\.Contracts\\', 'game-essential\contracts\src\PigeonPea.Rendering.Contracts\'
$content = $content -replace 'game-essential\\contracts\\PigeonPea\.Scene\.Contracts\\', 'game-essential\contracts\src\PigeonPea.Scene.Contracts\'
$content = $content -replace 'game-essential\\contracts\\PigeonPea\.Time\.Contracts\\', 'game-essential\contracts\src\PigeonPea.Time.Contracts\'

Set-Content -Path $slnPath -Value $content -NoNewline

Write-Host "✓ Solution file updated" -ForegroundColor Green
