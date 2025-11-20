---
canonical: true
created: '2025-11-19'
doc_id: RFC-00023
doc_type: rfc
related:
- RFC-00013
- RFC-00006
- ADR-00003
status: draft
summary: Refactor input system to follow tier-based architecture (Tier 1-4), eliminate
  redundant wrapper projects, and separate platform-specific device implementations
  into plugins following Unity Input System patterns
supersedes: []
tags:
- input
- architecture
- refactoring
- tiered-architecture
- plugins
- platform
title: 'Input System Architecture Refactoring: Tier-Based System and Platform Plugins'
---


# RFC-023: Input System Architecture Refactoring: Tier-Based System and Platform Plugins

- **Status:** Draft
- **Created:** 2025-11-19
- **Author:** Claude Agent (Architecture Review)
- **Related:** RFC-013 (Plugin Architecture Refinement), RFC-006 (Plugin System), ADR-003 (Service Tiers)

## Summary

Refactor the input system architecture to:

1. **Follow tier-based service architecture** (Tier 1-4) as defined in ADR-003
2. **Eliminate redundant wrapper projects** (`PigeonPea.Shared.Input`)
3. **Separate platform devices into Tier 4 plugins** (ConsoleKeyboard, SDL3Gamepad)
4. **Align with Unity Input System patterns** where:
   - Engine provides mechanism (InputSystem runtime)
   - Content-authoring defines configuration (.inputactions files define what inputs exist)
   - Game/content plugins define behavior (what to do with inputs)
5. **Move input from game-essential to app-essential** (input is app-level, not game-specific)

## Motivation

### Current Problems

1. **Empty Wrapper Project**
   - `PigeonPea.Shared.Input` (app-essential) contains no code
   - Simply references `PigeonPea.Input.Core` (engine layer)
   - Adds unnecessary indirection with no value
   - Violates "Shared libraries as building blocks" principle

2. **Duplicated Device Implementations**
   - `ConsoleKeyboardDevice` exists in TWO locations:
     - `PigeonPea.Plugins.Input.UniInputSystem/ConsoleKeyboardDevice.cs`
     - `PigeonPea.Game.Input/Devices/ConsoleKeyboardDevice.cs`
   - Identical implementations maintained separately
   - Violates DRY principle

3. **Wrong Layer Placement**
   - `PigeonPea.Game.Input` is in game-essential layer
   - But input is app-level concern (console apps, Windows apps, web apps all need input)
   - Game-specific events (MoveInputEvent, AttackInputEvent) mix with platform devices
   - Violates separation of concerns

4. **Platform Devices Not Pluggable**
   - `ConsoleKeyboardDevice` is hardcoded in plugin
   - SDL3 gamepad support would require changing service plugin
   - Platform devices should be Tier 4 providers (like ANSI/Braille renderers)
   - Each platform project should load only relevant device plugins

5. **Unclear Architecture Alignment**
   - Not following Unity Input System separation of concerns:
     - **Mechanism** (engine): How to read input
     - **Configuration** (content-authoring): What inputs exist
     - **Behavior** (game/content): What to do with inputs

### Goals

1. **Establish Clean Tier Architecture**
   - Tier 1: Input service contract (`IService`)
   - Tier 2: Proxy service (source-generated)
   - Tier 3: Input service implementation (orchestrates Input.Core)
   - Tier 4: Platform device providers (ConsoleKeyboard, SDL3Gamepad, etc.)

2. **Separate Platform Concerns**
   - Platform devices are plugins (Tier 4)
   - Service discovers and uses available devices
   - Console app loads ConsoleKeyboard + SDL3Gamepad plugins
   - Windows app loads different device plugins
   - Like rendering: DungeonRenderer (Tier 3) uses ANSIRenderer/BrailleRenderer (Tier 4)

3. **Follow Unity Input System Pattern**
   - Engine (`Input.Core`): Mechanism (InputSystem, InputAction, callbacks)
   - Content-authoring: Configuration (.inputactions files define what inputs exist)
   - Content plugins: Behavior (game code defines what to do with inputs)

4. **Eliminate Redundancy**
   - Delete `PigeonPea.Shared.Input` wrapper
   - Remove device duplication
   - Clear separation of concerns

5. **Proper Layering**
   - engine: Reusable input runtime (like Unity Input System package)
   - app-essential: Input service (Tier 1-3)
   - Platform plugins: Device implementations (Tier 4)
   - content-authoring: Action map definitions
   - Content plugins: Game behavior

## Architecture Overview

### Tier-Based System

```
┌─────────────────────────────────────────────────────────┐
│ Tier 1: Contracts (Interfaces)                          │
├─────────────────────────────────────────────────────────┤
│ PigeonPea.Contracts/Services/Input/                     │
│   └─ IService.cs                                        │
│       - IsActionPressed(actionId)                       │
│       - GetAxis(axisId)                                 │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ Tier 2: Proxy Services (source-gen)                     │
├─────────────────────────────────────────────────────────┤
│ PigeonPea.Contracts/Services/Input/Proxy/               │
│   └─ Service.cs (partial, [RealizeService])            │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ Tier 3: Real Services (Plugins)                         │
├─────────────────────────────────────────────────────────┤
│ PigeonPea.Plugins.Input.UniInputSystem/                 │
│   └─ UniInputSystemService.cs                          │
│       - Wraps Input.Core runtime                       │
│       - Loads action maps from content-authoring       │
│       - Discovers Tier 4 device providers              │
│       - NO device implementations here!                │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ Tier 4: Providers (Platform Devices)                    │
├─────────────────────────────────────────────────────────┤
│ Platform-specific plugins (project-level):              │
│                                                          │
│ projects/.../plugins/PigeonPea.Plugin.Input.            │
│ ConsoleKeyboard/                                        │
│   └─ ConsoleKeyboardDevice.cs (IInputDevice)           │
│                                                          │
│ projects/.../plugins/PigeonPea.Plugin.Input.            │
│ SDL3Gamepad/                                            │
│   └─ SDL3GamepadDevice.cs (IInputDevice)               │
│                                                          │
│ Similar to rendering:                                   │
│   Tier 3: DungeonRenderer (knows WHAT to render)       │
│   Tier 4: ANSIRenderer, BrailleRenderer (HOW)          │
└─────────────────────────────────────────────────────────┘
```

