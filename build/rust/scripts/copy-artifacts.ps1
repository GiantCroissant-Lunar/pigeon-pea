# Copy built artifacts to versioned directory for Windows
$ErrorActionPreference = "Stop"

try {
    # Get version from GitVersion
    $version = & GitVersion /showvariable SemVer 2>&1 | Select-Object -Last 1

    if ([string]::IsNullOrWhiteSpace($version) -or $version -like "*Error*") {
        Write-Host "Error: Could not get version from GitVersion" -ForegroundColor Red
        Write-Host "Using fallback version: 0.0.0-local" -ForegroundColor Yellow
        $version = "0.0.0-local"
    }

    # Get environment variables
    $projectName = $env:PROJECT_NAME
    if ([string]::IsNullOrWhiteSpace($projectName)) {
        $projectName = "dev-tool-server"
    }

    $binaryExt = $env:BINARY_EXT
    if ([string]::IsNullOrWhiteSpace($binaryExt)) {
        $binaryExt = ".exe"
    }

    # Get the script directory and resolve paths relative to it
    # The binary is built in projects/dungeon/rust/target/release/ (workspace root)
    # This script is in build/rust/scripts/
    $scriptDir = $PSScriptRoot
    $buildRustDir = Split-Path $scriptDir -Parent
    $buildDir = Split-Path $buildRustDir -Parent
    $projectRoot = Split-Path $buildDir -Parent

    # The actual binary location (workspace target directory)
    $workspaceTarget = Join-Path $projectRoot "projects\dungeon\rust\target\release"
    $sourceBinary = Join-Path $workspaceTarget "$projectName$binaryExt"

    # Artifacts go in build/_artifacts/
    $artifactsBaseFull = Join-Path $buildDir "_artifacts"
    $targetDir = Join-Path $artifactsBaseFull "$version\$projectName"

    Write-Host "Version: $version" -ForegroundColor Cyan
    Write-Host "Creating artifact directory: $targetDir" -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

    Write-Host "Copying binary from: $sourceBinary" -ForegroundColor Cyan
    Write-Host "             to: $targetDir" -ForegroundColor Cyan

    if (-not (Test-Path $sourceBinary)) {
        Write-Host "Error: Source binary not found at $sourceBinary" -ForegroundColor Red
        exit 1
    }

    Copy-Item $sourceBinary $targetDir -Force

    # Create version.txt
    $versionFile = Join-Path $targetDir "version.txt"
    $versionContent = @"
Version: $version
Built: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Platform: Windows
Binary: $projectName$binaryExt
"@
    $versionContent | Out-File -FilePath $versionFile -Encoding UTF8

    # Create README.md
    $readmeFile = Join-Path $targetDir "README.md"
    $readmeContent = @"
# $projectName

Version: $version

## Usage

Run the binary:

````powershell
.\$projectName$binaryExt
````

## Build Information

- Built: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
- Platform: Windows
- Build Type: Release
"@
    $readmeContent | Out-File -FilePath $readmeFile -Encoding UTF8

    Write-Host ""
    Write-Host "Artifacts published to: $targetDir" -ForegroundColor Green
    Write-Host "  - $projectName$binaryExt" -ForegroundColor Green
    Write-Host "  - version.txt" -ForegroundColor Green
    Write-Host "  - README.md" -ForegroundColor Green

    exit 0
}
catch {
    Write-Host "Error during artifact copy: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
