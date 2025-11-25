---
doc_id: GUIDE-2025-00001
title: Build System Usage Guide
doc_type: guide
status: active
canonical: true
created: 2025-11-25
updated: 2025-11-25
tags:
  - build
  - nuke
  - artifacts
  - plugins
  - versioning
summary: Complete guide for using the NUKE build system to produce versioned artifacts with proper plugin discovery and deployment
related:
  - .agent/rules/build-requirements.md
  - .agent/commands/build.yaml
---

# Build System Usage Guide

This guide describes how to build Pigeon Pea projects using the NUKE build system, which produces versioned artifacts with proper plugin discovery and deployment.

## Overview

The Pigeon Pea project uses a **NUKE-based build system** that:

- Generates **semantic versioned artifacts** using GitVersion
- Publishes to `build/_artifacts/{version}` directory structure
- **Discovers and copies all plugin projects** automatically
- Maintains a `latest` symlink for convenience
- Produces **self-contained deployments** for Windows (win-x64)
- Creates **build logs** for troubleshooting

## Quick Start

### Building All Players

```powershell
# Using NUKE directly (recommended)
./build/nuke/build.ps1 PublishPlayers

# Using the agent build command
# (If you're Claude Code, use the 'build' command)
```

### Build and Run

```powershell
# Build and run console app
./build/nuke/build.ps1 Task

# Build and run Windows app
./build/nuke/build.ps1 Task --player windows

# Build and run with arguments
./build/nuke/build.ps1 Task --player-args "--debug --level 5"
```

## Artifact Structure

After a successful build, artifacts are organized as follows:

```
build/
└── _artifacts/
    ├── {version}/                          # e.g., 0.0.1-610
    │   ├── PigeonPea.Console/             # Console app
    │   │   ├── PigeonPea.Console.exe
    │   │   └── ...dependencies...
    │   ├── PigeonPea.Windows/             # Windows app
    │   │   ├── PigeonPea.Windows.exe
    │   │   └── ...dependencies...
    │   ├── plugins/                        # Shared plugins directory
    │   │   ├── config-service/            # Plugin ID as directory
    │   │   ├── rendering-terminal-ansi/
    │   │   ├── scene-manager/
    │   │   └── ...other plugins...
    │   └── build-logs/                     # Build metadata and logs
    │       └── publish-players-*.log
    └── latest/                             # Symlink/copy to current version
        ├── PigeonPea.Console/
        ├── PigeonPea.Windows/
        ├── plugins/
        └── build-logs/
```

## Semantic Versioning with GitVersion

The build system uses **GitVersion** for automatic semantic versioning:

### Version Format

- **Main branch**: `{Major}.{Minor}.{Patch}` (e.g., `1.0.0`)
- **Feature branches**: `{Major}.{Minor}.{Patch}-{BranchName}.{CommitsSinceVersionSource}` (e.g., `0.0.1-feature-auth.42`)
- **Pull requests**: `{Major}.{Minor}.{Patch}-PullRequest{PRNumber}.{Commits}` (e.g., `0.0.1-PullRequest123.5`)

### Version Sources

GitVersion reads from:
- Git tags (e.g., `v1.0.0`)
- Git history and branch names
- GitVersion.yml configuration (if present)

### Checking Current Version

```powershell
dotnet gitversion /showvariable SemVer
```

## Plugin Discovery and Copying

### How It Works

The NUKE build system:

1. **Discovers all plugin projects** in the solution by finding projects with "Plugin" in their name
2. **Checks each project's output** at `{ProjectDir}/bin/{Configuration}/{TargetFramework}/`
3. **Validates** by checking for a `plugin.json` file
4. **Reads plugin.json** to get the plugin ID
5. **Copies the entire output directory** to `build/_artifacts/{version}/plugins/{plugin-id}/`

### Plugin Directory Naming

Plugins are organized by their **plugin ID** (from `plugin.json`), not by project name:

```json
{
  "id": "config-service",      // ← Used as directory name
  "name": "Configuration Service",
  "version": "1.0.0",
  ...
}
```

Results in: `build/_artifacts/0.0.1-610/plugins/config-service/`

### Plugin Discovery Paths

The console and Windows apps are configured to discover plugins at:

1. `./plugins` (relative to exe)
2. `../plugins` (shared plugins directory) ← Used by build system

This is configured in `appsettings.json`:

```json
{
  "PluginSystem": {
    "PluginPaths": ["plugins", "../plugins"]
  }
}
```

## Running Built Executables

### From Latest Build

```powershell
# Console app
./build/_artifacts/latest/PigeonPea.Console/PigeonPea.Console.exe

# Windows app
./build/_artifacts/latest/PigeonPea.Windows/PigeonPea.Windows.exe
```

### From Specific Version

```powershell
# Console app
./build/_artifacts/0.0.1-610/PigeonPea.Console/PigeonPea.Console.exe

# Windows app
./build/_artifacts/0.0.1-610/PigeonPea.Windows/PigeonPea.Windows.exe
```

## Build Configuration

### Runtime Configuration

Default: `win-x64` (self-contained)

To change:

```powershell
./build/nuke/build.ps1 PublishPlayers --runtime linux-x64
```

### Build Configuration

Default: `Debug`

Controlled by NUKE's standard `--configuration` parameter:

```powershell
./build/nuke/build.ps1 PublishPlayers --configuration Release
```

### Projects to Publish

Configured in `build/nuke/build.config.json`:

