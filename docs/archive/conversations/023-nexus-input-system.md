---
created: '2025-01-17'
doc_id: ''
doc_type: rfc
status: archived
summary: Comprehensive implementation guide for Nexus-Input, a Unity Input System-inspired
  input management library with action maps, JSON configuration, and engine-agnostic
  core
tags:
- input
- controls
- architecture
- library
- nexus-input
title: 'Nexus-Input: Unity-Inspired Input System'
---




# RFC-023: Nexus-Input - Unity-Inspired Input System

## Executive Summary

This RFC defines the complete implementation of **Nexus-Input**, a two-layer input management architecture inspired by Unity's Input System, consisting of:

1. **NexusInput.Core** - Engine-agnostic C# input abstraction (`_lib/nexus-input`)
2. **PigeonPea.Game.Input** - Platform-specific integration layer (`game-essential`)

The system provides action maps, rebindable controls, JSON configuration, and clean separation between logical input (actions) and physical input (devices), adapted for console and desktop roguelike games using Terminal.Gui v2 and Avalonia.

## Motivation

### Problems Being Solved

1. **Direct device polling**: Current input is hard-coded `Console.ReadKey()` calls
2. **No action abstraction**: "Move" is directly tied to arrow keys, not rebindable
3. **No configuration**: Players can't customize controls
4. **Platform-specific code**: Console input ≠ GUI input, duplicated logic
5. **No composite inputs**: Can't combine WASD into a 2D vector
6. **Hard to extend**: Adding gamepad support requires rewriting input logic

### Goals

1. Create a **portable, engine-agnostic** input action system
2. Support **multiple input devices** (Keyboard, Mouse, Gamepad) with unified API
3. Enable **JSON-based configuration** (.inputactions files)
4. Provide **action maps** for context switching (Gameplay, UI, Menu)
5. Support **composite bindings** (WASD → Vector2)
6. Maintain **integration** with existing `GameWorld.Update()` loop
7. Follow **existing patterns** from `_lib` projects (nexus-gas, nexus-goap)

### Non-Goals

- Real-time networking (local input only)
- Visual editors (JSON configuration is sufficient)
- Unity/Unreal integration (those use their own systems)
- Mobile touch input (desktop/console focus)

## What is Unity Input System?

**Unity Input System** is a flexible, data-driven input solution where:

1. **Actions** represent logical inputs (e.g., "Jump", "Fire", "Move")
2. **Bindings** map physical inputs to actions (e.g., Keyboard/Space → "Jump")
3. **Action Maps** group related actions (e.g., "Gameplay", "UI")
4. **Composites** combine multiple inputs (e.g., WASD → 2D Vector)
5. **Callbacks** notify code when actions are triggered

### Input System vs Direct Polling

| Input System                   | Direct Polling          |
| ------------------------------ | ----------------------- |
| Action-based (logical)         | Device-based (physical) |
| Rebindable, configurable       | Hard-coded              |
| Platform-agnostic              | Platform-specific       |
| Data-driven (JSON)             | Code-driven             |
| Easier to extend (add gamepad) | Requires refactoring    |

**For a roguelike**: Input System is ideal because:

- Players can customize controls
- Easy to add gamepad support later
- Clean separation between game logic and input devices
- Turn-based gives time for input processing

## Architecture Overview

### Two-Layer Design

```
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                        │
│  PigeonPea.Console, PigeonPea.Windows                      │
│  (UI rendering, game loop)                                  │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              ECS INTEGRATION LAYER                          │
│  PigeonPea.Game.Input                                       │
│  - InputActionAssets (JSON-loaded configurations)           │
│  - Platform adapters (ConsoleInputAdapter, AvaloniaAdapter) │
│  - GameWorld.HandleInput() integration                      │
│  - Event publishing (MessagePipe)                           │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                 CORE LIBRARY LAYER                          │
│  NexusInput.Core (100% portable C#)                        │
│  - InputAction, InputActionMap, InputActionAsset            │
│  - InputBinding (control paths, composites)                 │
│  - Device abstraction (IInputDevice)                        │
│  - JSON serialization/deserialization                       │
│  - NO platform dependencies                                 │
└─────────────────────────────────────────────────────────────┘
```

### Directory Structure

```
dotnet/
├── _lib/
│   └── nexus-input/
│       ├── README.md
│       ├── LICENSE
│       ├── nexus-input.sln
│       ├── src/
│       │   └── NexusInput.Core/
│       │       ├── NexusInput.Core.csproj
│       │       ├── Actions/
│       │       │   ├── InputAction.cs
│       │       │   ├── InputActionMap.cs
│       │       │   ├── InputActionAsset.cs
│       │       │   └── InputActionType.cs
│       │       ├── Bindings/
│       │       │   ├── InputBinding.cs
│       │       │   ├── InputControlPath.cs
│       │       │   ├── BindingComposite.cs
│       │       │   └── CompositeType.cs
│       │       ├── Controls/
│       │       │   ├── IInputDevice.cs
│       │       │   ├── InputDeviceState.cs
│       │       │   ├── InputValue.cs
│       │       │   └── InputControlType.cs
│       │       ├── Json/
│       │       │   ├── InputActionAssetJson.cs
│       │       │   └── JsonSerializer.cs
│       │       ├── Events/
│       │       │   ├── InputActionCallback.cs
│       │       │   ├── InputActionPhase.cs
│       │       │   └── InputContext.cs
│       │       └── InputSystem.cs
│       └── tests/
│           └── NexusInput.Core.Tests/
│               ├── NexusInput.Core.Tests.csproj
│               ├── Actions/
│               ├── Bindings/
│               ├── Json/
│               └── IntegrationTests/
│
└── game-essential/
    └── core/
        ├── src/
        │   └── PigeonPea.Game.Input/
        │       ├── PigeonPea.Game.Input.csproj
        │       ├── Devices/
        │       │   ├── ConsoleKeyboardDevice.cs
        │       │   ├── ConsoleMouseDevice.cs
        │       │   └── AvaloniaInputDevice.cs
        │       ├── Assets/
        │       │   ├── DefaultPlayerControls.inputactions (JSON)
        │       │   ├── UIControls.inputactions
        │       │   └── DebugControls.inputactions
        │       ├── Integration/
        │       │   ├── InputWorldExtensions.cs
        │       │   └── GameWorldInputIntegration.cs
        │       └── Events/
        │           ├── MoveInputEvent.cs
        │           ├── AttackInputEvent.cs
        │           └── InteractInputEvent.cs
        └── tests/
            └── PigeonPea.Game.Input.Tests/
                ├── PigeonPea.Game.Input.Tests.csproj
                └── Integration/
```

