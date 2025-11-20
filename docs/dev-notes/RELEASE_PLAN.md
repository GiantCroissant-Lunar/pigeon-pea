---
canonical: true
created: '2025-11-20'
doc_id: GUIDE-2025-00005
doc_type: guide
related: []
status: active
summary: 'This document describes how we plan to ship self-contained bundles of PigeonPea
  for different platforms, including:'
supersedes: []
tags:
- ci-cd
- ecs
- fonts
- guide
- rendering
- terminal
- testing
title: 'Release Plan: Cross-Platform Bundles'
---

# Release Plan: Cross-Platform Bundles

This document describes how we plan to ship **self-contained bundles** of PigeonPea for different platforms, including:

- Console app (braille/ANSI roguelike UI)
- Desktop app (Avalonia/Skia "Windows" app, later Linux/macOS too)
- Rio terminal + config for a good console UX
- Simple launcher scripts per platform

The goal is that users can:

- Download a ZIP/tarball for their platform
- Extract to a folder
- Run a launcher script
- Play the game (console or desktop) **without installing .NET, Rio, or any other tooling**.

This plan builds on the existing **Nuke + GitVersion** setup and the versioned artifacts under `build/_artifacts/{version}`.

---

## 1. Build Artifacts (Development vs Release)

### 1.1 Development Artifacts

For development and testing, we use Nuke and Taskfile:

- Nuke `PublishPlayers` target (via `IPublish` component):
  - Publishes players into **versioned** folders:
    - `build/_artifacts/{version}/PigeonPea.Console/`
    - `build/_artifacts/{version}/PigeonPea.Windows/`
  - Also maintains a **latest alias**:
    - `build/_artifacts/latest/PigeonPea.Console/`
    - `build/_artifacts/latest/PigeonPea.Windows/`

- Taskfile tasks:
  - `game:publish-latest`:
    - Runs Nuke `PublishPlayers` for `Runtime=win-x64`, `Configuration=Release`.
    - Refreshes `build/_artifacts/latest/...`.
  - `game:run-latest-console`:
    - Depends on `game:publish-latest`.
    - `cd` into `build/_artifacts/latest/PigeonPea.Console` and runs `PigeonPea.Console.exe`.

These are primarily for **developers and agents** to quickly build and run the latest dev builds.

### 1.2 Release Artifacts

For **release**, we will produce **platform-specific bundles** that do not require Task/Nuke or a local .NET SDK.

We will introduce a new set of Nuke targets (or parameters) to:

- Publish players for **multiple runtimes** (e.g., `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`).
- Copy those self-contained publishes into a release folder layout like:

```text
releases/
  {version}/
    windows/
      rio/              # bundled Rio terminal + config.toml + fonts
      console/          # published PigeonPea.Console for win-x64
      desktop/          # published PigeonPea.Windows (Avalonia) for win-x64
      run-console.bat
      run-desktop.bat

    linux/
      rio/              # optional: Rio binary + config (or instruct users to install)
      console/          # published PigeonPea.Console for linux-x64
      desktop/          # Avalonia desktop build for linux-x64 (future)
      run-console.sh
      run-desktop.sh

    macos/
      rio/              # optional: Rio app/binary + config
      console/          # osx-x64 / osx-arm64 publish
      desktop/          # Avalonia build for macOS (future)
      run-console.command
      run-desktop.command
```

These release folders are **distinct** from `build/_artifacts` and are intended to be zipped and distributed.

---

## 2. Rio Terminal Bundling

### 2.1 Rio Config with the Binary

We want Rio config to live **alongside** the Rio executable so that shipped bundles are self-contained.

- Bundle layout for Rio:

  ```text
  rio/
    rio.exe             # or rio binary / app
    config.toml
    fonts/ (optional)   # bundled fonts such as LunarSarasaMono
  ```

- Use the `RIO_CONFIG_HOME` environment variable so Rio uses `rio/config.toml` instead of `%USERPROFILE%` paths.

  On Windows:

  ```bat
  set "RIO_CONFIG_HOME=%BASE%rio"
  "%BASE%rio\rio.exe" ...
  ```

  On Linux/macOS:

  ```sh
  export RIO_CONFIG_HOME="$BASE/rio"
  "$BASE/rio/rio" ...
  ```

### 2.2 Rio Font Configuration (Braille / Box Drawing)

`config.toml` under the `rio/` folder should specify a font with good braille and box-drawing coverage (e.g. patched LunarSarasa):

```toml
# Global options (must be before any [section])
theme = "dracula"  # or your chosen theme

[font]
family = "LunarSarasaMono"  # name as registered on the OS
size = 14

[font.bold]
family = "LunarSarasaMono"

[font.italic]
family = "LunarSarasaMono"

[font.bold_italic]
family = "LunarSarasaMono"
```

The exact `family` value must match the installed font name on the target platform.

---

## 3. Platform-Specific Launch Scripts

For each platform, we provide simple launch scripts that:

1. Set `RIO_CONFIG_HOME` to the bundled `rio/` directory (for console).
2. Start Rio (for console) or the desktop app directly.
3. `cd` into the correct published app folder.

### 3.1 Windows Launchers

Assuming bundle layout:

```text
{bundle-root}/
  rio/
  console/
  desktop/
  run-console.bat
  run-desktop.bat
```

