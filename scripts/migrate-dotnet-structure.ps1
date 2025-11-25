#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Implements RFC-059: .NET Project Structure Unification
.DESCRIPTION
    This script performs three structural improvements:
    1. Adds src/ and tests/ subdirectories to all contracts/ folders
    2. Standardizes on Plugin (singular) instead of mixed Plugin/Plugins naming
    3. Reorganizes the orphaned dotnet/plugins/ directory
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$WhatIf,

    [Parameter()]
    [switch]$SkipBackup
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $PSScriptRoot
$dotnetRoot = Join-Path $scriptRoot "dotnet"

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Backup-Solution {
    if ($SkipBackup) {
        Write-Warning "Skipping backup (--SkipBackup flag set)"
        return
    }

    Write-Info "Creating backup of solution file..."
    $slnPath = Join-Path $dotnetRoot "PigeonPea.sln"
    $backupPath = Join-Path $dotnetRoot "PigeonPea.sln.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Copy-Item $slnPath $backupPath
    Write-Success "Backup created: $backupPath"
}

function Update-CsprojFile {
    param(
        [string]$ProjectPath,
        [string]$OldName,
        [string]$NewName
    )

    $csprojFiles = Get-ChildItem -Path $ProjectPath -Filter "*.csproj"
    foreach ($csproj in $csprojFiles) {
        Write-Info "  Updating $($csproj.Name)..."

        $content = Get-Content $csproj.FullName -Raw
        $updated = $content -replace [regex]::Escape($OldName), $NewName

        if ($content -ne $updated) {
            if (-not $WhatIf) {
                Set-Content -Path $csproj.FullName -Value $updated -NoNewline
            }
            Write-Success "    Updated assembly and namespace references"
        }

        # Rename the .csproj file itself if needed
        $newCsprojName = $csproj.Name -replace [regex]::Escape($OldName), $NewName
        if ($csproj.Name -ne $newCsprojName) {
            $newPath = Join-Path $csproj.Directory $newCsprojName
            if (-not $WhatIf) {
                Rename-Item $csproj.FullName $newPath
            }
            Write-Success "    Renamed $($csproj.Name) to $newCsprojName"
        }
    }
}

function Update-SourceFiles {
    param(
        [string]$ProjectPath,
        [string]$OldNamespace,
        [string]$NewNamespace
    )

    $csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -Recurse
    $filesUpdated = 0

    foreach ($csFile in $csFiles) {
        $content = Get-Content $csFile.FullName -Raw
        $updated = $content `
            -replace "namespace\s+$([regex]::Escape($OldNamespace))", "namespace $NewNamespace" `
            -replace "using\s+$([regex]::Escape($OldNamespace))", "using $NewNamespace"

        if ($content -ne $updated) {
            if (-not $WhatIf) {
                Set-Content -Path $csFile.FullName -Value $updated -NoNewline
            }
            $filesUpdated++
        }
    }

    if ($filesUpdated -gt 0) {
        Write-Success "    Updated $filesUpdated source file(s)"
    }
}

function Update-AllReferences {
    param(
        [string]$OldName,
        [string]$NewName
    )

    Write-Info "Updating all references to $OldName..."

    # Update all .csproj files that reference the old name
    $allCsprojFiles = Get-ChildItem -Path $dotnetRoot -Filter "*.csproj" -Recurse
    $referencesUpdated = 0

    foreach ($csproj in $allCsprojFiles) {
        $content = Get-Content $csproj.FullName -Raw
        $updated = $content -replace [regex]::Escape($OldName), $NewName

        if ($content -ne $updated) {
            if (-not $WhatIf) {
                Set-Content -Path $csproj.FullName -Value $updated -NoNewline
            }
            $referencesUpdated++
        }
    }

    if ($referencesUpdated -gt 0) {
        Write-Success "  Updated references in $referencesUpdated project(s)"
    }

    # Update all .cs files that use the old namespace
    $allCsFiles = Get-ChildItem -Path $dotnetRoot -Filter "*.cs" -Recurse
    $usingsUpdated = 0

    foreach ($csFile in $allCsFiles) {
        $content = Get-Content $csFile.FullName -Raw
        $updated = $content -replace "using\s+$([regex]::Escape($OldName))", "using $NewName"

        if ($content -ne $updated) {
            if (-not $WhatIf) {
                Set-Content -Path $csFile.FullName -Value $updated -NoNewline
            }
            $usingsUpdated++
        }
    }

    if ($usingsUpdated -gt 0) {
        Write-Success "  Updated using statements in $usingsUpdated file(s)"
    }
}