## Core Concepts (NexusInput.Core)

### 1. Input Actions

**Input Actions** represent logical player intentions (e.g., "Move", "Jump", "Fire").

**Key Properties**:

- **Name**: Unique identifier (e.g., "Move", "Attack")
- **Type**: Button, Value (float/Vector2)
- **Bindings**: List of input bindings that trigger this action
- **Callbacks**: `started`, `performed`, `canceled` events

**Key Types**:

- `InputAction`: Single action definition
- `InputActionType`: Button, Value, PassThrough
- `InputActionPhase`: Disabled, Waiting, Started, Performed, Canceled

**Example**:

```csharp
var moveAction = new InputAction
{
    Name = "Move",
    Type = InputActionType.Value, // Expects Vector2
    ExpectedControlType = "Vector2"
};

// Register callbacks
moveAction.OnPerformed(context =>
{
    var direction = context.ReadValue<Vector2>();
    player.Move(direction);
});
```

### 2. Input Bindings

**Input Bindings** map physical inputs (keyboard keys, mouse buttons) to actions.

**Key Properties**:

- **Path**: Control path (e.g., "<Keyboard>/w", "<Mouse>/leftButton")
- **Action**: Target action name
- **Composite**: Optional composite type (2D Vector, 1D Axis)
- **Processors**: Optional value transformations (Invert, Scale, Clamp)

**Key Types**:

- `InputBinding`: Binding definition
- `InputControlPath`: Physical control identifier
- `BindingComposite`: Multi-binding combiner (WASD → Vector2)
- `CompositeType`: TwoDVector, OneDAxis, ButtonWithOneModifier

**Example**:

```csharp
// Simple binding
var fireBinding = new InputBinding
{
    Path = "<Keyboard>/space",
    Action = "Fire"
};

// Composite binding (WASD → Vector2)
var wasdComposite = new BindingComposite
{
    Type = CompositeType.TwoDVector,
    Name = "WASD",
    Bindings = new Dictionary<string, string>
    {
        { "up", "<Keyboard>/w" },
        { "down", "<Keyboard>/s" },
        { "left", "<Keyboard>/a" },
        { "right", "<Keyboard>/d" }
    }
};
```

### 3. Input Action Maps

**Action Maps** group related actions for different contexts (Gameplay, UI, Menu).

**Key Features**:

- **Enable/Disable**: Only one map active at a time
- **Action Collection**: All actions in this context
- **Binding Collection**: All bindings for these actions

**Key Types**:

- `InputActionMap`: Map definition
- `InputActionAsset`: Container for multiple maps

**Example**:

```csharp
var gameplayMap = new InputActionMap
{
    Name = "Gameplay",
    Actions = new List<InputAction>
    {
        new InputAction { Name = "Move", Type = InputActionType.Value },
        new InputAction { Name = "Attack", Type = InputActionType.Button },
        new InputAction { Name = "Interact", Type = InputActionType.Button }
    }
};

var uiMap = new InputActionMap
{
    Name = "UI",
    Actions = new List<InputAction>
    {
        new InputAction { Name = "Navigate", Type = InputActionType.Value },
        new InputAction { Name = "Submit", Type = InputActionType.Button },
        new InputAction { Name = "Cancel", Type = InputActionType.Button }
    }
};

// Switch contexts
gameplayMap.Enable();
uiMap.Disable();
```

### 4. Input Devices (Abstraction)

**Input Devices** provide a unified interface for different platforms.

**Key Interface**:

```csharp
public interface IInputDevice
{
    string DeviceId { get; }
    InputDeviceState State { get; }

    bool IsControlActive(InputControlPath path);
    InputValue ReadControlValue(InputControlPath path);
    void Update();
}
```

**Implementations**:

- `ConsoleKeyboardDevice`: Wraps `Console.ReadKey()`
- `ConsoleMouseDevice`: Terminal mouse events (if supported)
- `AvaloniaInputDevice`: Wraps Avalonia keyboard/mouse events
- `GamepadDevice`: Future gamepad support

**Example**:

```csharp
// Platform-agnostic device polling
public class InputSystem
{
    private List<IInputDevice> _devices = new();

    public void Update()
    {
        foreach (var device in _devices)
        {
            device.Update();
        }

        // Check if control is active
        if (_keyboard.IsControlActive("<Keyboard>/w"))
        {
            // Process input
        }
    }
}
```

### 5. JSON Configuration

**JSON .inputactions files** store action maps, actions, and bindings.

**Format**:

```json
{
  "name": "PlayerControls",
  "maps": [
    {
      "name": "Gameplay",
      "actions": [
        {
          "name": "Move",
          "type": "Value",
          "expectedControlType": "Vector2"
        },
        {
          "name": "Attack",
          "type": "Button"
        }
      ],
      "bindings": [
        {
          "name": "WASD",
          "path": "",
          "action": "Move",
          "composite": "2DVector",
          "compositeParts": [
            { "name": "up", "path": "<Keyboard>/w" },
            { "name": "down", "path": "<Keyboard>/s" },
            { "name": "left", "path": "<Keyboard>/a" },
            { "name": "right", "path": "<Keyboard>/d" }
          ]
        },
        {
          "path": "<Keyboard>/space",
          "action": "Attack"
        }
      ]
    }
  ]
}
```

