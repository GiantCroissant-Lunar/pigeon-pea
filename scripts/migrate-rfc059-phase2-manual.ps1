#!/usr/bin/env pwsh
<#
.SYNOPSIS
    RFC-059 Phase 2: Rename Plugins to Plugin (Manual approach)
.DESCRIPTION
    Standardizes on Plugin (singular) using simple directory renames and content updates
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

function Rename-PluginProject {
    param(
        [string]$ProjectPath,
        [string]$OldName,
        [string]$NewName
    )

    Write-Host "  Processing $OldName..." -ForegroundColor Yellow

    # 1. Rename .csproj file first
    $csprojPath = Join-Path $ProjectPath "$OldName.csproj"
    if (Test-Path $csprojPath) {
        $newCsprojPath = Join-Path $ProjectPath "$NewName.csproj"
        git mv $csprojPath $newCsprojPath
    }

    # 2. Update content in all files
    Get-ChildItem -Path $ProjectPath -Recurse -Include "*.cs","*.csproj" | ForEach-Object {
        $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
        if ($content) {
            $updated = $content -replace [regex]::Escape($OldName), $NewName
            if ($content -ne $updated) {
                Set-Content -Path $_.FullName -Value $updated -NoNewline
            }
        }
    }

    # 3. Rename directory
    $newPath = $ProjectPath -replace [regex]::Escape($OldName), $NewName
    if ($ProjectPath -ne $newPath) {
        git mv $ProjectPath $newPath
    }

    Write-Host "    ✓ Renamed to $NewName" -ForegroundColor Green

    # 4. Update all references in dotnet directory
    $updated = 0
    Get-ChildItem -Path "dotnet" -Recurse -Include "*.cs","*.csproj" | ForEach-Object {
        $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
        if ($content) {
            $newContent = $content -replace [regex]::Escape($OldName), $NewName
            if ($content -ne $newContent) {
                Set-Content -Path $_.FullName -Value $newContent -NoNewline
                $updated++
            }
        }
    }
    if ($updated -gt 0) {
        Write-Host "    ✓ Updated $updated reference(s)" -ForegroundColor Green
    }

    return $newPath
}

Write-Host "`nPhase 2: Rename Plugins to Plugin" -ForegroundColor Cyan
Write-Host "==================================`n" -ForegroundColor Cyan

# Create tests directory
$appTestsDir = "dotnet\app-essential\plugins\tests"
New-Item -ItemType Directory -Path $appTestsDir -Force | Out-Null
Write-Host "✓ Created app-essential/plugins/tests/`n" -ForegroundColor Green

# App-essential plugins to rename
$appPlugins = @(
    "PigeonPea.Plugins.Analytics.OpenTelemetry",
    "PigeonPea.Plugins.Audio.LibVlc",
    "PigeonPea.Plugins.Config",
    "PigeonPea.Plugins.Diagnostic.OpenTelemetry",
    "PigeonPea.Plugins.Diagnostic.Sentry",
    "PigeonPea.Plugins.Diagnostic.Sentry.Tests",
    "PigeonPea.Plugins.Input.UniInputSystem",
    "PigeonPea.Plugins.Inventory.Basic",
    "PigeonPea.Plugins.Logging.Analytics",
    "PigeonPea.Plugins.Logging.Diagnostic",
    "PigeonPea.Plugins.Logging.Profiling",
    "PigeonPea.Plugins.Logging.Recording",
    "PigeonPea.Plugins.OpenTelemetry.Tests",
    "PigeonPea.Plugins.Profiling.Basic",
    "PigeonPea.Plugins.Profiling.Basic.Tests",
    "PigeonPea.Plugins.Profiling.OpenTelemetry",
    "PigeonPea.Plugins.Profiling.Sentry",
    "PigeonPea.Plugins.Profiling.Sentry.Tests",
    "PigeonPea.Plugins.Recording.Asciinema",
    "PigeonPea.Plugins.Recording.Asciinema.Tests",
    "PigeonPea.Plugins.Recording.Events",
    "PigeonPea.Plugins.Recording.Events.Tests",
    "PigeonPea.Plugins.Recording.FFmpeg",
    "PigeonPea.Plugins.Recording.FFmpeg.Tests",
    "PigeonPea.Plugins.Sentry.Tests"
)

Write-Host "Processing app-essential/plugins..." -ForegroundColor Magenta
$testProjects = @()

foreach ($plugin in $appPlugins) {
    $oldName = $plugin
    $newName = $oldName -replace "\.Plugins\.", ".Plugin."
    $projectPath = "dotnet\app-essential\plugins\src\$oldName"

    if (Test-Path $projectPath) {
        $newPath = Rename-PluginProject -ProjectPath $projectPath -OldName $oldName -NewName $newName

        if ($newName.EndsWith(".Tests")) {
            $testProjects += $newPath
        }
    }
}

# Move test projects
if ($testProjects.Count -gt 0) {
    Write-Host "`nMoving test projects to tests/..." -ForegroundColor Magenta
    foreach ($testPath in $testProjects) {
        $testName = Split-Path -Leaf $testPath
        $destPath = Join-Path $appTestsDir $testName
        if (Test-Path $testPath) {
            git mv $testPath $destPath
            Write-Host "  ✓ Moved $testName" -ForegroundColor Green
        }
    }
}

# Game-essential plugins to rename
$gamePlugins = @(
    "PigeonPea.Plugins.Animation.Basic",
    "PigeonPea.Plugins.Avatar.Basic",
    "PigeonPea.Plugins.Combat.Basic",
    "PigeonPea.Plugins.Inventory.Advanced",
    "PigeonPea.Plugins.Inventory.Basic",
    "PigeonPea.Plugins.Stats.Basic",
    "PigeonPea.Plugins.WorldManagement.Basic"
)

Write-Host "`nProcessing game-essential/plugins..." -ForegroundColor Magenta

foreach ($plugin in $gamePlugins) {
    $oldName = $plugin
    $newName = $oldName -replace "\.Plugins\.", ".Plugin."
    $projectPath = "dotnet\game-essential\plugins\src\$oldName"

    if (Test-Path $projectPath) {
        Rename-PluginProject -ProjectPath $projectPath -OldName $oldName -NewName $newName | Out-Null
    }
}

Write-Host "`n✓ Phase 2 Complete!" -ForegroundColor Green
Write-Host "Run Phase 3 to move orphaned plugins.`n" -ForegroundColor Cyan