### Dependency Rules

**Allowed:**

- Tier 2 → Tier 1 
- Tier 3 → Tier 1, Input.Core (engine) 
- Tier 4 → Tier 1, Input.Core (engine) 
- Tier 3 discovers Tier 4 via registry 
- app-essential → engine 
- projects → app-essential, engine 

**Not Allowed:**

- Tier 1 → Any other tier 
- Tier 3 → Tier 4 direct dependency (use registry)
- game-essential → app-essential input (input is app-level!) 
- Lower tier → Higher tier 

### Unity Input System Pattern

```
┌─────────────────────────────────────────────────────────┐
│ ENGINE (Mechanism - HOW to read input)                  │
├─────────────────────────────────────────────────────────┤
│ PigeonPea.Input.Core (engine/core/src)                  │
│   - InputSystem, InputAction, InputActionMap           │
│   - IInputDevice interface                             │
│   - Callback system: OnPerformed, OnCanceled           │
│   - Like: Unity Input System, Rewired                  │
│   - NO concrete device implementations!                │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ CONTENT-AUTHORING (Configuration - WHAT inputs exist)   │
├─────────────────────────────────────────────────────────┤
│ projects/dungeon/content-authoring/input/               │
│   └─ DefaultPlayerControls.inputactions                │
│       {                                                 │
│         "maps": [{                                      │
│           "name": "Gameplay",                           │
│           "actions": [                                  │
│             { "name": "Move", "type": "Vector2" },      │
│             { "name": "Attack", "type": "Button" }      │
│           ],                                            │
│           "bindings": [                                 │
│             { "action": "Move",                         │
│               "id": "move-keyboard",                    │
│               "type": "Composite",                      │
│               "compositeType": "TwoDVector",            │
│               "parts": [                                │
│                 { "name": "up", "path": "<Keyboard>/w" },     │
│                 { "name": "down", "path": "<Keyboard>/s" },   │
│                 { "name": "left", "path": "<Keyboard>/a" },   │
│                 { "name": "right", "path": "<Keyboard>/d" }   │
│               ]                                         │
│             },                                          │
│             { "action": "Move",                         │
│               "path": "<Gamepad>/leftStick" },          │
│             { "action": "Attack",                       │
│               "path": "<Keyboard>/space" }              │
│           ]                                             │
│         }]                                              │
│       }                                                 │
│                                                         │
│ Game designers define WHAT inputs exist, not behavior  │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ CONTENT PLUGINS (Behavior - WHAT TO DO with inputs)     │
├─────────────────────────────────────────────────────────┤
│ projects/dungeon/.../src/DungeonGame.Console/           │
│   └─ InputHandlers/PlayerInputHandler.cs               │
│       - Subscribes to Input.Core callbacks             │
│       - OnMove(context) → player.Move()                │
│       - OnAttack(context) → combat.Attack()            │
│       - OnInteract(context) → pickup.Item()            │
│                                                         │
│ Game code defines WHAT TO DO when actions triggered    │
└─────────────────────────────────────────────────────────┘
```

**Key Insight:** Separate mechanism (engine), configuration (content-authoring), and behavior (content plugins).

## Detailed Design

### Phase 1: Delete Redundant Wrapper

**Goal:** Remove `PigeonPea.Shared.Input` as it serves no purpose.

**Actions:**

1. **Verify existence:**
   ```bash
   ls dotnet/app-essential/core/src/PigeonPea.Shared.Input
   ```

2. **Delete project (if exists):**
   ```bash
   rm -rf dotnet/app-essential/core/src/PigeonPea.Shared.Input/
   ```

3. **Update references:**
   - `PigeonPea.Plugins.Input.UniInputSystem.csproj`:
     ```diff
     - <ProjectReference Include="..\..\core\src\PigeonPea.Shared.Input\PigeonPea.Shared.Input.csproj" />
     + <!-- Reference Input.Core directly from engine layer -->
     + <ProjectReference Include="..\..\..\..\engine\core\src\PigeonPea.Input.Core\PigeonPea.Input.Core.csproj" />
     ```

4. **Verify no other references:**
   ```bash
   grep -r "PigeonPea.Shared.Input" dotnet/
   ```

**Validation:**
- [ ] PigeonPea.Shared.Input directory deleted (or confirmed missing)
- [ ] UniInputSystem plugin builds successfully
- [ ] No broken references in solution

### Phase 2: Extract Platform Devices to Tier 4 Plugins

#### 2.1 Create ConsoleKeyboard Plugin

**New project:**

```
projects/dungeon/dotnet/console-app/plugins/
  └─ PigeonPea.Plugin.Input.ConsoleKeyboard/
      ├─ ConsoleKeyboardDevice.cs
      ├─ ConsoleKeyboardPlugin.cs
      ├─ plugin.json
      └─ PigeonPea.Plugin.Input.ConsoleKeyboard.csproj
```

**ConsoleKeyboardDevice.cs:**