**Loading**:

```csharp
var json = File.ReadAllText("PlayerControls.inputactions");
var asset = InputActionAsset.FromJson(json);

var gameplayMap = asset.GetMap("Gameplay");
gameplayMap.Enable();
```

## Core Library Implementation (Phase 1)

### Step 1.1: Create Project Structure

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib
mkdir nexus-input
cd nexus-input
mkdir src tests
cd src
mkdir NexusInput.Core
cd NexusInput.Core
mkdir Actions Bindings Controls Json Events
```

### Step 1.2: Create NexusInput.Core.csproj

**File**: `_lib/nexus-input/src/NexusInput.Core/NexusInput.Core.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>NexusInput</RootNamespace>

    <!-- NuGet Package Metadata -->
    <PackageId>NexusInput.Core</PackageId>
    <Version>0.1.0</Version>
    <Authors>Pigeon Pea Development Team</Authors>
    <Description>Engine-agnostic input system inspired by Unity Input System</Description>
    <PackageTags>gamedev;input;controls;actions</PackageTags>
    <RepositoryUrl>https://github.com/your-repo/nexus-input</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>

  <ItemGroup>
    <!-- JSON Serialization -->
    <PackageReference Include="System.Text.Json" Version="9.0.0" />
  </ItemGroup>

</Project>
```

### Step 1.3: Implement Input Actions

**File**: `_lib/nexus-input/src/NexusInput.Core/Actions/InputActionType.cs`

```csharp
namespace NexusInput.Actions;

/// <summary>
/// Type of input action.
/// </summary>
public enum InputActionType
{
    /// <summary>Single press/release (e.g., Jump, Fire)</summary>
    Button,

    /// <summary>Continuous value (e.g., Move stick, Mouse delta)</summary>
    Value,

    /// <summary>Continuous pass-through (no processing)</summary>
    PassThrough
}
```

**File**: `_lib/nexus-input/src/NexusInput.Core/Events/InputActionPhase.cs`

```csharp
namespace NexusInput.Events;

/// <summary>
/// Lifecycle phase of an input action.
/// </summary>
public enum InputActionPhase
{
    /// <summary>Action is disabled</summary>
    Disabled,

    /// <summary>Waiting for input</summary>
    Waiting,

    /// <summary>Input started (button pressed)</summary>
    Started,

    /// <summary>Input performed (threshold met)</summary>
    Performed,

    /// <summary>Input canceled (button released)</summary>
    Canceled
}
```

**File**: `_lib/nexus-input/src/NexusInput.Core/Controls/InputValue.cs`

```csharp
namespace NexusInput.Controls;

/// <summary>
/// Union type for input values (float, Vector2, bool).
/// </summary>
public readonly struct InputValue
{
    private readonly object? _value;
    public InputValueType Type { get; }

    public InputValue(bool value)
    {
        _value = value;
        Type = InputValueType.Button;
    }

    public InputValue(float value)
    {
        _value = value;
        Type = InputValueType.Axis;
    }

    public InputValue(Vector2 value)
    {
        _value = value;
        Type = InputValueType.Vector2;
    }

    public bool AsButton() => Type == InputValueType.Button ? (bool)_value! : throw new InvalidCastException();
    public float AsAxis() => Type == InputValueType.Axis ? (float)_value! : throw new InvalidCastException();
    public Vector2 AsVector2() => Type == InputValueType.Vector2 ? (Vector2)_value! : throw new InvalidCastException();

    public T Get<T>()
    {
        return Type switch
        {
            InputValueType.Button when typeof(T) == typeof(bool) => (T)_value!,
            InputValueType.Axis when typeof(T) == typeof(float) => (T)_value!,
            InputValueType.Vector2 when typeof(T) == typeof(Vector2) => (T)_value!,
            _ => throw new InvalidCastException($"Cannot cast {Type} to {typeof(T)}")
        };
    }

    public override string ToString() => _value?.ToString() ?? "null";
}

public enum InputValueType
{
    Button,
    Axis,
    Vector2
}

/// <summary>
/// Simple Vector2 struct (to avoid dependencies).
/// </summary>
public struct Vector2
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);

    public override string ToString() => $"({X:F2}, {Y:F2})";
}
```

**File**: `_lib/nexus-input/src/NexusInput.Core/Events/InputContext.cs`

```csharp
using NexusInput.Controls;

namespace NexusInput.Events;

/// <summary>
/// Context passed to action callbacks.
/// Contains input value and metadata.
/// </summary>
public sealed class InputContext
{
    public InputValue Value { get; }
    public InputActionPhase Phase { get; }
    public double Time { get; }
    public string ActionName { get; }

    public InputContext(string actionName, InputValue value, InputActionPhase phase, double time)
    {
        ActionName = actionName;
        Value = value;
        Phase = phase;
        Time = time;
    }

    public T ReadValue<T>() => Value.Get<T>();

    public override string ToString() => $"{ActionName} [{Phase}]: {Value}";
}
```

**File**: `_lib/nexus-input/src/NexusInput.Core/Actions/InputAction.cs`

```csharp
using NexusInput.Bindings;
using NexusInput.Events;

namespace NexusInput.Actions;

/// <summary>
/// Represents a logical input action (e.g., "Jump", "Fire").
/// </summary>
public sealed class InputAction
{
    public string Name { get; set; } = string.Empty;
    public InputActionType Type { get; set; } = InputActionType.Button;
    public string ExpectedControlType { get; set; } = "Button"; // "Button", "Axis", "Vector2"
    public InputActionPhase Phase { get; private set; } = InputActionPhase.Waiting;

    public List<InputBinding> Bindings { get; } = new();

    private readonly List<Action<InputContext>> _startedCallbacks = new();
    private readonly List<Action<InputContext>> _performedCallbacks = new();
    private readonly List<Action<InputContext>> _canceledCallbacks = new();