```json
{
  "publishProjectPaths": [
    "projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/PigeonPea.Console.csproj",
    "projects/dungeon/dotnet/windows-app/core/src/PigeonPea.Windows/PigeonPea.Windows.csproj"
  ]
}
```

## Troubleshooting

### Issue: "Could not load type" Errors

**Cause:** Old plugin DLLs with outdated namespaces in bin directories

**Solution:**
1. Clean plugin directories:
   ```powershell
   Remove-Item -Recurse -Force projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/bin/Debug/net9.0/plugins
   ```
2. Rebuild:
   ```powershell
   ./build/nuke/build.ps1 PublishPlayers
   ```

### Issue: Plugin Not Found at Runtime

**Symptoms:**
```
[DBG] Plugin path does not exist: ...\PigeonPea.Console\plugins
```

**Cause:** Plugin not copied to artifacts directory

**Solution:**
1. Check if plugin has `plugin.json` in its output directory
2. Verify plugin project name contains "Plugin"
3. Check build logs at `build/_artifacts/{version}/build-logs/`

### Issue: Plugin Depends on Missing Plugin

**Symptoms:**
```
Plugin 'hud-terminal-terminalgui' depends on missing plugin 'config-service'.
```

**Cause:** Dependent plugin not built or not included in solution

**Solution:**
1. Check if dependent plugin project exists
2. Build the plugin project explicitly:
   ```powershell
   dotnet build path/to/PluginProject.csproj
   ```
3. Add plugin project to solution if missing

### Issue: Version Not Found

**Cause:** GitVersion not available or git history issues

**Solution:**
1. Ensure you're in a git repository with commits
2. Install GitVersion:
   ```powershell
   dotnet tool restore
   ```
3. Verify:
   ```powershell
   dotnet gitversion
   ```

### Checking Build Logs

Build logs contain detailed information about each publish operation:

```powershell
# View latest build log
Get-Content build/_artifacts/latest/build-logs/publish-players-*.log
```

Log contents:
- Timestamp
- Version
- Runtime
- Configuration
- Project name
- Status (Success/Failed)
- Error details (if failed)

## Best Practices

### ✅ DO

- **Always use the NUKE build system** for producing artifacts
- **Run from versioned artifacts** (`build/_artifacts/{version}/`) for testing
- **Check build logs** at `build/_artifacts/{version}/build-logs/` if issues occur
- **Use semantic versioning** via git tags for releases
- **Keep plugin.json files** in sync with plugin IDs

### ❌ DON'T

- Don't use `dotnet build` or `dotnet publish` directly for deployments
- Don't run executables from project `bin/` directories
- Don't manually copy plugins between directories
- Don't commit `build/_artifacts/` (it's in .gitignore)
- Don't skip GitVersion for versioned builds

## Developer Workflow

### Standard Development Cycle

```powershell
# 1. Make code changes

# 2. Build and test
./build/nuke/build.ps1 Task

# 3. Verify from versioned artifacts
./build/_artifacts/latest/PigeonPea.Console/PigeonPea.Console.exe

# 4. Check build logs if issues occur
Get-Content build/_artifacts/latest/build-logs/*.log
```

### Quick Iteration (Development Only)

For rapid iteration during development:

```powershell
# Run directly from source (faster, but may have stale plugins)
dotnet run --project projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console
```

⚠️ **Warning:** Running with `dotnet run` uses plugins from the project's bin directory, which may have stale DLLs. For reliable testing, always use the NUKE build system.

### Release Process

```powershell
# 1. Tag the release
git tag -a v1.0.0 -m "Release 1.0.0"

# 2. Build release artifacts
./build/nuke/build.ps1 PublishPlayers --configuration Release

# 3. Verify version
# Should output: 1.0.0
dotnet gitversion /showvariable SemVer

# 4. Test from artifacts
./build/_artifacts/1.0.0/PigeonPea.Console/PigeonPea.Console.exe

# 5. Archive or deploy
# Artifacts are at: build/_artifacts/1.0.0/
```

## Integration with CI/CD

The NUKE build system is designed for CI/CD integration:

```yaml
# Example GitHub Actions workflow
- name: Build
  run: ./build/nuke/build.ps1 PublishPlayers --configuration Release

- name: Upload Artifacts
  uses: actions/upload-artifact@v3
  with:
    name: pigeon-pea-${{ steps.gitversion.outputs.semVer }}
    path: build/_artifacts/${{ steps.gitversion.outputs.semVer }}/
```

## Architecture Compliance

This build system enforces:

- ✅ **Tiered architecture** (see `.agent/rules/dotnet-architecture.md`)
- ✅ **Plugin isolation** (plugins never depend on other plugins)
- ✅ **Clean plugin loading** (no ALC type identity issues)
- ✅ **Reproducible builds** (GitVersion semantic versioning)
- ✅ **Version history** (each build preserved in versioned directory)

## Related Documentation

- [Build Requirements](.agent/rules/build-requirements.md) - Detailed build requirements and rules
- [Build Command](.agent/commands/build.yaml) - Agent build command definition
- [.NET Architecture Guide](docs/guides/dotnet-tiered-architecture-guide.md) - Architecture overview
- [Plugin System Architecture](docs/rfcs/013-plugin-architecture-refinement-tiered.md) - Plugin design rationale

## Support

For issues or questions:
- Check build logs at `build/_artifacts/{version}/build-logs/`
- Review [Build Requirements](.agent/rules/build-requirements.md)
- See [Troubleshooting](#troubleshooting) section above