```csharp
using PigeonPea.Input.Core.Bindings;
using PigeonPea.Input.Core.Controls;

namespace PigeonPea.Plugin.Input.ConsoleKeyboard;

/// <summary>
/// Console keyboard device (System.Console) - Tier 4 provider.
/// </summary>
public sealed class ConsoleKeyboardDevice : IInputDevice
{
    public string DeviceId => "Console-Keyboard";
    public string DeviceType => "Keyboard";

    private readonly Dictionary<string, bool> _keyStates = new();
    private ConsoleKeyInfo? _lastKey;

    public void Update()
    {
        if (Console.KeyAvailable)
        {
            _lastKey = Console.ReadKey(intercept: true);
            UpdateKeyState(_lastKey.Value.Key, true);
        }
        else if (_lastKey.HasValue)
        {
            UpdateKeyState(_lastKey.Value.Key, false);
            _lastKey = null;
        }
    }

    public bool IsControlActive(InputControlPath path)
    {
        if (path.DeviceType != "Keyboard") return false;
        // Note: keyName must match exact string used in inputactions (case-sensitive?)
        // Ensure MapConsoleKey returns strings compatible with inputactions
        var keyName = path.ControlName.ToLowerInvariant();
        return _keyStates.TryGetValue(keyName, out var value) && value;
    }

    public InputValue ReadControlValue(InputControlPath path)
    {
        var isActive = IsControlActive(path);
        return new InputValue(isActive);
    }

    private void UpdateKeyState(ConsoleKey key, bool isPressed)
    {
        var keyName = MapConsoleKey(key);
        _keyStates[keyName] = isPressed;
    }

    private static string MapConsoleKey(ConsoleKey key) => key switch
    {
        ConsoleKey.W => "w",
        ConsoleKey.A => "a",
        ConsoleKey.S => "s",
        ConsoleKey.D => "d",
        ConsoleKey.Spacebar => "space",
        ConsoleKey.Enter => "enter",
        ConsoleKey.Escape => "escape",
        ConsoleKey.UpArrow => "uparrow",
        ConsoleKey.DownArrow => "downarrow",
        ConsoleKey.LeftArrow => "leftarrow",
        ConsoleKey.RightArrow => "rightarrow",
        ConsoleKey.I => "i",
        ConsoleKey.E => "e",
        _ => key.ToString().ToLowerInvariant()
    };
}
```

**ConsoleKeyboardPlugin.cs:**

```csharp
using PigeonPea.Contracts.Plugin;
using Microsoft.Extensions.Logging;

namespace PigeonPea.Plugin.Input.ConsoleKeyboard;

public sealed class ConsoleKeyboardPlugin : IPlugin
{
    public string Id => "PigeonPea.Plugin.Input.ConsoleKeyboard";
    public string Name => "Console Keyboard Input Device";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context)
    {
        var logger = context.GetLogger<ConsoleKeyboardPlugin>();
        logger?.LogInformation("Console Keyboard device plugin initialized");

        // Register device as a provider
        var device = new ConsoleKeyboardDevice();
        context.Registry.Register<IInputDevice>(device, new ServiceMetadata
        {
            ServiceId = "ConsoleKeyboard",
            Priority = 100,
            PluginId = Id,
            Tags = new[] { "input", "keyboard", "console" }
        });

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }
}
```

**plugin.json:**

```json
{
  "id": "PigeonPea.Plugin.Input.ConsoleKeyboard",
  "name": "Console Keyboard Input Device",
  "version": "1.0.0",
  "description": "Console keyboard device provider for System.Console input",
  "author": "Pigeon Pea Development Team",
  "pluginType": "InputDevice",
  "entryPoint": "PigeonPea.Plugin.Input.ConsoleKeyboard.ConsoleKeyboardPlugin",
  "dependencies": [],
  "platforms": ["console"],
  "tags": ["input", "device", "keyboard", "tier-4"]
}
```

**PigeonPea.Plugin.Input.ConsoleKeyboard.csproj:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <!-- Engine layer -->
    <ProjectReference Include="..\..\..\..\..\..\engine\core\src\PigeonPea.Input.Core\PigeonPea.Input.Core.csproj" />

    <!-- App-essential contracts -->
    <ProjectReference Include="..\..\..\..\..\..\app-essential\core\src\PigeonPea.Contracts\PigeonPea.Contracts.csproj" />

    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <None Include="plugin.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <Target Name="CopyPluginToConsoleApp" AfterTargets="Build">
    <PropertyGroup>
      <PluginOutputPath>$(MSBuildThisFileDirectory)bin\$(Configuration)\$(TargetFramework)\</PluginOutputPath>
      <PluginTargetDir>$(MSBuildThisFileDirectory)..\..\core\src\PigeonPea.Console\bin\$(Configuration)\$(TargetFramework)\plugins\$(AssemblyName)\</PluginTargetDir>
    </PropertyGroup>
    <ItemGroup>
      <PluginFiles Include="$(PluginOutputPath)**\*.*" />
    </ItemGroup>
    <MakeDir Directories="$(PluginTargetDir)" />
    <Copy SourceFiles="@(PluginFiles)" DestinationFolder="$(PluginTargetDir)%(RecursiveDir)" SkipUnchangedFiles="true" />
  </Target>

</Project>
```

#### 2.2 Create SDL3Gamepad Plugin

**Update Directory.Packages.props:**
Before creating the project, ensure SDL3-CS is available.
```xml
<!-- projects/dungeon/dotnet/Directory.Packages.props -->
<PackageVersion Include="SDL3-CS" Version="3.0.0-preview*" />
```

**New project:**

```
projects/dungeon/dotnet/console-app/plugins/
  └─ PigeonPea.Plugin.Input.SDL3Gamepad/
      ├─ SDL3GamepadDevice.cs
      ├─ SDL3GamepadPlugin.cs
      ├─ plugin.json
      └─ PigeonPea.Plugin.Input.SDL3Gamepad.csproj