    /// <summary>
    /// Registers a callback for when the action starts.
    /// </summary>
    public void OnStarted(Action<InputContext> callback)
    {
        _startedCallbacks.Add(callback);
    }

    /// <summary>
    /// Registers a callback for when the action is performed.
    /// </summary>
    public void OnPerformed(Action<InputContext> callback)
    {
        _performedCallbacks.Add(callback);
    }

    /// <summary>
    /// Registers a callback for when the action is canceled.
    /// </summary>
    public void OnCanceled(Action<InputContext> callback)
    {
        _canceledCallbacks.Add(callback);
    }

    /// <summary>
    /// Triggers the action with the given value.
    /// </summary>
    internal void Trigger(InputValue value, InputActionPhase phase, double time)
    {
        Phase = phase;
        var context = new InputContext(Name, value, phase, time);

        var callbacks = phase switch
        {
            InputActionPhase.Started => _startedCallbacks,
            InputActionPhase.Performed => _performedCallbacks,
            InputActionPhase.Canceled => _canceledCallbacks,
            _ => null
        };

        if (callbacks != null)
        {
            foreach (var callback in callbacks)
            {
                callback(context);
            }
        }
    }

    public override string ToString() => $"{Name} ({Type})";
}
```

### Step 1.4: Implement Input Bindings

**File**: `_lib/nexus-input/src/NexusInput.Core/Bindings/InputControlPath.cs`

```csharp
namespace NexusInput.Bindings;

/// <summary>
/// Represents a path to a physical control (e.g., "<Keyboard>/w", "<Mouse>/leftButton").
/// Format: "<DeviceType>/controlName"
/// </summary>
public readonly struct InputControlPath : IEquatable<InputControlPath>
{
    public string Path { get; }
    public string DeviceType { get; }
    public string ControlName { get; }

    public InputControlPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Control path cannot be empty", nameof(path));

        Path = path;

        // Parse "<DeviceType>/controlName"
        if (path.StartsWith('<') && path.Contains('>'))
        {
            var parts = path.Split('/');
            DeviceType = parts[0].Trim('<', '>');
            ControlName = parts.Length > 1 ? parts[1] : string.Empty;
        }
        else
        {
            DeviceType = "Unknown";
            ControlName = path;
        }
    }

    public bool IsValid => !string.IsNullOrEmpty(DeviceType) && !string.IsNullOrEmpty(ControlName);

    public override string ToString() => Path;
    public override int GetHashCode() => Path.GetHashCode();
    public override bool Equals(object? obj) => obj is InputControlPath other && Equals(other);
    public bool Equals(InputControlPath other) => Path == other.Path;

    public static bool operator ==(InputControlPath left, InputControlPath right) => left.Equals(right);
    public static bool operator !=(InputControlPath left, InputControlPath right) => !left.Equals(right);

    public static implicit operator string(InputControlPath path) => path.Path;
    public static implicit operator InputControlPath(string path) => new(path);
}
```

**File**: `_lib/nexus-input/src/NexusInput.Core/Bindings/CompositeType.cs`

```csharp
namespace NexusInput.Bindings;

/// <summary>
/// Type of composite binding.
/// </summary>
public enum CompositeType
{
    None,

    /// <summary>Combines 4 buttons into 2D vector (WASD)</summary>
    TwoDVector,

    /// <summary>Combines 2 buttons into 1D axis (-, +)</summary>
    OneDAxis,

    /// <summary>Button with modifier (Ctrl+C)</summary>
    ButtonWithOneModifier
}
```

**File**: `_lib/nexus-input/src/NexusInput.Core/Bindings/BindingComposite.cs`

```csharp
namespace NexusInput.Bindings;

/// <summary>
/// Combines multiple inputs into a single value (e.g., WASD → Vector2).
/// </summary>
public sealed class BindingComposite
{
    public string Name { get; set; } = string.Empty;
    public CompositeType Type { get; set; } = CompositeType.None;

    /// <summary>
    /// Part bindings (e.g., "up" -> "<Keyboard>/w", "down" -> "<Keyboard>/s")
    /// </summary>
    public Dictionary<string, InputControlPath> Bindings { get; } = new();

    public override string ToString() => $"{Name} ({Type})";
}
```

**File**: `_lib/nexus-input/src/NexusInput.Core/Bindings/InputBinding.cs`

```csharp
namespace NexusInput.Bindings;

/// <summary>
/// Maps a physical control to an action.
/// </summary>
public sealed class InputBinding
{
    public string Name { get; set; } = string.Empty;
    public InputControlPath Path { get; set; }
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Optional composite (for multi-input bindings like WASD).
    /// </summary>
    public BindingComposite? Composite { get; set; }

    public bool IsComposite => Composite != null;

    public override string ToString() =>
        IsComposite ? $"{Name} (Composite: {Composite!.Type})" : $"{Path} → {Action}";
}
```

### Step 1.5: Implement Input Action Maps

**File**: `_lib/nexus-input/src/NexusInput.Core/Actions/InputActionMap.cs`

```csharp
namespace NexusInput.Actions;

/// <summary>
/// Collection of related actions (e.g., "Gameplay", "UI").
/// </summary>
public sealed class InputActionMap
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; private set; }

    public List<InputAction> Actions { get; } = new();

    /// <summary>
    /// Enables this action map.
    /// </summary>
    public void Enable()
    {
        Enabled = true;
    }

    /// <summary>
    /// Disables this action map.
    /// </summary>
    public void Disable()
    {
        Enabled = false;
    }

    /// <summary>
    /// Gets an action by name.
    /// </summary>
    public InputAction? GetAction(string name)
    {
        return Actions.FirstOrDefault(a => a.Name == name);
    }

    public override string ToString() => $"{Name} ({Actions.Count} actions, {(Enabled ? "Enabled" : "Disabled")})";
}
```

**File**: `_lib/nexus-input/src/NexusInput.Core/Actions/InputActionAsset.cs`

```csharp
namespace NexusInput.Actions;