function Update-SolutionFile {
    param(
        [hashtable[]]$ProjectMoves
    )

    Write-Info "Updating solution file..."

    $slnPath = Join-Path $dotnetRoot "PigeonPea.sln"
    $content = Get-Content $slnPath -Raw

    foreach ($move in $ProjectMoves) {
        $oldPath = $move.OldPath
        $newPath = $move.NewPath
        $content = $content -replace [regex]::Escape($oldPath), $newPath
    }

    if (-not $WhatIf) {
        Set-Content -Path $slnPath -Value $content -NoNewline
    }

    Write-Success "Solution file updated"
}

# ============================================================================
# PHASE 1: Reorganize Contracts
# ============================================================================

function Phase1-ReorganizeContracts {
    Write-Host "`n========================================" -ForegroundColor Magenta
    Write-Host "PHASE 1: Reorganize Contracts" -ForegroundColor Magenta
    Write-Host "========================================`n" -ForegroundColor Magenta

    $projectMoves = @()

    # App-essential contracts
    Write-Info "Processing app-essential/contracts/..."
    $appContractsRoot = Join-Path $dotnetRoot "app-essential\contracts"
    $appContractsSrc = Join-Path $appContractsRoot "src"
    $appContractsTests = Join-Path $appContractsRoot "tests"

    if (-not $WhatIf) {
        New-Item -ItemType Directory -Path $appContractsSrc -Force | Out-Null
        New-Item -ItemType Directory -Path $appContractsTests -Force | Out-Null
    }
    Write-Success "  Created src/ and tests/ directories"

    $appContractProjects = Get-ChildItem -Path $appContractsRoot -Directory |
        Where-Object { $_.Name.StartsWith("PigeonPea.") }

    foreach ($project in $appContractProjects) {
        $newPath = Join-Path $appContractsSrc $project.Name
        if (-not $WhatIf) {
            Move-Item $project.FullName $newPath
        }
        Write-Success "  Moved $($project.Name) to src/"

        $oldRelPath = "app-essential\contracts\$($project.Name)"
        $newRelPath = "app-essential\contracts\src\$($project.Name)"
        $projectMoves += @{
            OldPath = $oldRelPath
            NewPath = $newRelPath
        }
    }

    # Move misplaced test
    $misplacedTest = Join-Path $dotnetRoot "app-essential\core\tests\PigeonPea.Contracts.Recording.Tests"
    if (Test-Path $misplacedTest) {
        $newTestPath = Join-Path $appContractsTests "PigeonPea.Contracts.Recording.Tests"
        if (-not $WhatIf) {
            Move-Item $misplacedTest $newTestPath
        }
        Write-Success "  Moved PigeonPea.Contracts.Recording.Tests to contracts/tests/"

        $projectMoves += @{
            OldPath = "app-essential\core\tests\PigeonPea.Contracts.Recording.Tests"
            NewPath = "app-essential\contracts\tests\PigeonPea.Contracts.Recording.Tests"
        }
    }

    # Game-essential contracts
    Write-Info "Processing game-essential/contracts/..."
    $gameContractsRoot = Join-Path $dotnetRoot "game-essential\contracts"
    $gameContractsSrc = Join-Path $gameContractsRoot "src"
    $gameContractsTests = Join-Path $gameContractsRoot "tests"

    if (-not $WhatIf) {
        New-Item -ItemType Directory -Path $gameContractsSrc -Force | Out-Null
        New-Item -ItemType Directory -Path $gameContractsTests -Force | Out-Null
    }
    Write-Success "  Created src/ and tests/ directories"

    $gameContractProjects = Get-ChildItem -Path $gameContractsRoot -Directory |
        Where-Object { $_.Name.StartsWith("PigeonPea.") }

    foreach ($project in $gameContractProjects) {
        $newPath = Join-Path $gameContractsSrc $project.Name
        if (-not $WhatIf) {
            Move-Item $project.FullName $newPath
        }
        Write-Success "  Moved $($project.Name) to src/"

        $oldRelPath = "game-essential\contracts\$($project.Name)"
        $newRelPath = "game-essential\contracts\src\$($project.Name)"
        $projectMoves += @{
            OldPath = $oldRelPath
            NewPath = $newRelPath
        }
    }

    return $projectMoves
}

