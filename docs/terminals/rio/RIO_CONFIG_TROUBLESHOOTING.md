# Rio Terminal Configuration Troubleshooting Guide

**Date:** 2025-11-18  
**Project:** Pigeon Pea - Dungeon Dev Server  
**Purpose:** Document Rio Terminal configuration issues and solutions

## Overview

This document records the issues encountered while configuring Rio Terminal for the Dungeon dev-tool-server Rust project, and the solutions implemented to resolve them.

## Table of Contents

1. [Background](#background)
2. [Issues Discovered](#issues-discovered)
3. [Solutions Implemented](#solutions-implemented)
4. [Best Practices](#best-practices)
5. [Related Files](#related-files)

---

## Background

### Project Structure

```
build/
├── _artifacts/
│   └── {version}/
│       └── dev-tool-server/
│           ├── dev-tool-server.exe    # Built binary
│           └── config.toml            # Rio config (generated)
├── rust/
│   ├── Makefile.toml                  # cargo-make build config
│   └── scripts/
│       ├── launch-latest.ps1          # Launch script
│       ├── copy-artifacts.ps1         # Artifact copy script
│       └── get-version.sh             # Version extraction
└── ...

projects/dungeon/rust/
├── dev-tool-server/                   # Rust TUI project
└── rio-config.toml                    # Rio config template
```

### Goal

Launch the dev-tool-server binary in Rio Terminal with:
- Custom configuration per version
- Proper font loading
- Clean startup without errors
- Isolated config that doesn't affect other Rio instances

---

## Issues Discovered

### Issue 1: Font Configuration Errors

**Error Message:**
```
Font(s) not found: [Microsoft JhengHei, Microsoft YaHei, SimSun]
Rio will proceed with the default configuration
```

#### Root Cause

The `extras` array in the Rio config tried to load fonts that didn't exist in:
- Windows system fonts
- The `additional-dirs` paths specified

**Problem Code:**
```toml
extras = [
    { family = "Microsoft YaHei" },     # Not found in system
    { family = "Microsoft JhengHei" },  # Not found in system
    { family = "SimSun" },              # Not found in system
    { family = "Consolas" }             # ✓ Works (Windows built-in)
]
```

#### Why It Happened

When we changed the `working_directory` to the artifacts folder, Rio could no longer find the relative font paths. Additionally, these CJK fonts may not be installed on all Windows systems by default.

---

### Issue 2: TOML Duplicate Key Error

**Error Message:**
```
[[keybindings]] duplicate key
Rio will proceed with the default configuration
```

**Location:** Line 79 in `rio-config.toml`

#### Root Cause

Invalid TOML structure mixing table and array-of-tables with the same key:

**Problem Code:**
```toml
[keybindings]              # ❌ Table header
# Custom keybindings for development workflow
[[keybindings]]            # ❌ Array of tables (conflicts!)
key = "Ctrl+Shift+N"
action = "NewWindow"
...
```

In TOML, you cannot have both:
- `[keybindings]` (a single table)
- `[[keybindings]]` (an array of tables)

with the same key name.

---

### Issue 3: TOML Type Error

**Error Message:**
```
TOML parse error at line 8, column 15
decorations = true
wanted string or table
```

#### Root Cause

The `decorations` property in Rio Terminal expects a **string value**, not a boolean.

**Problem Code:**
```toml
[window]
decorations = true      # ❌ Boolean not allowed
resizable = true        # ❌ Not a valid Rio option
```

**Valid Values:**
- `"Enabled"` (default)
- `"Disabled"`
- `"Transparent"`
- `"Buttonless"`

---

### Issue 4: Config Not Being Read

**Error:** Rio was using default configuration instead of our custom config.

#### Root Cause

The script was copying the config to `D:\lunar-snake\tools\rio\config.toml` (Rio's default location), but when we changed the `working_directory` to the artifacts folder, Rio couldn't find fonts and other relative resources.

**Problem:** Config and binary were in different locations, causing path resolution issues.

---

## Solutions Implemented

### Solution 1: Clean Up Font Extras

**File:** `build/rust/scripts/launch-latest.ps1`

**Change:** Remove problematic fonts from the `extras` array:

```powershell
# Remove problematic fonts from extras array that cause "Font(s) not found" errors
# Keep only fonts that are guaranteed to work on Windows
$configContent = $configContent -replace 'extras = \[[\s\S]*?\]', @"
extras = [
    { family = "Consolas" }
]
"@
```

**Result:** Rio launches without font warnings.

---

### Solution 2: Fix TOML Duplicate Key

**File:** `projects/dungeon/rust/rio-config.toml`

**Change:** Remove the `[keybindings]` table header:

**Before:**
```toml
[keybindings]              # ❌ Remove this
# Custom keybindings for development workflow
[[keybindings]]
key = "Ctrl+Shift+N"
...
```

**After:**
```toml
# Custom keybindings for development workflow
[[keybindings]]            # ✓ Array of tables only
key = "Ctrl+Shift+N"
action = "NewWindow"
description = "Open new Rio window"

[[keybindings]]
key = "Ctrl+Shift+T"
...
```

**Result:** Valid TOML structure with no duplicate keys.

---

### Solution 3: Fix TOML Type Errors

**File:** `projects/dungeon/rust/rio-config.toml`

**Changes:**

1. **Fix decorations type:**
```toml
[window]
decorations = "Enabled"  # ✓ String value
```

2. **Remove invalid option:**
```toml
# Removed: resizable = true  (not a valid Rio config option)
```

**Result:** Valid Rio Terminal configuration with correct types.

---

### Solution 4: Use `$RIO_CONFIG_HOME` Environment Variable

**File:** `build/rust/scripts/launch-latest.ps1`

**Key Change:** Instead of copying config to Rio's default location, use the `RIO_CONFIG_HOME` environment variable to tell Rio where to find the config.

**Before (Problematic):**
```powershell
# Copy config to Rio's default location
$RioConfig = Join-Path $RioDir "config.toml"
Copy-Item $TempConfig $RioConfig -Force

# Backup/restore logic needed
if (Test-Path $RioConfig) {
    Copy-Item $RioConfig $RioConfigBackup -Force
}
```

**After (Clean):**
```powershell
# Save config directly to artifact directory
$artifactConfigDir = Join-Path $artifactsDir "$Version\dev-tool-server"
$artifactConfig = Join-Path $artifactConfigDir "config.toml"
$configContent | Out-File -FilePath $artifactConfig -Encoding UTF8

# Set RIO_CONFIG_HOME to artifact directory
$env:RIO_CONFIG_HOME = $artifactConfigDir

# Launch Rio - it will read config from $RIO_CONFIG_HOME
& $RioExe
```

**Benefits:**
- ✅ Config lives alongside the binary it configures
- ✅ Each version has its own isolated config
- ✅ No backup/restore needed
- ✅ No interference with other Rio instances
- ✅ Cleaner, more maintainable code

---

### Solution 5: Platform-Aware Path Escaping

**File:** `build/rust/scripts/launch-latest.ps1`

**Change:** Use platform-specific directory separator for TOML path escaping:

```powershell
# Use platform's directory separator and escape for TOML if needed
$separator = [System.IO.Path]::DirectorySeparatorChar
if ($separator -eq '\') {
    # Windows: Escape backslashes for TOML (single escape: \ becomes \\)
    $rioFontsPath1Escaped = $rioFontsPath1 -replace '\\', '\\'
    $rioFontsPath2Escaped = $rioFontsPath2 -replace '\\', '\\'
} else {
    # Linux/macOS: Forward slashes need no escaping in TOML
    $rioFontsPath1Escaped = $rioFontsPath1
    $rioFontsPath2Escaped = $rioFontsPath2
}
```

**Why:** Windows uses backslashes (`\`) which must be escaped in TOML (`\\`), while Linux/macOS use forward slashes (`/`) which don't need escaping.

---

## Best Practices

### 1. Rio Terminal Configuration

#### Use `$RIO_CONFIG_HOME` for Custom Configs

**DO:**
```powershell
$env:RIO_CONFIG_HOME = "D:\path\to\config\directory"
& rio-portable-x86_64.exe
```

**DON'T:**
```powershell
# Avoid copying configs to Rio's default location
Copy-Item $config "D:\tools\rio\config.toml"
```

#### String Values for Window Properties

**DO:**
```toml
[window]
decorations = "Enabled"  # String value
```

**DON'T:**
```toml
[window]
decorations = true       # Boolean not allowed
```

#### Array of Tables for Keybindings

**DO:**
```toml
[[keybindings]]
key = "Ctrl+Shift+N"
action = "NewWindow"

[[keybindings]]
key = "Ctrl+Shift+T"
action = "NewTab"
```

**DON'T:**
```toml
[keybindings]            # ❌ Don't mix table and array-of-tables
[[keybindings]]
key = "Ctrl+Shift+N"
```

---

### 2. Font Configuration

#### Only Include Fonts That Exist

**DO:**
```toml
extras = [
    { family = "Consolas" }  # Windows built-in font
]
```

**DON'T:**
```toml
extras = [
    { family = "Microsoft YaHei" },     # May not exist
    { family = "Microsoft JhengHei" },  # May not exist
    { family = "SimSun" }               # May not exist
]
```

#### Use Absolute Paths for Font Directories

**DO:**
```toml
additional-dirs = [
    "D:\\tools\\rio\\JetBrainsMono",
    "D:\\tools\\rio\\Microsoft JhengHei Regular"
]
```

**DON'T:**
```toml
additional-dirs = [
    "./JetBrainsMono",              # Breaks when working_directory changes
    "./Microsoft JhengHei Regular"
]
```

---

### 3. TOML Path Escaping

#### Platform-Aware Escaping

**Windows:**
```toml
working_directory = "D:\\path\\to\\directory"  # Escape backslashes
```

**Linux/macOS:**
```toml
working_directory = "/path/to/directory"  # No escaping needed
```

**PowerShell Implementation:**
```powershell
$separator = [System.IO.Path]::DirectorySeparatorChar
if ($separator -eq '\') {
    $escaped = $path -replace '\\', '\\'  # Windows
} else {
    $escaped = $path                       # Linux/macOS
}
```

---

### 4. Version Isolation

#### Keep Configs with Artifacts

**Directory Structure:**
```
build/_artifacts/
├── 0.1.0/
│   └── dev-tool-server/
│       ├── dev-tool-server.exe
│       └── config.toml           # ✓ Version-specific config
├── 0.2.0/
│   └── dev-tool-server/
│       ├── dev-tool-server.exe
│       └── config.toml           # ✓ Another version's config
```

**Benefits:**
- Each version has its own config
- Easy to reproduce specific version behavior
- No conflicts between versions

---

## Related Files

### Configuration Files

- **Template:** [`projects/dungeon/rust/rio-config.toml`](../../../projects/dungeon/rust/rio-config.toml)
  - Source template for Rio configuration
  - Gets processed by launch script

- **Generated Config:** `build/_artifacts/{version}/dev-tool-server/config.toml`
  - Generated from template by launch script
  - Version-specific configuration

### Scripts

- **Launch Script:** [`build/rust/scripts/launch-latest.ps1`](../../../build/rust/scripts/launch-latest.ps1)
  - Finds latest artifact version
  - Generates config from template
  - Sets `$RIO_CONFIG_HOME` and launches Rio

- **Artifact Copy:** [`build/rust/scripts/copy-artifacts.ps1`](../../../build/rust/scripts/copy-artifacts.ps1)
  - Copies built binaries to versioned artifacts directory

- **Version Detection:** [`build/rust/scripts/get-version.sh`](../../../build/rust/scripts/get-version.sh)
  - Extracts version from Cargo.toml

### Build Configuration

- **Cargo Make:** [`build/rust/Makefile.toml`](../../../build/rust/Makefile.toml)
  - Defines build tasks for Rust projects
  - Includes `publish` task that builds and copies artifacts

- **Taskfile:** [`Taskfile.yml`](../../../Taskfile.yml)
  - Project-wide task definitions
  - Tasks: `rust:build`, `rust:publish`, `rust:run-latest`

### Documentation

- **Build System README:** [`build/rust/README.md`](../../../build/rust/README.md)
  - Overview of Rust build system
  - Usage instructions for cargo-make

- **Rio Terminal Docs:** [`docs/rioterm/RIOTERM.html`](../RIOTERM.html)
  - Rio Terminal documentation (if applicable)

---

## Verification Checklist

Use this checklist to verify Rio Terminal configuration:

- [ ] Config file has valid TOML syntax (no duplicate keys)
- [ ] `decorations` uses string value, not boolean
- [ ] `[[keybindings]]` uses array-of-tables syntax (double brackets)
- [ ] Font `extras` only includes fonts that exist
- [ ] Font `additional-dirs` uses absolute paths with proper escaping
- [ ] `$RIO_CONFIG_HOME` is set to config directory
- [ ] Config lives in artifact directory alongside binary
- [ ] Launch script uses platform-aware path escaping
- [ ] Rio launches without TOML parsing errors
- [ ] Rio launches without font warnings
- [ ] Binary executes correctly in Rio Terminal

---

## Quick Reference

### Launch Rio with Latest Artifact

```bash
task rust:run-latest
```

### Build and Launch

```bash
task rust:build-and-run
```

### Manual Launch

```powershell
# Set config location
$env:RIO_CONFIG_HOME = "D:\path\to\artifact\dir"

# Launch Rio
& "D:\tools\rio\rio-portable-x86_64.exe"
```

### Validate TOML Syntax

```bash
# Using taplo (TOML linter)
taplo check projects/dungeon/rust/rio-config.toml
```

---

## Troubleshooting Tips

### Rio Shows Default Config Instead of Custom

**Check:**
1. Is `$RIO_CONFIG_HOME` set correctly?
2. Does `config.toml` exist in that directory?
3. Is the TOML syntax valid?

### Font Warnings on Startup

**Solutions:**
1. Remove non-existent fonts from `extras` array
2. Use absolute paths in `additional-dirs`
3. Verify fonts exist in specified directories

### TOML Parse Errors

**Common Issues:**
1. Boolean instead of string for `decorations`
2. Duplicate key (mixing `[table]` and `[[array]]`)
3. Unescaped backslashes in Windows paths

---

## Future Improvements

### Potential Enhancements

1. **Cross-Platform Font Detection**
   - Auto-detect available fonts on system
   - Generate `extras` array dynamically

2. **Config Validation**
   - Pre-validate TOML before launching Rio
   - Provide helpful error messages

3. **Font Bundling**
   - Bundle required fonts with artifacts
   - Set `additional-dirs` to bundled fonts

4. **Profile Support**
   - Allow switching between different Rio profiles
   - Light/dark themes, different font sizes, etc.

---

## Conclusion

Rio Terminal configuration requires careful attention to:
- TOML syntax and types
- Font availability and paths
- Config location and loading mechanism
- Platform-specific path escaping

By using `$RIO_CONFIG_HOME` and keeping configs with artifacts, we achieve clean version isolation and avoid interfering with other Rio instances.

All configuration errors have been resolved, and Rio Terminal now launches successfully with the dev-tool-server binary.

---

**Last Updated:** 2025-11-18  
**Status:** ✅ All issues resolved  
**Next Review:** When adding new Rio configuration features
