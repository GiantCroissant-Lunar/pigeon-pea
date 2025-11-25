---
doc_id: FINDING-2025-00001
title: Plugin Loading Issues - Config Service and Dependencies
doc_type: finding
status: draft
canonical: false
created: 2025-11-25
tags:
  - plugins
  - issues
  - config-service
  - dependencies
summary: Documentation of plugin loading issues discovered during build infrastructure setup, particularly config-service plugin with broken references
---

# Plugin Loading Issues - Config Service and Dependencies

## Status

**Draft** - Issues identified but not yet resolved

## Context

During the setup of the versioned build artifact system (2025-11-25), we discovered that several plugin projects have broken references or are not being built properly.

## Issues Identified

### 1. Config Service Plugin Not Built

**Location:** `dotnet/app-essential/plugins/src/PigeonPea.Plugins.Config/`

**Symptoms:**
- Plugin DLL not found in bin directory after build
- Project not included in main solution (`dotnet/PigeonPea.sln`)
- Build fails with missing namespace errors

**Build Errors:**
```
error CS0234: 命名空間 'PigeonPea' 中沒有類型或命名空間名稱 'Config'
error CS0234: 命名空間 'PigeonPea' 中沒有類型或命名空間名稱 'Contracts'
```

**Root Cause:**
- Project references old directory structure: `dotnet\app-essential\core\src\PigeonPea.Contracts\`
- Correct path is: `dotnet\app-essential\contracts\src\PigeonPea.Contracts\`
- Project has not been updated after namespace/directory refactoring

### 2. Dependent Plugin Fails to Load

**Plugin:** `hud-terminal-terminalgui`
**Location:** `projects/dungeon/dotnet/console-app/plugins/PigeonPea.Plugins.Hud.TerminalGui/`

**Dependency Declaration:**
```json
{
  "id": "hud-terminal-terminalgui",
  "dependencies": [
    {
      "id": "config-service"
    }
  ]
}
```

**Runtime Error:**
```
Plugin 'hud-terminal-terminalgui' depends on missing plugin 'config-service'.
```

**Impact:**
- Console app fails to start
- Cannot use Terminal.Gui HUD
- Blocks testing of build artifacts

## Current State

### What Works ✅

- NUKE build system correctly discovers plugin projects
- Plugins with valid `plugin.json` are copied to artifacts
- Plugin directory structure is correct (`build/_artifacts/{version}/plugins/{plugin-id}/`)
- Plugins that don't depend on config-service load successfully:
  - `rendering-terminal-ansi`
  - `rendering-terminal-braille`
  - `scene-manager`
  - `stats-basic`

### What's Broken ❌

- `config-service` plugin not building
- `hud-terminal-terminalgui` plugin can't load due to missing dependency
- Console app can't start with default configuration

## Potential Solutions

### Option 1: Fix Config Plugin Project References

**Effort:** Medium
**Impact:** Fixes root cause

Steps:
1. Update `PigeonPea.Plugin.Config.csproj` project references
2. Fix namespace imports in source files
3. Add project to `dotnet/PigeonPea.sln`
4. Rebuild and test

**Files to Update:**
- `dotnet/app-essential/plugins/src/PigeonPea.Plugins.Config/PigeonPea.Plugin.Config.csproj`
- `dotnet/app-essential/plugins/src/PigeonPea.Plugins.Config/ConfigPlugin.cs`
- `dotnet/app-essential/plugins/src/PigeonPea.Plugins.Config/ConfigurationConfigService.cs`

### Option 2: Remove Dependency Temporarily

**Effort:** Low
**Impact:** Allows testing, doesn't fix root cause

Steps:
1. Edit `PigeonPea.Plugins.Hud.TerminalGui/plugin.json`
2. Remove `config-service` from dependencies array
3. Rebuild

**Note:** This may cause runtime errors if HUD plugin actually uses config service.

### Option 3: Use Alternative Configuration

**Effort:** Medium-High
**Impact:** May require architecture changes

Consider if `hud-terminal-terminalgui` really needs config service, or if configuration could be provided differently.

## Investigation Needed

### Questions to Answer

1. **What does the config service actually provide?**
   - What configuration does HUD plugin need?
   - Can it use `IConfiguration` directly instead?

2. **Why was config-service not in the solution?**
   - Was it deliberately excluded?
   - Is it part of an older architecture that's being phased out?

3. **Are there other plugins with similar issues?**
   - Should we audit all plugin projects for broken references?
   - What's the list of "critical" vs "optional" plugins?

### Plugins to Audit

Based on directory listing, these plugins may have similar issues:

**App-Essential Plugins:**
- `PigeonPea.Plugin.Analytics.OpenTelemetry`
- `PigeonPea.Plugin.Audio.LibVlc`
- `PigeonPea.Plugin.Diagnostic.*`
- `PigeonPea.Plugin.Input.UniInputSystem`
- `PigeonPea.Plugin.Logging.*`
- `PigeonPea.Plugin.Profiling.*`
- `PigeonPea.Plugin.Recording.*`

**Game-Essential Plugins:**
- `PigeonPea.Plugin.Animation.Basic`
- `PigeonPea.Plugin.Avatar.Basic`
- `PigeonPea.Plugin.Combat.Basic`
- `PigeonPea.Plugin.Inventory.Advanced`
- `PigeonPea.Plugin.Map.*`
- `PigeonPea.Plugin.Navigation.*`
- `PigeonPea.Plugin.Time.*`
- `PigeonPea.Plugin.WorldManagement.Basic`

## Workaround for Testing

Until the config service issue is resolved, the build artifacts can't be run with the default HUD. Possible workarounds:

1. **Use a different renderer:**
   ```powershell
   ./build/_artifacts/latest/PigeonPea.Console/PigeonPea.Console.exe --renderer ansi
   ```

2. **Run from source with direct `dotnet run`:**
   ```powershell
   dotnet run --project projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console
   ```
   (Note: May still have plugin loading issues)

3. **Disable plugin loading temporarily** (requires code changes)

## Next Steps

### Immediate (Blockers)

1. Decide on solution approach (Option 1, 2, or 3)
2. Fix config-service plugin or remove dependency
3. Test that console app runs from build artifacts

### Short-term (Cleanup)

1. Audit all plugin projects for broken references
2. Add missing plugins to solution file
3. Document which plugins are required vs optional

### Long-term (Architecture)

1. Consider if config-service pattern is still needed
2. Review plugin dependency management
3. Ensure plugin projects stay in sync with refactorings

## Related Documentation

- [Build System Usage Guide](../guides/build-system-usage.md)
- [Build Requirements](../../.agent/rules/build-requirements.md)
- [Plugin Architecture RFC](../rfcs/013-plugin-architecture-refinement-tiered.md)

## Timeline

- **2025-11-25:** Issues discovered during build infrastructure setup
- **TBD:** Resolution target date

## Impact Assessment

**Severity:** Medium
- Blocks running console app from build artifacts
- Does not block development (can still use `dotnet run`)
- Does not block Windows app (if it doesn't depend on config-service)

**Scope:** Limited
- Affects console app primarily
- May affect other plugins that depend on config-service
- Does not affect core build infrastructure

## Contact

For questions or updates on this issue, see the related tracking issue (to be created).