```

**SDL3GamepadDevice.cs:**

```csharp
using PigeonPea.Input.Core.Bindings;
using PigeonPea.Input.Core.Controls;
using SDL3; // Assuming SDL3 bindings package

namespace PigeonPea.Plugin.Input.SDL3Gamepad;

/// <summary>
/// SDL3 gamepad device - Tier 4 provider.
/// </summary>
public sealed class SDL3GamepadDevice : IInputDevice, IDisposable
{
    public string DeviceId => "SDL3-Gamepad";
    public string DeviceType => "Gamepad";

    private IntPtr _gamepad;
    private readonly Dictionary<string, float> _axisStates = new();
    private readonly Dictionary<string, bool> _buttonStates = new();

    public SDL3GamepadDevice()
    {
        // Initialize SDL3 gamepad subsystem
        SDL.SDL_Init(SDL.SDL_INIT_GAMEPAD);

        // Open first available gamepad
        var numGamepads = SDL.SDL_NumJoysticks();
        if (numGamepads > 0)
        {
            _gamepad = SDL.SDL_GameControllerOpen(0);
        }
    }

    public void Update()
    {
        if (_gamepad == IntPtr.Zero) return;

        SDL.SDL_GameControllerUpdate();

        // Update axes
        _axisStates["leftStickX"] = SDL.SDL_GameControllerGetAxis(_gamepad, SDL.SDL_CONTROLLER_AXIS_LEFTX) / 32768f;
        _axisStates["leftStickY"] = SDL.SDL_GameControllerGetAxis(_gamepad, SDL.SDL_CONTROLLER_AXIS_LEFTY) / 32768f;
        _axisStates["rightStickX"] = SDL.SDL_GameControllerGetAxis(_gamepad, SDL.SDL_CONTROLLER_AXIS_RIGHTX) / 32768f;
        _axisStates["rightStickY"] = SDL.SDL_GameControllerGetAxis(_gamepad, SDL.SDL_CONTROLLER_AXIS_RIGHTY) / 32768f;

        // Update buttons
        _buttonStates["a"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_A) > 0;
        _buttonStates["b"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_B) > 0;
        _buttonStates["x"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_X) > 0;
        _buttonStates["y"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_Y) > 0;
        // ... other buttons
    }

    public bool IsControlActive(InputControlPath path)
    {
        if (path.DeviceType != "Gamepad") return false;

        var controlName = path.ControlName.ToLowerInvariant();

        // Check buttons
        if (_buttonStates.TryGetValue(controlName, out var buttonValue))
            return buttonValue;

        // Check axes (consider active if > deadzone)
        if (_axisStates.TryGetValue(controlName, out var axisValue))
            return Math.Abs(axisValue) > 0.2f;

        return false;
    }

    public InputValue ReadControlValue(InputControlPath path)
    {
        if (path.DeviceType != "Gamepad")
            return new InputValue(false);

        var controlName = path.ControlName.ToLowerInvariant();

        // Read button
        if (_buttonStates.TryGetValue(controlName, out var buttonValue))
            return new InputValue(buttonValue);

        // Read axis
        if (_axisStates.TryGetValue(controlName, out var axisValue))
            return new InputValue(axisValue);

        return new InputValue(0f);
    }

    public void Dispose()
    {
        if (_gamepad != IntPtr.Zero)
        {
            SDL.SDL_GameControllerClose(_gamepad);
            _gamepad = IntPtr.Zero;
        }
        SDL.SDL_Quit();
    }
}
```

**SDL3GamepadPlugin.cs:**

```csharp
using PigeonPea.Contracts.Plugin;
using Microsoft.Extensions.Logging;

namespace PigeonPea.Plugin.Input.SDL3Gamepad;

public sealed class SDL3GamepadPlugin : IPlugin
{
    public string Id => "PigeonPea.Plugin.Input.SDL3Gamepad";
    public string Name => "SDL3 Gamepad Input Device";
    public string Version => "1.0.0";

    private SDL3GamepadDevice? _device;

    public Task InitializeAsync(IPluginContext context)
    {
        var logger = context.GetLogger<SDL3GamepadPlugin>();
        logger?.LogInformation("SDL3 Gamepad device plugin initialized");

        // Register device as a provider
        _device = new SDL3GamepadDevice();
        context.Registry.Register<IInputDevice>(_device, new ServiceMetadata
        {
            ServiceId = "SDL3Gamepad",
            Priority = 100,
            PluginId = Id,
            Tags = new[] { "input", "gamepad", "sdl3" }
        });

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _device?.Dispose();
        return Task.CompletedTask;
    }
}
```

**plugin.json:**

```json
{
  "id": "PigeonPea.Plugin.Input.SDL3Gamepad",
  "name": "SDL3 Gamepad Input Device",
  "version": "1.0.0",
  "description": "SDL3 gamepad device provider for cross-platform gamepad support",
  "author": "Pigeon Pea Development Team",
  "pluginType": "InputDevice",
  "entryPoint": "PigeonPea.Plugin.Input.SDL3Gamepad.SDL3GamepadPlugin",
  "dependencies": ["SDL3-CS"],
  "platforms": ["console", "windows"],
  "tags": ["input", "device", "gamepad", "sdl3", "tier-4"]
}
```

**PigeonPea.Plugin.Input.SDL3Gamepad.csproj:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <!-- Engine layer -->
    <ProjectReference Include="..\..\..\..\..\..\engine\core\src\PigeonPea.Input.Core\PigeonPea.Input.Core.csproj" />

    <!-- App-essential contracts -->
    <ProjectReference Include="..\..\..\..\..\..\app-essential\core\src\PigeonPea.Contracts\PigeonPea.Contracts.csproj" />

    <!-- SDL3 bindings -->
    <PackageReference Include="SDL3-CS" Version="*" />

    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <None Include="plugin.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <Target Name="CopyPluginToConsoleApp" AfterTargets="Build">
    <PropertyGroup>
      <PluginOutputPath>$(MSBuildThisFileDirectory)bin\$(Configuration)\$(TargetFramework)\</PluginOutputPath>
      <PluginTargetDir>$(MSBuildThisFileDirectory)..\..\core\src\PigeonPea.Console\bin\$(Configuration)\$(TargetFramework)\plugins\$(AssemblyName)\</PluginTargetDir>
    </PropertyGroup>
    <ItemGroup>
      <PluginFiles Include="$(PluginOutputPath)**\*.*" />
    </ItemGroup>
    <MakeDir Directories="$(PluginTargetDir)" />
    <Copy SourceFiles="@(PluginFiles)" DestinationFolder="$(PluginTargetDir)%(RecursiveDir)" SkipUnchangedFiles="true" />
  </Target>

</Project>
```