# ============================================================================
# PHASE 2: Rename Plugins to Plugin
# ============================================================================

function Phase2-RenamePlugins {
    Write-Host "`n========================================" -ForegroundColor Magenta
    Write-Host "PHASE 2: Rename Plugins to Plugin" -ForegroundColor Magenta
    Write-Host "========================================`n" -ForegroundColor Magenta

    $projectMoves = @()

    # App-essential plugins
    Write-Info "Processing app-essential/plugins/..."

    # Create tests directory
    $appPluginsTests = Join-Path $dotnetRoot "app-essential\plugins\tests"
    if (-not $WhatIf) {
        New-Item -ItemType Directory -Path $appPluginsTests -Force | Out-Null
    }
    Write-Success "  Created tests/ directory"

    $appPluginsSrc = Join-Path $dotnetRoot "app-essential\plugins\src"
    $appPluginProjects = Get-ChildItem -Path $appPluginsSrc -Directory |
        Where-Object { $_.Name -like "PigeonPea.Plugins.*" }

    foreach ($project in $appPluginProjects) {
        $oldName = $project.Name
        $newName = $oldName -replace "\.Plugins\.", ".Plugin."
        $newPath = Join-Path $project.Parent $newName

        Write-Info "  Renaming $oldName to $newName..."

        # Update project files and source code
        Update-CsprojFile -ProjectPath $project.FullName -OldName $oldName -NewName $newName
        Update-SourceFiles -ProjectPath $project.FullName -OldNamespace $oldName -NewNamespace $newName

        # Rename directory
        if (-not $WhatIf) {
            Rename-Item $project.FullName $newPath
        }
        Write-Success "    Renamed directory"

        # Move tests to tests/ directory if this is a test project
        if ($newName -like "*.Tests") {
            $finalPath = Join-Path $appPluginsTests $newName
            if (-not $WhatIf -and (Test-Path $newPath)) {
                Move-Item $newPath $finalPath
            }
            Write-Success "    Moved to tests/"

            $projectMoves += @{
                OldPath = "app-essential\plugins\src\$oldName"
                NewPath = "app-essential\plugins\tests\$newName"
            }
        } else {
            $projectMoves += @{
                OldPath = "app-essential\plugins\src\$oldName"
                NewPath = "app-essential\plugins\src\$newName"
            }
        }

        # Update all references
        Update-AllReferences -OldName $oldName -NewName $newName
    }

    # Game-essential plugins
    Write-Info "Processing game-essential/plugins/..."

    $gamePluginsSrc = Join-Path $dotnetRoot "game-essential\plugins\src"
    $gamePluginProjects = Get-ChildItem -Path $gamePluginsSrc -Directory |
        Where-Object { $_.Name -like "PigeonPea.Plugins.*" }

    foreach ($project in $gamePluginProjects) {
        $oldName = $project.Name
        $newName = $oldName -replace "\.Plugins\.", ".Plugin."
        $newPath = Join-Path $project.Parent $newName

        Write-Info "  Renaming $oldName to $newName..."

        # Update project files and source code
        Update-CsprojFile -ProjectPath $project.FullName -OldName $oldName -NewName $newName
        Update-SourceFiles -ProjectPath $project.FullName -OldNamespace $oldName -NewNamespace $newName

        # Rename directory
        if (-not $WhatIf) {
            Rename-Item $project.FullName $newPath
        }
        Write-Success "    Renamed directory"

        $projectMoves += @{
            OldPath = "game-essential\plugins\src\$oldName"
            NewPath = "game-essential\plugins\src\$newName"
        }

        # Update all references
        Update-AllReferences -OldName $oldName -NewName $newName
    }

    return $projectMoves
}