/// <summary>
/// Container for multiple action maps (loaded from JSON).
/// </summary>
public sealed class InputActionAsset
{
    public string Name { get; set; } = string.Empty;
    public List<InputActionMap> ActionMaps { get; } = new();

    /// <summary>
    /// Gets an action map by name.
    /// </summary>
    public InputActionMap? GetMap(string name)
    {
        return ActionMaps.FirstOrDefault(m => m.Name == name);
    }

    /// <summary>
    /// Enables all action maps.
    /// </summary>
    public void EnableAllMaps()
    {
        foreach (var map in ActionMaps)
        {
            map.Enable();
        }
    }

    /// <summary>
    /// Disables all action maps.
    /// </summary>
    public void DisableAllMaps()
    {
        foreach (var map in ActionMaps)
        {
            map.Disable();
        }
    }

    public override string ToString() => $"{Name} ({ActionMaps.Count} maps)";
}
```

### Step 1.6: Implement JSON Serialization

**File**: `_lib/nexus-input/src/NexusInput.Core/Json/InputActionAssetJson.cs`

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusInput.Actions;
using NexusInput.Bindings;

namespace NexusInput.Json;

/// <summary>
/// JSON representation of InputActionAsset (matches Unity format).
/// </summary>
public sealed class InputActionAssetJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("maps")]
    public List<ActionMapJson> Maps { get; set; } = new();

    public sealed class ActionMapJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("actions")]
        public List<ActionJson> Actions { get; set; } = new();

        [JsonPropertyName("bindings")]
        public List<BindingJson> Bindings { get; set; } = new();
    }

    public sealed class ActionJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Button"; // "Button", "Value", "PassThrough"

        [JsonPropertyName("expectedControlType")]
        public string? ExpectedControlType { get; set; }
    }

    public sealed class BindingJson
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("composite")]
        public string? Composite { get; set; }

        [JsonPropertyName("compositeParts")]
        public List<CompositePartJson>? CompositeParts { get; set; }
    }

    public sealed class CompositePartJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }

    /// <summary>
    /// Converts JSON to InputActionAsset.
    /// </summary>
    public InputActionAsset ToInputActionAsset()
    {
        var asset = new InputActionAsset { Name = Name };

        foreach (var mapJson in Maps)
        {
            var map = new InputActionMap { Name = mapJson.Name };

            // Create actions
            foreach (var actionJson in mapJson.Actions)
            {
                var action = new InputAction
                {
                    Name = actionJson.Name,
                    Type = ParseActionType(actionJson.Type),
                    ExpectedControlType = actionJson.ExpectedControlType ?? actionJson.Type
                };
                map.Actions.Add(action);
            }

            // Create bindings
            foreach (var bindingJson in mapJson.Bindings)
            {
                var binding = new InputBinding
                {
                    Name = bindingJson.Name ?? string.Empty,
                    Path = new InputControlPath(bindingJson.Path),
                    Action = bindingJson.Action
                };

                // Handle composites
                if (!string.IsNullOrEmpty(bindingJson.Composite) && bindingJson.CompositeParts != null)
                {
                    var composite = new BindingComposite
                    {
                        Name = bindingJson.Name ?? bindingJson.Composite,
                        Type = ParseCompositeType(bindingJson.Composite)
                    };

                    foreach (var part in bindingJson.CompositeParts)
                    {
                        composite.Bindings[part.Name] = new InputControlPath(part.Path);
                    }

                    binding.Composite = composite;
                }

                // Add binding to action
                var action = map.GetAction(binding.Action);
                action?.Bindings.Add(binding);
            }

            asset.ActionMaps.Add(map);
        }

        return asset;
    }

    /// <summary>
    /// Converts InputActionAsset to JSON.
    /// </summary>
    public static InputActionAssetJson FromInputActionAsset(InputActionAsset asset)
    {
        var json = new InputActionAssetJson { Name = asset.Name };

        foreach (var map in asset.ActionMaps)
        {
            var mapJson = new ActionMapJson { Name = map.Name };

            // Convert actions
            foreach (var action in map.Actions)
            {
                mapJson.Actions.Add(new ActionJson
                {
                    Name = action.Name,
                    Type = action.Type.ToString(),
                    ExpectedControlType = action.ExpectedControlType
                });
            }

            // Convert bindings
            foreach (var action in map.Actions)
            {
                foreach (var binding in action.Bindings)
                {
                    var bindingJson = new BindingJson
                    {
                        Name = binding.Name,
                        Path = binding.Path.Path,
                        Action = binding.Action
                    };

                    if (binding.Composite != null)
                    {
                        bindingJson.Composite = binding.Composite.Type.ToString();
                        bindingJson.CompositeParts = binding.Composite.Bindings
                            .Select(kvp => new CompositePartJson { Name = kvp.Key, Path = kvp.Value.Path })
                            .ToList();
                    }

                    mapJson.Bindings.Add(bindingJson);
                }
            }

            json.Maps.Add(mapJson);
        }

        return json;
    }

    private static InputActionType ParseActionType(string type)
    {
        return type.ToLower() switch
        {
            "button" => InputActionType.Button,
            "value" => InputActionType.Value,
            "passthrough" => InputActionType.PassThrough,
            _ => InputActionType.Button
        };
    }

    private static CompositeType ParseCompositeType(string composite)
    {
        return composite.ToLower() switch
        {
            "2dvector" => CompositeType.TwoDVector,
            "1daxis" => CompositeType.OneDAxis,
            "buttonwithonemodifier" => CompositeType.ButtonWithOneModifier,
            _ => CompositeType.None
        };
    }

    /// <summary>
    /// Loads from JSON string.
    /// </summary>
    public static InputActionAsset FromJson(string json)
    {
        var jsonObj = JsonSerializer.Deserialize<InputActionAssetJson>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        });

        return jsonObj?.ToInputActionAsset() ?? new InputActionAsset();
    }

    /// <summary>
    /// Saves to JSON string.
    /// </summary>
    public static string ToJson(InputActionAsset asset)
    {
        var jsonObj = FromInputActionAsset(asset);
        return JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
```