### Phase 3: Update Tier 3 Service to Discover Devices

**Goal:** UniInputSystemService discovers and uses Tier 4 device providers.

**Update UniInputSystemService.cs:**

```csharp
using PigeonPea.Input.Core;
using PigeonPea.Input.Core.Actions;
using PigeonPea.Input.Core.Controls;
using PigeonPea.Contracts.Input.Services;
using PigeonPea.Contracts.Plugin;
using Microsoft.Extensions.Logging;

namespace PigeonPea.Plugins.Input.UniInputSystem;

public sealed class UniInputSystemService : IService, IDisposable
{
    private readonly ILogger? _logger;
    private readonly IRegistry _registry;
    private readonly InputSystem _inputSystem;
    private readonly InputActionAsset _asset;
    private readonly Dictionary<string, bool> _buttonStates = new();
    private readonly Dictionary<string, float> _axisStates = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly object _sync = new();
    private double _lastUpdateSeconds;
    private bool _disposed;

    public UniInputSystemService(IRegistry registry, ILogger? logger)
    {
        _logger = logger;
        _registry = registry;
        _inputSystem = new InputSystem();

        // Discover and register Tier 4 device providers
        DiscoverAndRegisterDevices();

        // Load action maps from content-authoring
        var json = LoadDefaultPlayerControls();
        _asset = InputActionAssetJson.FromJson(json);
        _inputSystem.RegisterAsset(_asset);

        // Enable and register callbacks
        foreach (var map in _asset.ActionMaps)
        {
            RegisterCallbacks(map);
            map.Enable();
        }
    }

    private void DiscoverAndRegisterDevices()
    {
        // Get all registered IInputDevice providers (Tier 4)
        var devices = _registry.GetAll<IInputDevice>();

        _logger?.LogInformation("Discovered {Count} input device providers", devices.Count());

        foreach (var device in devices)
        {
            _logger?.LogInformation("Registering input device: {DeviceId} ({DeviceType})",
                device.DeviceId, device.DeviceType);
            _inputSystem.RegisterDevice(device);
        }

        if (!devices.Any())
        {
            _logger?.LogWarning("No input device providers found! Input will not work.");
        }
    }

    private string LoadDefaultPlayerControls()
    {
        // TODO: In production, use IFileSystem service or Configuration
        // For now, assume file is copied to content/input/
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "content", "input", "DefaultPlayerControls.inputactions");
        
        if (File.Exists(path))
        {
            _logger?.LogInformation("Loading input controls from {Path}", path);
            return File.ReadAllText(path);
        }
        
        _logger?.LogWarning("Input controls file not found at {Path}. Using empty config.", path);
        return "{ \"maps\": [] }";
    }

    // ... rest of implementation (IsActionPressed, GetAxis, etc.)
    // Remove hardcoded ConsoleKeyboardDevice instantiation!
}
```

**Delete from UniInputSystemService:**

```diff
- var keyboard = new ConsoleKeyboardDevice();
- _inputSystem.RegisterDevice(keyboard);
```

### Phase 4: Delete Game.Input (Move to Content)

**Goal:** Remove game-specific input from game-essential layer.

**Actions:**

1. **Verify existence:**
   ```bash
   ls dotnet/game-essential/core/src/PigeonPea.Game.Input
   ```

2. **Delete project (if exists):**
   ```bash
   rm -rf dotnet/game-essential/core/src/PigeonPea.Game.Input/
   ```