**run-console.bat**:

```bat
@echo off
setlocal
set "BASE=%~dp0"

set "RIO_CONFIG_HOME=%BASE%rio"
"%BASE%rio\rio.exe" -e "%BASE%console\PigeonPea.Console.exe"
endlocal
```

**run-desktop.bat**:

```bat
@echo off
setlocal
set "BASE=%~dp0"
"%BASE%desktop\PigeonPea.Windows.exe"
endlocal
```

Notes:

- `RIO_CONFIG_HOME` ensures Rio uses the bundled `rio/config.toml`.
- `-e` launches the console app as the command inside Rio.

### 3.2 Linux Launchers (Shell Scripts)

Bundle layout (linux):

```text
{bundle-root}/
  rio/
  console/
  desktop/
  run-console.sh
  run-desktop.sh
```

Make scripts executable (`chmod +x`).

**run-console.sh**:

```sh
#!/usr/bin/env bash
set -euo pipefail
BASE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export RIO_CONFIG_HOME="$BASE/rio"
"$BASE/rio/rio" -e "$BASE/console/PigeonPea.Console"  # binary name may differ
```

**run-desktop.sh**:

```sh
#!/usr/bin/env bash
set -euo pipefail
BASE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
"$BASE/desktop/PigeonPea.Windows"  # later replaced with a Linux desktop binary
```

### 3.3 macOS Launchers

For macOS, we can use `.command` scripts (or `.sh`) with similar content to Linux, and mark them executable. The key is to:

- Resolve the bundle root (`BASE`) relative to the script.
- Set `RIO_CONFIG_HOME` appropriately.
- Invoke the bundled Rio binary and console/desktop app.

---

## 4. Building Release Bundles with Nuke

### 4.1 Inputs and Versioning

We already have:

- GitVersion integration in Nuke (`GitVersionNuGet` property).
- Versioned dev artifacts: `build/_artifacts/{version}/{ProjectName}`.

For release, we plan a new Nuke target (approximate shape):

```csharp
Target PackRelease => _ => _
    .DependsOn(PublishPlayers) // and other publish targets per platform
    .Executes(() =>
    {
        // For each platform (win-x64, linux-x64, macOS):
        //   - Copy published players from build/_artifacts/{version}/...
        //   - Copy rio binaries + config into rio/
        //   - Copy launcher scripts into root
        //   - Optionally zip folder into releases/{version}/{platform}.zip
    });
```

Implementation details to decide later:

- How many platforms to support initially (likely `win-x64` first).
- Whether to embed Rio binaries for Linux/macOS or rely on system-installed Rio.
- Whether to have Nuke zip the bundles by default.

### 4.2 Relationship to Dev Artifacts

- `build/_artifacts/{version}` remains the **single source of truth** for all publishes.
- Release packaging:
  - Reads from `build/_artifacts/{version}`.
  - Writes into `releases/{version}/{platform}`.
- This keeps dev and release paths separate and makes it easy to rebuild bundles for a given version.

---

## 5. Cross-Platform Considerations

1. **Runtime Identifiers (RIDs)**
   - Windows: `win-x64`
   - Linux: `linux-x64` (or `linux-arm64` later)
   - macOS: `osx-x64`, `osx-arm64`
   - Nuke will need to publish for each RID we support.

2. **Avalonia Desktop Support**
   - The current `PigeonPea.Windows` project is tuned for Windows.
   - Avalonia can target Linux and macOS; we will:
     - Either generalize the project name (e.g., `PigeonPea.Desktop`) or
     - Introduce per-platform desktop projects.
   - Release plan assumes we eventually ship a desktop bundle per platform.

3. **Rio Availability**
   - Windows: we plan to bundle Rio.
   - Linux/macOS: two options:
     - Bundle Rio binaries for those platforms.
     - Or document a dependency on system-installed Rio and provide `config.toml` + instructions.
   - The release plan supports both by making `rio/` optional per platform.

4. **Fonts**
   - To ensure consistent braille/box-drawing rendering, we may:
     - Bundle a patched font (e.g., `LunarSarasaMono`) in each release.
     - Configure Rio (and desktop app if needed) to use that font.
   - Licensing needs to be checked for redistributing any fonts.

---

## 6. Next Steps

1. **Finalize Platform Scope for First Release**
   - Likely start with **Windows**:
     - win-x64 console + desktop bundles
     - Rio bundled with `config.toml` and braille-capable font.

2. **Add Nuke Release Target(s)**
   - Implement `PackRelease` (or similar) to:
     - Publish players for desired RIDs.
     - Create bundle directories under `releases/{version}/{platform}`.
     - Copy Rio binaries, configs, and launcher scripts.

3. **Refine Launcher Scripts and Test End-to-End**
   - Verify that:
     - Double-clicking `run-console`/`run-desktop` works on a clean machine.
     - No extra installation is required beyond extracting the archive.

4. **Extend to Linux and macOS** (optional, later)
   - Publish appropriate RIDs.
   - Ensure Avalonia desktop app builds and runs on those platforms.
   - Decide whether to bundle Rio or rely on system packages.

5. **Document User-Facing Setup**
   - Write a short `README` in each release bundle describing:
     - How to run console vs desktop.
     - Any OS-specific considerations.
     - Key keyboard controls for the game.