### Step 1.7: Implement Device Abstraction

**File**: `_lib/nexus-input/src/NexusInput.Core/Controls/IInputDevice.cs`

```csharp
using NexusInput.Bindings;

namespace NexusInput.Controls;

/// <summary>
/// Platform-agnostic input device interface.
/// Implemented per-platform (Console, Avalonia, SDL, etc.).
/// </summary>
public interface IInputDevice
{
    string DeviceId { get; }
    string DeviceType { get; } // "Keyboard", "Mouse", "Gamepad"

    /// <summary>
    /// Checks if a control is currently active/pressed.
    /// </summary>
    bool IsControlActive(InputControlPath path);

    /// <summary>
    /// Reads the current value of a control.
    /// </summary>
    InputValue ReadControlValue(InputControlPath path);

    /// <summary>
    /// Updates device state (called each frame).
    /// </summary>
    void Update();
}
```

### Step 1.8: Implement InputSystem (Main Entry Point)

**File**: `_lib/nexus-input/src/NexusInput.Core/InputSystem.cs`

```csharp
using NexusInput.Actions;
using NexusInput.Bindings;
using NexusInput.Controls;
using NexusInput.Events;

namespace NexusInput;

/// <summary>
/// Main input system. Polls devices and triggers actions.
/// </summary>
public sealed class InputSystem
{
    private readonly List<IInputDevice> _devices = new();
    private readonly List<InputActionAsset> _assets = new();
    private double _currentTime = 0;

    /// <summary>
    /// Registers an input device.
    /// </summary>
    public void RegisterDevice(IInputDevice device)
    {
        _devices.Add(device);
    }

    /// <summary>
    /// Registers an input action asset.
    /// </summary>
    public void RegisterAsset(InputActionAsset asset)
    {
        _assets.Add(asset);
    }

    /// <summary>
    /// Updates all devices and processes input.
    /// Call once per frame/update.
    /// </summary>
    public void Update(double deltaTime)
    {
        _currentTime += deltaTime;

        // Update all devices
        foreach (var device in _devices)
        {
            device.Update();
        }

        // Process all enabled action maps
        foreach (var asset in _assets)
        {
            foreach (var map in asset.ActionMaps.Where(m => m.Enabled))
            {
                ProcessActionMap(map);
            }
        }
    }

    private void ProcessActionMap(InputActionMap map)
    {
        foreach (var action in map.Actions)
        {
            ProcessAction(action);
        }
    }

    private void ProcessAction(InputAction action)
    {
        foreach (var binding in action.Bindings)
        {
            if (binding.IsComposite)
            {
                ProcessCompositeBinding(action, binding);
            }
            else
            {
                ProcessSimpleBinding(action, binding);
            }
        }
    }

    private void ProcessSimpleBinding(InputAction action, InputBinding binding)
    {
        var device = _devices.FirstOrDefault(d => d.DeviceType == binding.Path.DeviceType);
        if (device == null) return;

        bool isActive = device.IsControlActive(binding.Path);

        if (isActive && action.Phase == InputActionPhase.Waiting)
        {
            // Button pressed (Started)
            var value = device.ReadControlValue(binding.Path);
            action.Trigger(value, InputActionPhase.Started, _currentTime);
            action.Trigger(value, InputActionPhase.Performed, _currentTime);
        }
        else if (!isActive && action.Phase == InputActionPhase.Performed)
        {
            // Button released (Canceled)
            action.Trigger(new InputValue(false), InputActionPhase.Canceled, _currentTime);
            action.Phase = InputActionPhase.Waiting;
        }
    }

    private void ProcessCompositeBinding(InputAction action, InputBinding binding)
    {
        var composite = binding.Composite!;

        if (composite.Type == CompositeType.TwoDVector)
        {
            // Read WASD inputs
            var up = ReadCompositePartValue(composite, "up");
            var down = ReadCompositePartValue(composite, "down");
            var left = ReadCompositePartValue(composite, "left");
            var right = ReadCompositePartValue(composite, "right");

            float x = (right ? 1f : 0f) - (left ? 1f : 0f);
            float y = (up ? 1f : 0f) - (down ? 1f : 0f);

            var vector = new Vector2(x, y);
            var value = new InputValue(vector);

            if (vector.X != 0 || vector.Y != 0)
            {
                action.Trigger(value, InputActionPhase.Performed, _currentTime);
            }
            else if (action.Phase == InputActionPhase.Performed)
            {
                action.Trigger(new InputValue(Vector2.Zero), InputActionPhase.Canceled, _currentTime);
            }
        }
    }

    private bool ReadCompositePartValue(BindingComposite composite, string partName)
    {
        if (!composite.Bindings.TryGetValue(partName, out var path))
            return false;

        var device = _devices.FirstOrDefault(d => d.DeviceType == path.DeviceType);
        if (device == null) return false;

        return device.IsControlActive(path);
    }
}
```

## Phase 1 Completion Checklist

- [ ] Project structure created
- [ ] All Actions classes implemented
- [ ] All Bindings classes implemented
- [ ] All Controls interfaces implemented
- [ ] All Events classes implemented
- [ ] JSON serialization/deserialization working
- [ ] InputSystem main class implemented
- [ ] Solution builds without errors

**Verification Command**:

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-input
dotnet build
```

Expected output: `Build succeeded. 0 Warning(s). 0 Error(s).`

---

## Phase 2: Platform Integration (PigeonPea.Game.Input)

### Step 2.1: Implement Console Device Adapter

**File**: `game-essential/core/src/PigeonPea.Game.Input/Devices/ConsoleKeyboardDevice.cs`

```csharp
using NexusInput.Bindings;
using NexusInput.Controls;

namespace PigeonPea.Game.Input.Devices;