3. **Move game behavior to content plugins:**

   Create in content layer:
   ```bash
   projects/dungeon/dotnet/console-app/core/src/DungeonGame.Console/
     └─ Input/
         └─ PlayerInputHandler.cs
   ```

   **PlayerInputHandler.cs:**

   ```csharp
   using PigeonPea.Input.Core;
   using PigeonPea.Input.Core.Actions;
   using PigeonPea.Contracts.Input.Services;
   using SadRogue.Primitives;

   namespace DungeonGame.Console.Input;

   /// <summary>
   /// Handles player input for dungeon game (behavior layer).
   /// Uses Unity Input System pattern: subscribe to actions and define behavior.
   /// </summary>
   public sealed class PlayerInputHandler
   {
       private readonly IService _inputService;
       private readonly GameWorld _gameWorld;
       private readonly InputActionAsset _playerControls;

       public PlayerInputHandler(IService inputService, GameWorld gameWorld)
       {
           _inputService = inputService;
           _gameWorld = gameWorld;

           // Load action map (from content-authoring)
           var json = LoadPlayerControls();
           _playerControls = InputActionAssetJson.FromJson(json);

           // Subscribe to actions and define BEHAVIOR
           RegisterBehaviors();
       }

       private void RegisterBehaviors()
       {
           var gameplayMap = _playerControls.GetMap("Gameplay");
           if (gameplayMap == null) return;

           // Move action → move player in dungeon
           var moveAction = gameplayMap.GetAction("Move");
           moveAction?.OnPerformed(context =>
           {
               var direction = context.ReadValue<Vector2>();
               var point = new Point((int)direction.X, (int)direction.Y);
               _gameWorld.TryMovePlayer(point);
           });

           // Attack action → perform attack
           var attackAction = gameplayMap.GetAction("Attack");
           attackAction?.OnPerformed(context =>
           {
               _gameWorld.TryAttack();
           });

           // Interact action → pickup item
           var interactAction = gameplayMap.GetAction("Interact");
           interactAction?.OnPerformed(context =>
           {
               _gameWorld.TryPickupItem();
           });

           // Inventory action → open inventory
           var inventoryAction = gameplayMap.GetAction("Inventory");
           inventoryAction?.OnPerformed(context =>
           {
               _gameWorld.TryUseItem(0);
           });

           // Enable gameplay map
           gameplayMap.Enable();
       }

       private string LoadPlayerControls()
       {
           // Load from content-authoring embedded resource
           var assembly = typeof(PlayerInputHandler).Assembly;
           var resourceName = "DungeonGame.Console.Assets.DefaultPlayerControls.inputactions";

           using var stream = assembly.GetManifestResourceStream(resourceName);
           if (stream == null)
               throw new InvalidOperationException($"Could not find: {resourceName}");

           using var reader = new StreamReader(stream);
           return reader.ReadToEnd();
       }

       public void Update(double deltaTime)
       {
           // Input system handles device updates automatically
       }
   }
   ```

### Phase 5: Content-Authoring Action Maps

**Goal:** Centralize action map definitions in content-authoring.

**Create:**

```
projects/dungeon/content-authoring/input/
  └─ DefaultPlayerControls.inputactions
```

**DefaultPlayerControls.inputactions:**

```json
{
  "name": "DefaultPlayerControls",
  "maps": [
    {
      "name": "Gameplay",
      "id": "gameplay-map",
      "actions": [
        {
          "name": "Move",
          "type": "Vector2",
          "id": "move-action",
          "expectedControlType": "Vector2"
        },
        {
          "name": "Attack",
          "type": "Button",
          "id": "attack-action",
          "expectedControlType": "Button"
        },
        {
          "name": "Interact",
          "type": "Button",
          "id": "interact-action",
          "expectedControlType": "Button"
        },
        {
          "name": "Inventory",
          "type": "Button",
          "id": "inventory-action",
          "expectedControlType": "Button"
        },
        {
          "name": "Pause",
          "type": "Button",
          "id": "pause-action",
          "expectedControlType": "Button"
        }
      ],
      "bindings": [
        {
          "action": "Move",
          "path": "<Keyboard>/w",
          "interactions": "",
          "processors": "",
          "groups": "",
          "composite": "2DVector",
          "compositeType": "Vector2",
          "compositeParts": [
            { "name": "up", "path": "<Keyboard>/w" },
            { "name": "down", "path": "<Keyboard>/s" },
            { "name": "left", "path": "<Keyboard>/a" },
            { "name": "right", "path": "<Keyboard>/d" }
          ]
        },
        {
          "action": "Move",
          "path": "<Keyboard>/uparrow",
          "composite": "2DVector",
          "compositeType": "Vector2",
          "compositeParts": [
            { "name": "up", "path": "<Keyboard>/uparrow" },
            { "name": "down", "path": "<Keyboard>/downarrow" },
            { "name": "left", "path": "<Keyboard>/leftarrow" },
            { "name": "right", "path": "<Keyboard>/rightarrow" }
          ]
        },
        {
          "action": "Move",
          "path": "<Gamepad>/leftStick"
        },
        {
          "action": "Attack",
          "path": "<Keyboard>/space"
        },
        {
          "action": "Attack",
          "path": "<Gamepad>/buttonSouth"
        },
        {
          "action": "Interact",
          "path": "<Keyboard>/e"
        },
        {
          "action": "Interact",
          "path": "<Gamepad>/buttonWest"
        },
        {
          "action": "Inventory",
          "path": "<Keyboard>/i"
        },
        {
          "action": "Inventory",
          "path": "<Gamepad>/buttonNorth"
        },
        {
          "action": "Pause",
          "path": "<Keyboard>/escape"
        },
        {
          "action": "Pause",
          "path": "<Gamepad>/start"
        }
      ]
    },
    {
      "name": "UI",
      "id": "ui-map",
      "actions": [
        {
          "name": "Navigate",
          "type": "Vector2",
          "expectedControlType": "Vector2"
        },
        {
          "name": "Submit",
          "type": "Button",
          "expectedControlType": "Button"
        },
        {
          "name": "Cancel",
          "type": "Button",
          "expectedControlType": "Button"
        }
      ],
      "bindings": [
        {
          "action": "Navigate",
          "path": "<Keyboard>/w",
          "composite": "2DVector",
          "compositeParts": [
            { "name": "up", "path": "<Keyboard>/w" },
            { "name": "down", "path": "<Keyboard>/s" },
            { "name": "left", "path": "<Keyboard>/a" },
            { "name": "right", "path": "<Keyboard>/d" }
          ]
        },
        {
          "action": "Navigate",
          "path": "<Gamepad>/leftStick"
        },
        {
          "action": "Submit",
          "path": "<Keyboard>/enter"
        },
        {
          "action": "Submit",
          "path": "<Gamepad>/buttonSouth"
        },
        {
          "action": "Cancel",
          "path": "<Keyboard>/escape"
        },
        {
          "action": "Cancel",
          "path": "<Gamepad>/buttonEast"
        }
      ]
    }
  ],
  "controlSchemes": []
}
```