# ============================================================================
# PHASE 3: Reorganize Orphaned Plugins
# ============================================================================

function Phase3-ReorganizeOrphanedPlugins {
    Write-Host "`n========================================" -ForegroundColor Magenta
    Write-Host "PHASE 3: Reorganize Orphaned Plugins" -ForegroundColor Magenta
    Write-Host "========================================`n" -ForegroundColor Magenta

    $projectMoves = @()

    $orphanedPluginsSrc = Join-Path $dotnetRoot "plugins\src"
    $gamePluginsSrc = Join-Path $dotnetRoot "game-essential\plugins\src"

    if (-not (Test-Path $orphanedPluginsSrc)) {
        Write-Warning "No orphaned plugins directory found, skipping..."
        return $projectMoves
    }

    $orphanedProjects = Get-ChildItem -Path $orphanedPluginsSrc -Directory

    foreach ($project in $orphanedProjects) {
        $oldName = $project.Name
        $newName = $oldName -replace "\.Plugins\.", ".Plugin."

        Write-Info "  Processing $oldName..."

        # Update project files and source code
        Update-CsprojFile -ProjectPath $project.FullName -OldName $oldName -NewName $newName
        Update-SourceFiles -ProjectPath $project.FullName -OldNamespace $oldName -NewNamespace $newName

        # Move to game-essential with new name
        $newPath = Join-Path $gamePluginsSrc $newName
        if (-not $WhatIf) {
            Move-Item $project.FullName $newPath
        }
        Write-Success "    Moved to game-essential/plugins/src/ as $newName"

        $projectMoves += @{
            OldPath = "plugins\src\$oldName"
            NewPath = "game-essential\plugins\src\$newName"
        }

        # Update all references
        Update-AllReferences -OldName $oldName -NewName $newName
    }

    # Remove orphaned plugins directory
    $orphanedPluginsRoot = Join-Path $dotnetRoot "plugins"
    if ((Test-Path $orphanedPluginsRoot) -and ((Get-ChildItem $orphanedPluginsSrc).Count -eq 0)) {
        if (-not $WhatIf) {
            Remove-Item -Path $orphanedPluginsRoot -Recurse -Force
        }
        Write-Success "  Removed orphaned plugins/ directory"
    }

    return $projectMoves
}

# ============================================================================
# Main Execution
# ============================================================================

try {
    Write-Host "`n" -NoNewline
    Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║  RFC-059: .NET Project Structure Unification Migration    ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
    Write-Host "`n"

    if ($WhatIf) {
        Write-Warning "Running in WHAT-IF mode - no changes will be made"
    }

    # Backup solution file
    Backup-Solution

    # Execute phases
    $allProjectMoves = @()

    $phase1Moves = Phase1-ReorganizeContracts
    $allProjectMoves += $phase1Moves

    $phase2Moves = Phase2-RenamePlugins
    $allProjectMoves += $phase2Moves

    $phase3Moves = Phase3-ReorganizeOrphanedPlugins
    $allProjectMoves += $phase3Moves

    # Update solution file
    Update-SolutionFile -ProjectMoves $allProjectMoves

    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "Migration Complete!" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Green

    if (-not $WhatIf) {
        Write-Info "Next steps:"
        Write-Info "1. Build the solution: dotnet build PigeonPea.sln"
        Write-Info "2. Run tests: dotnet test PigeonPea.sln"
        Write-Info "3. Commit changes with git"
    }

} catch {
    Write-Error "Migration failed: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