/// <summary>
/// Console keyboard device (System.Console).
/// </summary>
public sealed class ConsoleKeyboardDevice : IInputDevice
{
    public string DeviceId => "Console-Keyboard";
    public string DeviceType => "Keyboard";

    private readonly Dictionary<string, bool> _keyStates = new();
    private ConsoleKeyInfo? _lastKey;

    public void Update()
    {
        // Poll console keyboard (non-blocking)
        if (Console.KeyAvailable)
        {
            _lastKey = Console.ReadKey(intercept: true);
            UpdateKeyState(_lastKey.Value.Key, true);
        }
        else
        {
            // Clear previous key state
            if (_lastKey.HasValue)
            {
                UpdateKeyState(_lastKey.Value.Key, false);
                _lastKey = null;
            }
        }
    }

    public bool IsControlActive(InputControlPath path)
    {
        if (path.DeviceType != "Keyboard") return false;

        var keyName = path.ControlName.ToLower();
        return _keyStates.GetValueOrDefault(keyName, false);
    }

    public InputValue ReadControlValue(InputControlPath path)
    {
        bool isActive = IsControlActive(path);
        return new InputValue(isActive);
    }

    private void UpdateKeyState(ConsoleKey key, bool isPressed)
    {
        var keyName = MapConsoleKey(key);
        _keyStates[keyName] = isPressed;
    }

    private string MapConsoleKey(ConsoleKey key)
    {
        return key switch
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
            _ => key.ToString().ToLower()
        };
    }
}
```

### Step 2.2: Create Default Player Controls

**File**: `game-essential/core/src/PigeonPea.Game.Input/Assets/DefaultPlayerControls.inputactions`

```json
{
  "name": "DefaultPlayerControls",
  "maps": [
    {
      "name": "Gameplay",
      "actions": [
        {
          "name": "Move",
          "type": "Value",
          "expectedControlType": "Vector2"
        },
        {
          "name": "Attack",
          "type": "Button"
        },
        {
          "name": "Interact",
          "type": "Button"
        },
        {
          "name": "Inventory",
          "type": "Button"
        },
        {
          "name": "Pause",
          "type": "Button"
        }
      ],
      "bindings": [
        {
          "name": "WASD",
          "path": "",
          "action": "Move",
          "composite": "2DVector",
          "compositeParts": [
            { "name": "up", "path": "<Keyboard>/w" },
            { "name": "down", "path": "<Keyboard>/s" },
            { "name": "left", "path": "<Keyboard>/a" },
            { "name": "right", "path": "<Keyboard>/d" }
          ]
        },
        {
          "name": "Arrow Keys",
          "path": "",
          "action": "Move",
          "composite": "2DVector",
          "compositeParts": [
            { "name": "up", "path": "<Keyboard>/uparrow" },
            { "name": "down", "path": "<Keyboard>/downarrow" },
            { "name": "left", "path": "<Keyboard>/leftarrow" },
            { "name": "right", "path": "<Keyboard>/rightarrow" }
          ]
        },
        {
          "path": "<Keyboard>/space",
          "action": "Attack"
        },
        {
          "path": "<Keyboard>/e",
          "action": "Interact"
        },
        {
          "path": "<Keyboard>/i",
          "action": "Inventory"
        },
        {
          "path": "<Keyboard>/escape",
          "action": "Pause"
        }
      ]
    },
    {
      "name": "UI",
      "actions": [
        {
          "name": "Navigate",
          "type": "Value",
          "expectedControlType": "Vector2"
        },
        {
          "name": "Submit",
          "type": "Button"
        },
        {
          "name": "Cancel",
          "type": "Button"
        }
      ],
      "bindings": [
        {
          "name": "Arrow Keys",
          "path": "",
          "action": "Navigate",
          "composite": "2DVector",
          "compositeParts": [
            { "name": "up", "path": "<Keyboard>/uparrow" },
            { "name": "down", "path": "<Keyboard>/downarrow" },
            { "name": "left", "path": "<Keyboard>/leftarrow" },
            { "name": "right", "path": "<Keyboard>/rightarrow" }
          ]
        },
        {
          "path": "<Keyboard>/enter",
          "action": "Submit"
        },
        {
          "path": "<Keyboard>/escape",
          "action": "Cancel"
        }
      ]
    }
  ]
}
```

### Step 2.3: Integrate with GameWorld

**File**: `game-essential/core/src/PigeonPea.Game.Input/Integration/GameWorldInputIntegration.cs`

```csharp
using MessagePipe;
using NexusInput;
using NexusInput.Actions;
using NexusInput.Json;
using PigeonPea.Game.Input.Devices;
using PigeonPea.Game.Input.Events;
using SadRogue.Primitives;

namespace PigeonPea.Game.Input.Integration;

/// <summary>
/// Integrates NexusInput with GameWorld.
/// </summary>
public sealed class GameWorldInputIntegration
{
    private readonly InputSystem _inputSystem;
    private readonly InputActionAsset _playerControls;
    private readonly GameWorld _gameWorld;

    private readonly IPublisher<MoveInputEvent>? _movePublisher;
    private readonly IPublisher<AttackInputEvent>? _attackPublisher;

    public GameWorldInputIntegration(
        GameWorld gameWorld,
        IPublisher<MoveInputEvent>? movePublisher = null,
        IPublisher<AttackInputEvent>? attackPublisher = null)
    {
        _gameWorld = gameWorld;
        _movePublisher = movePublisher;
        _attackPublisher = attackPublisher;

        _inputSystem = new InputSystem();

        // Register console keyboard device
        var keyboard = new ConsoleKeyboardDevice();
        _inputSystem.RegisterDevice(keyboard);

        // Load default player controls
        var json = LoadDefaultPlayerControls();
        _playerControls = InputActionAssetJson.FromJson(json);
        _inputSystem.RegisterAsset(_playerControls);

        // Enable gameplay map
        var gameplayMap = _playerControls.GetMap("Gameplay");
        gameplayMap?.Enable();

        // Register callbacks
        RegisterCallbacks(gameplayMap!);
    }