**Note:** Game designers edit this file to define/modify input mappings.

## Implementation Strategy

### Incremental Approach (App Always Functional)

**Week 1: Preparation**

1. **Day 1: Create platform device plugins**
   - Create `PigeonPea.Plugin.Input.ConsoleKeyboard`
   - Create `PigeonPea.Plugin.Input.SDL3Gamepad`
   - Verify they build and load successfully
   - Test in parallel with existing code

2. **Day 2: Update Tier 3 service**
   - Modify `UniInputSystemService` to discover devices via registry
   - Keep old hardcoded device as fallback
   - Verify device discovery works

3. **Day 3: Testing**
   - Test console app with new device plugins
   - Verify keyboard and gamepad input works
   - Compare behavior with old code

**Week 2: Migration**

1. **Day 1: Remove device from Tier 3**
   - Delete `ConsoleKeyboardDevice` from `UniInputSystem` plugin
   - Verify Tier 4 providers are used
   - Test thoroughly

2. **Day 2: Create content-authoring action maps**
   - Move `.inputactions` files to content-authoring
   - Create `PlayerInputHandler` in content plugin
   - Test game behavior

3. **Day 3: Delete Game.Input**
   - Remove `PigeonPea.Game.Input` project
   - Verify no broken references
   - Update content plugins to handle behavior

**Week 3: Cleanup**

1. **Day 1: Delete Shared.Input wrapper**
   - Remove `PigeonPea.Shared.Input` project
   - Update references to use Input.Core directly
   - Verify builds

2. **Day 2: Documentation**
   - Update architecture docs
   - Document plugin creation process
   - Add code examples

3. **Day 3: Validation**
   - Full regression testing
   - Verify all input scenarios work
   - Performance testing

### Validation Checkpoints

**After each week:**

- [ ] Console app starts without errors
- [ ] Keyboard input works in dungeon
- [ ] Gamepad input works (if SDL3 plugin loaded)
- [ ] Action maps load from content-authoring
- [ ] Player movement, attack, interact all work
- [ ] No references to deleted projects
- [ ] Plugins load successfully

## Testing Strategy

### Unit Tests

1. **Device Plugin Tests:**

   ```csharp
   [Test]
   public void ConsoleKeyboardDevice_UpdatesKeyStates()
   {
       var device = new ConsoleKeyboardDevice();

       // Simulate key press
       // ... (mock Console.ReadKey)

       device.Update();

       var path = new InputControlPath { DeviceType = "Keyboard", ControlName = "w" };
       Assert.IsTrue(device.IsControlActive(path));
   }
   ```

2. **Service Discovery Tests:**

   ```csharp
   [Test]
   public async Task UniInputSystemService_DiscoversDeviceProviders()
   {
       var registry = new PluginRegistry();
       var device = new ConsoleKeyboardDevice();
       registry.Register<IInputDevice>(device, metadata);

       var service = new UniInputSystemService(registry, null);

       // Verify device was registered with InputSystem
       // ...
   }
   ```

### Integration Tests

1. **Full Input Stack:**

   ```csharp
   [Test]
   public async Task FullInputStack_KeyboardToGameAction()
   {
       // Load all plugins
       await LoadPlugins();

       // Get input service
       var inputService = registry.Get<IService>();

       // Simulate key press
       SimulateKeyPress(ConsoleKey.W);

       // Check action is pressed
       Assert.IsTrue(inputService.IsActionPressed("Move"));

       var y = inputService.GetAxis("MoveY");
       Assert.AreEqual(1.0f, y, 0.01f);
   }
   ```

### Manual Tests

1. **Console App Input:**
   - Start dungeon console app
   - Press WASD → player moves
   - Press Space → player attacks
   - Press E → player interacts
   - Press Escape → game pauses

2. **Gamepad Input (if available):**
   - Connect gamepad
   - Left stick → player moves
   - A button → player attacks
   - X button → player interacts

3. **Device Hot-Swap:**
   - Start with keyboard only
   - Connect gamepad
   - Verify both work simultaneously

## Migration Path

### For Existing Code

1. **Projects using Game.Input:**
   - Move input handling to content plugins
   - Use `IService` from app-essential
   - Subscribe to Input.Core callbacks directly

2. **Projects with custom devices:**
   - Extract device to Tier 4 plugin
   - Register via `IRegistry`
   - Follow plugin structure

3. **Action map definitions:**
   - Move to content-authoring directory
   - Embed as resources in content plugins
   - Game designers maintain these files

### For New Features

**Adding new platform device:**

```
1. Create plugin: PigeonPea.Plugin.Input.{DeviceName}
2. Implement IInputDevice
3. Register in plugin.json
4. Tier 3 service discovers automatically!
```

**Adding new game action:**

```
1. Edit content-authoring/.inputactions file
2. Add action and bindings
3. Subscribe to action in content plugin behavior code
4. No changes to Tier 1-3 needed!
```

## Success Criteria

- [ ] `PigeonPea.Shared.Input` deleted
- [ ] `PigeonPea.Game.Input` deleted
- [ ] `ConsoleKeyboardDevice` exists only in Tier 4 plugin
- [ ] `SDL3GamepadDevice` implemented as Tier 4 plugin
- [ ] `UniInputSystemService` discovers devices via registry
- [ ] Action maps centralized in content-authoring
- [ ] Game behavior in content plugins (not shared layer)
- [ ] Console app works with keyboard
- [ ] Console app works with gamepad (if SDL3 plugin loaded)
- [ ] No ALC issues
- [ ] Following Unity Input System pattern
- [ ] Architecture documentation updated