    private void RegisterCallbacks(InputActionMap gameplayMap)
    {
        // Move action
        var moveAction = gameplayMap.GetAction("Move");
        moveAction?.OnPerformed(context =>
        {
            var direction = context.ReadValue<NexusInput.Controls.Vector2>();
            var point = new Point((int)direction.X, (int)direction.Y);

            _gameWorld.TryMovePlayer(point);
            _movePublisher?.Publish(new MoveInputEvent { Direction = point });
        });

        // Attack action
        var attackAction = gameplayMap.GetAction("Attack");
        attackAction?.OnPerformed(context =>
        {
            // Attack in current direction (or wait for direction input)
            _attackPublisher?.Publish(new AttackInputEvent { Timestamp = (float)context.Time });
        });

        // Interact action
        var interactAction = gameplayMap.GetAction("Interact");
        interactAction?.OnPerformed(context =>
        {
            _gameWorld.TryPickupItem();
        });

        // Inventory action
        var inventoryAction = gameplayMap.GetAction("Inventory");
        inventoryAction?.OnPerformed(context =>
        {
            // Open inventory UI (publish event)
        });

        // Pause action
        var pauseAction = gameplayMap.GetAction("Pause");
        pauseAction?.OnPerformed(context =>
        {
            // Pause game (publish event)
        });
    }

    public void Update(double deltaTime)
    {
        _inputSystem.Update(deltaTime);
    }

    public void SwitchToUIMap()
    {
        _playerControls.GetMap("Gameplay")?.Disable();
        _playerControls.GetMap("UI")?.Enable();
    }

    public void SwitchToGameplayMap()
    {
        _playerControls.GetMap("UI")?.Disable();
        _playerControls.GetMap("Gameplay")?.Enable();
    }

    private string LoadDefaultPlayerControls()
    {
        // For simplicity, embed JSON or load from file
        // In production, read from Assets/DefaultPlayerControls.inputactions
        return File.ReadAllText("Assets/DefaultPlayerControls.inputactions");
    }
}
```

---

## Remaining Phases Summary

**Phase 3: Testing** (Week 1-2)

- Unit tests for InputAction, InputBinding, Composites
- JSON serialization round-trip tests
- Integration tests with GameWorld

**Phase 4: Advanced Features** (Week 2-3)

- Rebinding UI (runtime control changes)
- Input processors (Invert, Scale, Deadzone)
- Gamepad support (future)

**Phase 5: Integration with DOTween** (Week 3)

- Camera shake triggered by input (uses DOTween)
- UI animations on button press (uses DOTween)
- Smooth player movement (uses DOTween)

## DOTween Integration Notes

Since you're using **DOTween** directly, here's how NexusInput integrates:

### Example: Camera Shake on Attack

```csharp
using DG.Tweening;

// In GameWorldInputIntegration
attackAction?.OnPerformed(context =>
{
    // Trigger camera shake using DOTween
    var camera = _gameWorld.Camera;
    camera.Transform
        .DOShakePosition(duration: 0.2f, strength: 0.5f, vibrato: 10)
        .SetEase(Ease.OutQuad);

    _attackPublisher?.Publish(new AttackInputEvent { Timestamp = (float)context.Time });
});
```

### Example: Button Press Animation

```csharp
// In UI layer
submitAction?.OnPerformed(context =>
{
    // Animate button press using DOTween
    button.Transform
        .DOScale(0.9f, 0.1f)
        .SetLoops(2, LoopType.Yoyo)
        .SetEase(Ease.OutQuad);
});
```

### Example: Smooth Player Movement

```csharp
moveAction?.OnPerformed(context =>
{
    var direction = context.ReadValue<Vector2>();
    var targetPos = playerPos + new Vector2(direction.X * tileSize, direction.Y * tileSize);

    // Use DOTween for smooth movement
    player.Transform
        .DOMove(targetPos, 0.2f)
        .SetEase(Ease.OutQuad);
});
```

**Note**: DOTween is added as a NuGet package to the platform-specific projects (PigeonPea.Console, PigeonPea.Windows), not to NexusInput.Core.

---

## Success Criteria

- [ ] NexusInput.Core builds with minimal dependencies (System.Text.Json only)
- [ ] All unit tests passing (≥80% coverage)
- [ ] PigeonPea.Game.Input integrates with Arch ECS
- [ ] JSON .inputactions files load correctly
- [ ] WASD and arrow keys control player movement
- [ ] Action maps switch correctly (Gameplay ↔ UI)
- [ ] Composite bindings work (WASD → Vector2)
- [ ] Integration with DOTween for animations

## References

- **Unity Input System Docs**: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/
- **Unity Input System GitHub**: https://github.com/Unity-Technologies/InputSystem
- **Similar C# Projects**:
  - SDL2 Input: https://github.com/flibitijibibo/SDL2-CS
  - MonoGame Input: https://docs.monogame.net/articles/input.html
- **Existing Patterns**:
  - Nexus-GAS: RFC-019
  - Nexus-GOAP: RFC-020
- **DOTween Integration**: http://dotween.demigiant.com/documentation.php

## Appendix: Quick Start Commands

```bash
# Build NexusInput.Core
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-input
dotnet build

# Run NexusInput.Core tests
dotnet test

# Build PigeonPea.Game.Input
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\game-essential\core\src\PigeonPea.Game.Input
dotnet build

# Add to solution
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet
dotnet sln PigeonPea.sln add _lib\nexus-input\src\NexusInput.Core\NexusInput.Core.csproj
dotnet sln PigeonPea.sln add game-essential\core\src\PigeonPea.Game.Input\PigeonPea.Game.Input.csproj
```

---

**End of RFC-023: Nexus-Input Implementation Guide**

_This document provides complete implementation instructions for Phase 1-2. The system provides Unity-like input management with JSON configuration, action maps, and clean integration with existing game systems. DOTween is used separately in platform-specific projects for animations and effects._