## Future Enhancements

1. **Additional Device Providers:**
   - Mouse input device (console)
   - Touch input device (mobile)
   - VR controller device
   - Custom hardware devices

2. **Device Hot-Swap:**
   - Detect device connect/disconnect
   - Re-bind actions dynamically
   - Notify game of device changes

3. **Input Rebinding:**
   - Runtime binding configuration
   - Save/load custom bindings
   - UI for rebinding

4. **Input Recording/Playback:**
   - Record input sessions
   - Playback for testing
   - Demo mode

5. **Accessibility:**
   - Alternative input schemes
   - Assistive device support
   - Customizable sensitivity/dead zones

## References

- [RFC-006: Plugin System Architecture](./006-plugin-system-architecture.md)
- [RFC-013: Plugin Architecture Refinement (Tier-Based)](./013-plugin-architecture-refinement-tiered.md)
- [ADR-003: Service Tiers and Category Layout](../adr/ADR-0003-service-tiers.md)
- [Unity Input System Documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/index.html)
- [SDL3 Documentation](https://wiki.libsdl.org/SDL3/FrontPage)

## Appendix A: Project Structure

### Before (Current)

```
dotnet/
├─ engine/core/src/
│   └─ PigeonPea.Input.Core/          ✅ Engine runtime
│
├─ app-essential/core/src/
│   ├─ PigeonPea.Contracts/
│   │   └─ Services/Input/            ✅ Tier 1
│   │       └─ Proxy/                 ✅ Tier 2
│   │
│   └─ PigeonPea.Shared.Input/        ❌ Empty wrapper
│
├─ app-essential/plugins/src/
│   └─ PigeonPea.Plugins.Input.UniInputSystem/  ✅ Tier 3
│       └─ ConsoleKeyboardDevice.cs   ⚠️  Should be Tier 4
│
└─ game-essential/core/src/
    └─ PigeonPea.Game.Input/          ❌ Wrong layer
        ├─ Devices/
        │   └─ ConsoleKeyboardDevice.cs  ❌ Duplicate
        ├─ Events/
        │   ├─ MoveInputEvent.cs      ⚠️  Game behavior
        │   ├─ AttackInputEvent.cs    ⚠️  Should be in content
        │   └─ InteractInputEvent.cs  ⚠️  Should be in content
        └─ Integration/
            └─ GameWorldInputIntegration.cs  ⚠️  Should be in content
```

### After (Target)

```
dotnet/
├─ engine/core/src/
│   └─ PigeonPea.Input.Core/          ✅ Engine runtime
│       ├─ InputSystem, InputAction
│       └─ IInputDevice (interface only)
│
├─ app-essential/core/src/
│   └─ PigeonPea.Contracts/
│       └─ Services/Input/            ✅ Tier 1 + 2
│           ├─ IService.cs
│           └─ Proxy/Service.cs
│
└─ app-essential/plugins/src/
    └─ PigeonPea.Plugins.Input.UniInputSystem/  ✅ Tier 3
        └─ UniInputSystemService.cs
            - Discovers Tier 4 devices via registry
            - NO device implementations!

projects/dungeon/
├─ content-authoring/input/
│   └─ DefaultPlayerControls.inputactions  ✅ Configuration
│
├─ dotnet/console-app/plugins/
│   ├─ PigeonPea.Plugin.Input.ConsoleKeyboard/  ✅ Tier 4
│   │   └─ ConsoleKeyboardDevice.cs
│   │
│   └─ PigeonPea.Plugin.Input.SDL3Gamepad/      ✅ Tier 4
│       └─ SDL3GamepadDevice.cs
│
└─ dotnet/console-app/core/src/DungeonGame.Console/
    └─ Input/
        └─ PlayerInputHandler.cs      ✅ Game behavior
            - OnMove() → move player
            - OnAttack() → attack
```

## Appendix B: Flow Diagram

```
┌─────────────────────────────────────────────────────────┐
│ CONTENT-AUTHORING (Configuration)                       │
│ DefaultPlayerControls.inputactions                      │
│   - Defines: Move, Attack, Interact actions            │
│   - Bindings: WASD, arrows, gamepad stick, etc.        │
└─────────────────────────────────────────────────────────┘
                        ↓ loaded by
┌─────────────────────────────────────────────────────────┐
│ TIER 3: UniInputSystemService                           │
│   - Loads action maps                                   │
│   - Discovers Tier 4 device providers from registry    │
│   - Wraps Input.Core runtime                           │
└─────────────────────────────────────────────────────────┘
                        ↓ uses
┌─────────────────────────────────────────────────────────┐
│ ENGINE: PigeonPea.Input.Core                            │
│   - InputSystem.Update()                                │
│   - Processes device input                             │
│   - Triggers action callbacks                          │
└─────────────────────────────────────────────────────────┘
         ↓ reads                        ↑ calls callbacks
┌──────────────────────┐       ┌─────────────────────────┐
│ TIER 4: Devices      │       │ CONTENT: Behavior       │
│                      │       │                         │
│ ConsoleKeyboard      │       │ PlayerInputHandler      │
│ SDL3Gamepad          │       │   OnMove() → move       │
│ (discovered from     │       │   OnAttack() → attack   │
│  registry)           │       │   (subscribes to        │
└──────────────────────┘       │    Input.Core)          │
                               └─────────────────────────┘
```

**Key:**
- **Configuration** (content-authoring): WHAT inputs exist
- **Mechanism** (engine): HOW to read input
- **Service** (Tier 3): Orchestration
- **Providers** (Tier 4): Platform-specific devices
- **Behavior** (content plugins): WHAT TO DO with inputs

---

**End of RFC-023**
