---
title: 'Nexus-Camera2D: ProCamera2D-Inspired 2D Camera System'
doc_type: 'rfc'
status: 'draft'
created: '2025-01-17'
tags: ['camera', '2d', 'rendering', 'architecture', 'library', 'nexus-camera2d']
summary: 'Comprehensive implementation guide for Nexus-Camera2D, a modular 2D camera system inspired by ProCamera2D with extensions for follow, boundaries, shake, zoom, and parallax'
---

# RFC-024: Nexus-Camera2D - Modular 2D Camera System

## Executive Summary

This RFC defines the complete implementation of **Nexus-Camera2D**, a modular two-layer 2D camera architecture inspired by ProCamera2D, consisting of:

1. **NexusCamera2D.Core** - Engine-agnostic C# 2D camera system (`_lib/nexus-camera2d`)
2. **PigeonPea.Game.Camera** - Integration with rendering pipeline (`game-essential`)

The system provides extensible camera behaviors (follow, boundaries, shake, zoom, parallax) through a modular extension pattern, adapted for console and desktop roguelike games with 2D tile-based rendering.

## Motivation

### Problems Being Solved

1. **Basic camera**: Current `Camera.cs` has minimal functionality (follow + clamp)
2. **No smooth movement**: Camera jumps instantly to player position
3. **No camera effects**: No shake, zoom, or screenshake feedback
4. **No multi-target support**: Can't follow multiple entities (for multiplayer/split-focus)
5. **Hard to extend**: Adding new camera behaviors requires modifying core camera class
6. **No parallax support**: Multi-layer backgrounds move at the same speed

### Goals

1. Create a **portable, engine-agnostic** 2D camera system
2. Support **modular extensions** (add only what you need, zero overhead for unused features)
3. Provide **smooth camera movement** (lerp, exponential damping, spring physics)
4. Enable **camera shake** (intensity, duration, frequency parameters)
5. Support **zoom in/out** with smooth transitions
6. Implement **parallax scrolling** for multi-layer backgrounds
7. Support **boundaries** (keep camera within level bounds)
8. Enable **pixel-perfect rendering** (snap to pixel grid for crisp 2D)
9. Maintain **integration** with existing rendering pipeline
10. Follow **existing patterns** from `_lib` projects (nexus-gas, nexus-goap, nexus-input)

### Non-Goals

- 3D camera support (use dedicated 3D camera for that)
- Split-screen rendering (can add later)
- Cinematic cutscenes (different system)
- Visual editors (code-configured is sufficient)

## What is ProCamera2D?

**ProCamera2D** is a Unity asset providing a modular 2D camera system where:

1. **Camera Core** handles basic rendering and positioning
2. **Extensions** add behaviors (follow, boundaries, shake, zoom, parallax)
3. **Triggers** activate effects based on spatial conditions
4. **Targets** define what the camera should follow with weighted influence

### ProCamera2D vs Basic Camera

| ProCamera2D | Basic Camera |
|-------------|--------------|
| Modular extensions | Monolithic class |
| Smooth movement (damping) | Instant follow |
| Camera shake, zoom effects | No effects |
| Multi-target support | Single target |
| Parallax scrolling | No parallax |
| Pixel-perfect rendering | No grid snapping |

**For a roguelike**: Modular camera is ideal because:
- Smooth follow feels more polished than instant snap
- Camera shake adds impact to attacks/spells
- Pixel-perfect ensures crisp tile rendering
- Extensible design allows adding behaviors without refactoring
- Parallax adds depth to dungeon backgrounds

## Architecture Overview

### Two-Layer Design

```
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                        │
│  PigeonPea.Console, PigeonPea.Windows                      │
│  (Rendering, camera viewport management)                    │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              ECS INTEGRATION LAYER                          │
│  PigeonPea.Game.Camera                                      │
│  - CameraComponent (ECS)                                    │
│  - CameraSystem (updates camera each frame)                 │
│  - Integration with DOTween for shake/zoom effects          │
│  - Integration with rendering pipeline                      │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                 CORE LIBRARY LAYER                          │
│  NexusCamera2D.Core (100% portable C#)                     │
│  - Camera2D (core camera controller)                        │
│  - Extensions (Follow, Boundaries, Shake, Zoom, Parallax)   │
│  - Targets (CameraTarget with weight and offset)            │
│  - Math (CameraTransform, Rect, Vector2 helpers)            │
│  - NO external dependencies (pure C#)                       │
└─────────────────────────────────────────────────────────────┘
```

### Extension Pattern

```
┌─────────────────────────────────────┐
│          Camera2D                   │
│  - Transform (Position, Rotation)   │
│  - Zoom                             │
│  - Targets                          │
│  - Extensions (list)                │
└──────────┬──────────────────────────┘
           │
           ├──► ICameraExtension
           │    - PreUpdate()
           │    - Update()
           │    - PostUpdate()
           │
           ├──► FollowExtension
           │    (Smooth follow with damping)
           │
           ├──► BoundariesExtension
           │    (Constrain to level bounds)
           │
           ├──► ShakeExtension
           │    (Procedural camera shake)
           │
           ├──► ZoomExtension
           │    (Smooth zoom in/out)
           │
           ├──► ParallaxExtension
           │    (Multi-layer background scrolling)
           │
           └──► PixelPerfectExtension
                (Snap to pixel grid)
```

### Directory Structure

```
dotnet/
├── _lib/
│   └── nexus-camera2d/
│       ├── README.md
│       ├── LICENSE
│       ├── nexus-camera2d.sln
│       ├── src/
│       │   └── NexusCamera2D.Core/
│       │       ├── NexusCamera2D.Core.csproj
│       │       ├── Core/
│       │       │   ├── Camera2D.cs
│       │       │   ├── CameraTarget.cs
│       │       │   ├── CameraTransform.cs
│       │       │   └── CameraUpdateMode.cs
│       │       ├── Extensions/
│       │       │   ├── ICameraExtension.cs
│       │       │   ├── FollowExtension.cs
│       │       │   ├── BoundariesExtension.cs
│       │       │   ├── ShakeExtension.cs
│       │       │   ├── ZoomExtension.cs
│       │       │   ├── ParallaxExtension.cs
│       │       │   ├── PixelPerfectExtension.cs
│       │       │   └── DeadzoneExtension.cs
│       │       ├── Triggers/
│       │       │   ├── ICameraTrigger.cs
│       │       │   ├── BoundaryTrigger.cs
│       │       │   ├── ZoomTrigger.cs
│       │       │   └── RailTrigger.cs
│       │       ├── Math/
│       │       │   ├── Vector2.cs
│       │       │   ├── Rect.cs
│       │       │   └── MathHelper.cs
│       │       └── Damping/
│       │           ├── DampingType.cs
│       │           └── DampingHelper.cs
│       └── tests/
│           └── NexusCamera2D.Core.Tests/
│               ├── NexusCamera2D.Core.Tests.csproj
│               ├── Core/
│               ├── Extensions/
│               └── Math/
│
└── game-essential/
    └── core/
        ├── src/
        │   └── PigeonPea.Game.Camera/
        │       ├── PigeonPea.Game.Camera.csproj
        │       ├── Components/
        │       │   ├── CameraComponent.cs
        │       │   └── CameraTargetComponent.cs
        │       ├── Systems/
        │       │   ├── CameraUpdateSystem.cs
        │       │   └── ParallaxRenderingSystem.cs
        │       ├── Integration/
        │       │   ├── CameraWorldExtensions.cs
        │       │   └── DOTweenShakeAdapter.cs
        │       └── Presets/
        │           ├── DungeonCameraPreset.cs
        │           └── WorldMapCameraPreset.cs
        └── tests/
            └── PigeonPea.Game.Camera.Tests/
                ├── PigeonPea.Game.Camera.Tests.csproj
                └── Integration/
```

## Core Concepts (NexusCamera2D.Core)

### 1. Camera2D (Core Controller)

**Camera2D** is the main camera controller that manages targets, extensions, and transforms.

**Key Properties**:
- **Transform**: Position, rotation (for 2D usually 0)
- **Zoom**: Scale factor (1.0 = normal, 2.0 = 2x zoom in)
- **ViewportSize**: Rendering viewport dimensions
- **Targets**: Entities the camera follows
- **Extensions**: Modular behaviors

**Key Types**:
- `Camera2D`: Main camera controller
- `CameraTransform`: Position + rotation + zoom
- `CameraUpdateMode`: Update, LateUpdate, FixedUpdate, Manual

**Example**:
```csharp
var camera = new Camera2D
{
    ViewportWidth = 1920,
    ViewportHeight = 1080,
    Zoom = 1.0f
};

// Add extensions
camera.AddExtension(new FollowExtension { Smoothness = 0.1f });
camera.AddExtension(new BoundariesExtension { Bounds = new Rect(0, 0, 100, 100) });
camera.AddExtension(new ShakeExtension());

// Set target
camera.AddTarget(playerEntity, weight: 1.0f);

// Update each frame
camera.Update(deltaTime);
```

### 2. Camera Targets

**Camera Targets** define entities the camera should follow, with weighted influence.

**Key Properties**:
- **Position**: Target world position
- **Weight**: Influence on final camera position (0-1)
- **Offset**: Position offset from target
- **Enabled**: Whether this target is active

**Example**:
```csharp
// Follow player only
camera.AddTarget(playerPosition, weight: 1.0f);

// Follow player + enemy (50/50 split)
camera.AddTarget(playerPosition, weight: 0.5f);
camera.AddTarget(enemyPosition, weight: 0.5f);

// Follow player primarily, but keep enemy in view
camera.AddTarget(playerPosition, weight: 0.8f);
camera.AddTarget(enemyPosition, weight: 0.2f);
```

### 3. Camera Extensions

**Extensions** add modular behaviors to the camera.

**Key Interface**:
```csharp
public interface ICameraExtension
{
    string Name { get; }
    bool Enabled { get; set; }

    void Initialize(Camera2D camera);
    void PreUpdate(float deltaTime);
    void Update(float deltaTime);
    void PostUpdate(float deltaTime);
}
```

**Update Order**:
1. **PreUpdate**: Prepare state (e.g., read targets)
2. **Update**: Modify camera transform (e.g., apply follow)
3. **PostUpdate**: Finalize state (e.g., apply boundaries)

**Example**:
```csharp
public class FollowExtension : ICameraExtension
{
    public string Name => "Follow";
    public bool Enabled { get; set; } = true;
    public float Smoothness { get; set; } = 0.1f;

    private Camera2D? _camera;

    public void Initialize(Camera2D camera)
    {
        _camera = camera;
    }

    public void Update(float deltaTime)
    {
        if (_camera == null || !Enabled) return;

        // Calculate weighted average of targets
        var targetPos = CalculateTargetPosition();

        // Smooth follow using exponential damping
        var currentPos = _camera.Transform.Position;
        var newPos = Vector2.Lerp(currentPos, targetPos, 1f - Mathf.Exp(-Smoothness * deltaTime));

        _camera.Transform.Position = newPos;
    }

    // ... other methods
}
```

### 4. Follow Extension (Smooth Camera Movement)

**FollowExtension** smoothly follows targets using various damping algorithms.

**Key Features**:
- **Damping Types**: Linear (lerp), Exponential, Spring
- **Smoothness**: Control response time
- **Look-Ahead**: Anticipate player movement direction

**Example**:
```csharp
var followExt = new FollowExtension
{
    Smoothness = 0.15f, // 150ms response time
    DampingType = DampingType.Exponential,
    LookAhead = 2.0f // Look 2 tiles ahead in movement direction
};
camera.AddExtension(followExt);
```

### 5. Boundaries Extension (Level Constraints)

**BoundariesExtension** constrains camera to level bounds.

**Key Features**:
- **Bounds**: Rectangular area camera can't leave
- **Soft Boundaries**: Camera slows down near edges
- **Animated Transitions**: Smooth transition when bounds change

**Example**:
```csharp
var boundariesExt = new BoundariesExtension
{
    Bounds = new Rect(x: 0, y: 0, width: dungeonWidth, height: dungeonHeight),
    SoftEdge = 5.0f // Start slowing 5 tiles from edge
};
camera.AddExtension(boundariesExt);
```

### 6. Shake Extension (Camera Shake Effects)

**ShakeExtension** adds procedural camera shake for impact feedback.

**Key Features**:
- **Intensity**: Shake magnitude
- **Duration**: How long shake lasts
- **Frequency**: Oscillation speed
- **Damping**: Exponential decay

**Example**:
```csharp
var shakeExt = new ShakeExtension();
camera.AddExtension(shakeExt);

// Trigger shake on player attack
shakeExt.Shake(intensity: 5.0f, duration: 0.3f, frequency: 20f);

// Different shake for different events
shakeExt.Shake(intensity: 10.0f, duration: 0.5f); // Big explosion
shakeExt.Shake(intensity: 2.0f, duration: 0.2f);  // Small hit
```

**Note**: Can also integrate with **DOTween** for more complex shake patterns (see Integration section).

### 7. Zoom Extension (Camera Zoom)

**ZoomExtension** provides smooth zoom in/out.

**Key Features**:
- **Target Zoom**: Desired zoom level
- **Zoom Speed**: Transition speed
- **Min/Max Zoom**: Constraints

**Example**:
```csharp
var zoomExt = new ZoomExtension
{
    MinZoom = 0.5f,
    MaxZoom = 3.0f,
    ZoomSpeed = 2.0f
};
camera.AddExtension(zoomExt);

// Zoom in
zoomExt.SetTargetZoom(2.0f);

// Zoom out
zoomExt.SetTargetZoom(0.8f);
```

### 8. Parallax Extension (Multi-Layer Backgrounds)

**ParallaxExtension** manages multi-layer parallax scrolling.

**Key Features**:
- **Layers**: Multiple background layers
- **Speed Multipliers**: Each layer moves at different speed (0 = static, 1 = same as camera)
- **Infinite Scrolling**: Loop layers for endless backgrounds

**Example**:
```csharp
var parallaxExt = new ParallaxExtension();
parallaxExt.AddLayer("sky", speedMultiplier: 0.1f);      // Slow (far)
parallaxExt.AddLayer("clouds", speedMultiplier: 0.3f);   // Medium
parallaxExt.AddLayer("mountains", speedMultiplier: 0.5f);// Fast
parallaxExt.AddLayer("trees", speedMultiplier: 0.8f);    // Very fast (near)

camera.AddExtension(parallaxExt);

// In rendering loop
foreach (var layer in parallaxExt.Layers)
{
    var layerOffset = camera.Transform.Position * layer.SpeedMultiplier;
    RenderLayer(layer.Name, layerOffset);
}
```

### 9. Pixel Perfect Extension (Grid Snapping)

**PixelPerfectExtension** snaps camera to pixel grid for crisp 2D rendering.

**Key Features**:
- **Pixels Per Unit**: How many pixels = 1 world unit
- **Snap To Grid**: Round position to nearest pixel

**Example**:
```csharp
var pixelPerfectExt = new PixelPerfectExtension
{
    PixelsPerUnit = 16 // 16x16 tiles
};
camera.AddExtension(pixelPerfectExt);

// Camera position will be snapped: (10.3, 5.7) → (10.0, 6.0)
```

### 10. Deadzone Extension (Follow Deadzone)

**DeadzoneExtension** creates an area where player can move without camera following.

**Key Features**:
- **Deadzone Size**: Size of center "dead" area
- **Soft Edge**: Gradual follow near deadzone edge

**Example**:
```csharp
var deadzoneExt = new DeadzoneExtension
{
    DeadzoneWidth = 10f, // Player can move 10 units before camera follows
    DeadzoneHeight = 8f,
    SoftEdge = 2f
};
camera.AddExtension(deadzoneExt);
```

## Core Library Implementation (Phase 1)

### Step 1.1: Create Project Structure

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib
mkdir nexus-camera2d
cd nexus-camera2d
mkdir src tests
cd src
mkdir NexusCamera2D.Core
cd NexusCamera2D.Core
mkdir Core Extensions Triggers Math Damping
```

### Step 1.2: Create NexusCamera2D.Core.csproj

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/NexusCamera2D.Core.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>NexusCamera2D</RootNamespace>

    <!-- NuGet Package Metadata -->
    <PackageId>NexusCamera2D.Core</PackageId>
    <Version>0.1.0</Version>
    <Authors>Pigeon Pea Development Team</Authors>
    <Description>Engine-agnostic 2D camera system inspired by ProCamera2D</Description>
    <PackageTags>gamedev;camera;2d;rendering</PackageTags>
    <RepositoryUrl>https://github.com/your-repo/nexus-camera2d</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>

  <ItemGroup>
    <!-- No external dependencies - pure C# -->
  </ItemGroup>

</Project>
```

### Step 1.3: Implement Math Primitives

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Math/Vector2.cs`

```csharp
namespace NexusCamera2D.Math;

/// <summary>
/// Simple 2D vector (to avoid external dependencies).
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

    public float Magnitude => MathF.Sqrt(X * X + Y * Y);
    public Vector2 Normalized
    {
        get
        {
            var mag = Magnitude;
            return mag > 0 ? new Vector2(X / mag, Y / mag) : Zero;
        }
    }

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, float scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vector2 operator /(Vector2 a, float scalar) => new(a.X / scalar, a.Y / scalar);

    public static float Distance(Vector2 a, Vector2 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
    {
        t = MathHelper.Clamp01(t);
        return new Vector2(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t
        );
    }

    public override string ToString() => $"({X:F2}, {Y:F2})";
}
```

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Math/Rect.cs`

```csharp
namespace NexusCamera2D.Math;

/// <summary>
/// Rectangle (for boundaries, triggers).
/// </summary>
public struct Rect
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float Left => X;
    public float Right => X + Width;
    public float Top => Y;
    public float Bottom => Y + Height;

    public Vector2 Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(Vector2 point)
    {
        return point.X >= Left && point.X <= Right &&
               point.Y >= Top && point.Y <= Bottom;
    }

    public Vector2 Clamp(Vector2 point)
    {
        return new Vector2(
            MathHelper.Clamp(point.X, Left, Right),
            MathHelper.Clamp(point.Y, Top, Bottom)
        );
    }

    public override string ToString() => $"Rect({X}, {Y}, {Width}, {Height})";
}
```

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Math/MathHelper.cs`

```csharp
namespace NexusCamera2D.Math;

/// <summary>
/// Math utility functions.
/// </summary>
public static class MathHelper
{
    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static float Clamp01(float value) => Clamp(value, 0f, 1f);

    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Clamp01(t);
    }

    public static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;

        return current + MathF.Sign(target - current) * maxDelta;
    }

    public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float deltaTime, float maxSpeed = float.PositiveInfinity)
    {
        smoothTime = MathF.Max(0.0001f, smoothTime);
        float omega = 2f / smoothTime;
        float x = omega * deltaTime;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
        float change = current - target;
        float originalTo = target;
        float maxChange = maxSpeed * smoothTime;
        change = Clamp(change, -maxChange, maxChange);
        target = current - change;
        float temp = (velocity + omega * change) * deltaTime;
        velocity = (velocity - omega * temp) * exp;
        float output = target + (change + temp) * exp;

        if (originalTo - current > 0.0f == output > originalTo)
        {
            output = originalTo;
            velocity = (output - originalTo) / deltaTime;
        }

        return output;
    }
}
```

### Step 1.4: Implement Camera Core

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Core/CameraTransform.cs`

```csharp
using NexusCamera2D.Math;

namespace NexusCamera2D.Core;

/// <summary>
/// Camera transform (position, rotation, zoom).
/// </summary>
public sealed class CameraTransform
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Rotation { get; set; } = 0f; // Degrees (usually 0 for 2D)
    public float Zoom { get; set; } = 1.0f;

    public override string ToString() => $"Pos: {Position}, Rot: {Rotation:F1}°, Zoom: {Zoom:F2}x";
}
```

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Core/CameraTarget.cs`

```csharp
using NexusCamera2D.Math;

namespace NexusCamera2D.Core;

/// <summary>
/// Target the camera should follow.
/// </summary>
public sealed class CameraTarget
{
    public Vector2 Position { get; set; }
    public float Weight { get; set; } = 1.0f; // 0-1 influence
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public bool Enabled { get; set; } = true;

    public Vector2 EffectivePosition => Position + Offset;

    public override string ToString() => $"Target @ {Position} (Weight: {Weight:F2})";
}
```

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Core/CameraUpdateMode.cs`

```csharp
namespace NexusCamera2D.Core;

/// <summary>
/// When camera updates.
/// </summary>
public enum CameraUpdateMode
{
    Update,      // Normal update
    LateUpdate,  // After game logic
    FixedUpdate, // Fixed timestep
    Manual       // User calls Update()
}
```

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Core/Camera2D.cs`

```csharp
using NexusCamera2D.Extensions;
using NexusCamera2D.Math;

namespace NexusCamera2D.Core;

/// <summary>
/// Main 2D camera controller.
/// </summary>
public sealed class Camera2D
{
    public CameraTransform Transform { get; } = new();
    public float ViewportWidth { get; set; }
    public float ViewportHeight { get; set; }
    public CameraUpdateMode UpdateMode { get; set; } = CameraUpdateMode.Update;

    private readonly List<CameraTarget> _targets = new();
    private readonly List<ICameraExtension> _extensions = new();

    /// <summary>
    /// Adds a target for the camera to follow.
    /// </summary>
    public void AddTarget(Vector2 position, float weight = 1.0f, Vector2? offset = null)
    {
        _targets.Add(new CameraTarget
        {
            Position = position,
            Weight = weight,
            Offset = offset ?? Vector2.Zero
        });
    }

    /// <summary>
    /// Clears all targets.
    /// </summary>
    public void ClearTargets()
    {
        _targets.Clear();
    }

    /// <summary>
    /// Gets all active targets.
    /// </summary>
    public IReadOnlyList<CameraTarget> Targets => _targets.Where(t => t.Enabled).ToList();

    /// <summary>
    /// Adds an extension to the camera.
    /// </summary>
    public void AddExtension(ICameraExtension extension)
    {
        _extensions.Add(extension);
        extension.Initialize(this);
    }

    /// <summary>
    /// Gets an extension by type.
    /// </summary>
    public T? GetExtension<T>() where T : ICameraExtension
    {
        return _extensions.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Removes an extension.
    /// </summary>
    public void RemoveExtension(ICameraExtension extension)
    {
        _extensions.Remove(extension);
    }

    /// <summary>
    /// Updates the camera (call each frame).
    /// </summary>
    public void Update(float deltaTime)
    {
        // PreUpdate
        foreach (var ext in _extensions.Where(e => e.Enabled))
        {
            ext.PreUpdate(deltaTime);
        }

        // Update
        foreach (var ext in _extensions.Where(e => e.Enabled))
        {
            ext.Update(deltaTime);
        }

        // PostUpdate
        foreach (var ext in _extensions.Where(e => e.Enabled))
        {
            ext.PostUpdate(deltaTime);
        }
    }

    /// <summary>
    /// Calculates the weighted average position of all targets.
    /// </summary>
    public Vector2 CalculateTargetPosition()
    {
        var activeTargets = Targets.ToList();
        if (activeTargets.Count == 0)
            return Transform.Position;

        float totalWeight = activeTargets.Sum(t => t.Weight);
        if (totalWeight == 0)
            return Transform.Position;

        Vector2 weightedSum = Vector2.Zero;
        foreach (var target in activeTargets)
        {
            weightedSum = weightedSum + target.EffectivePosition * target.Weight;
        }

        return weightedSum / totalWeight;
    }

    public override string ToString() => $"Camera2D: {Transform}";
}
```

### Step 1.5: Implement Extension Interface

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Extensions/ICameraExtension.cs`

```csharp
namespace NexusCamera2D.Extensions;

/// <summary>
/// Interface for modular camera extensions.
/// </summary>
public interface ICameraExtension
{
    string Name { get; }
    bool Enabled { get; set; }

    /// <summary>
    /// Called when extension is added to camera.
    /// </summary>
    void Initialize(NexusCamera2D.Core.Camera2D camera);

    /// <summary>
    /// Called before Update (prepare state).
    /// </summary>
    void PreUpdate(float deltaTime);

    /// <summary>
    /// Called during Update (modify camera transform).
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    /// Called after Update (finalize state).
    /// </summary>
    void PostUpdate(float deltaTime);
}
```

### Step 1.6: Implement Damping Helper

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Damping/DampingType.cs`

```csharp
namespace NexusCamera2D.Damping;

/// <summary>
/// Type of camera damping/smoothing.
/// </summary>
public enum DampingType
{
    /// <summary>Linear interpolation (lerp)</summary>
    Linear,

    /// <summary>Exponential decay (smooth, frame-rate independent)</summary>
    Exponential,

    /// <summary>Spring physics (bouncy)</summary>
    Spring
}
```

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Damping/DampingHelper.cs`

```csharp
using NexusCamera2D.Math;

namespace NexusCamera2D.Damping;

/// <summary>
/// Helper for camera damping/smoothing algorithms.
/// </summary>
public static class DampingHelper
{
    /// <summary>
    /// Linear lerp damping.
    /// </summary>
    public static Vector2 LinearDamp(Vector2 current, Vector2 target, float smoothness, float deltaTime)
    {
        float t = MathHelper.Clamp01(smoothness * deltaTime);
        return Vector2.Lerp(current, target, t);
    }

    /// <summary>
    /// Exponential decay damping (frame-rate independent).
    /// </summary>
    public static Vector2 ExponentialDamp(Vector2 current, Vector2 target, float smoothness, float deltaTime)
    {
        float decay = 1f - MathF.Exp(-smoothness * deltaTime);
        return Vector2.Lerp(current, target, decay);
    }

    /// <summary>
    /// Spring damping (smooth with slight overshoot).
    /// </summary>
    public static Vector2 SpringDamp(Vector2 current, Vector2 target, ref Vector2 velocity, float smoothness, float deltaTime)
    {
        float smoothTime = 1f / smoothness;
        float vx = velocity.X;
        float vy = velocity.Y;

        float newX = MathHelper.SmoothDamp(current.X, target.X, ref vx, smoothTime, deltaTime);
        float newY = MathHelper.SmoothDamp(current.Y, target.Y, ref vy, smoothTime, deltaTime);

        velocity = new Vector2(vx, vy);
        return new Vector2(newX, newY);
    }
}
```

### Step 1.7: Implement Follow Extension

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Extensions/FollowExtension.cs`

```csharp
using NexusCamera2D.Core;
using NexusCamera2D.Damping;
using NexusCamera2D.Math;

namespace NexusCamera2D.Extensions;

/// <summary>
/// Smooth follow extension (damped movement towards targets).
/// </summary>
public sealed class FollowExtension : ICameraExtension
{
    public string Name => "Follow";
    public bool Enabled { get; set; } = true;

    public float Smoothness { get; set; } = 5.0f; // Higher = faster response
    public DampingType DampingType { get; set; } = DampingType.Exponential;
    public float LookAhead { get; set; } = 0f; // Look ahead in movement direction

    private Camera2D? _camera;
    private Vector2 _velocity = Vector2.Zero; // For spring damping
    private Vector2 _previousTargetPos = Vector2.Zero;

    public void Initialize(Camera2D camera)
    {
        _camera = camera;
        _previousTargetPos = camera.CalculateTargetPosition();
    }

    public void PreUpdate(float deltaTime)
    {
        // Nothing needed
    }

    public void Update(float deltaTime)
    {
        if (_camera == null || !Enabled) return;

        var targetPos = _camera.CalculateTargetPosition();

        // Calculate look-ahead offset
        if (LookAhead > 0)
        {
            var direction = (targetPos - _previousTargetPos).Normalized;
            targetPos = targetPos + direction * LookAhead;
        }

        var currentPos = _camera.Transform.Position;
        Vector2 newPos;

        switch (DampingType)
        {
            case DampingType.Linear:
                newPos = DampingHelper.LinearDamp(currentPos, targetPos, Smoothness, deltaTime);
                break;
            case DampingType.Exponential:
                newPos = DampingHelper.ExponentialDamp(currentPos, targetPos, Smoothness, deltaTime);
                break;
            case DampingType.Spring:
                newPos = DampingHelper.SpringDamp(currentPos, targetPos, ref _velocity, Smoothness, deltaTime);
                break;
            default:
                newPos = targetPos;
                break;
        }

        _camera.Transform.Position = newPos;
        _previousTargetPos = targetPos;
    }

    public void PostUpdate(float deltaTime)
    {
        // Nothing needed
    }
}
```

### Step 1.8: Implement Boundaries Extension

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Extensions/BoundariesExtension.cs`

```csharp
using NexusCamera2D.Core;
using NexusCamera2D.Math;

namespace NexusCamera2D.Extensions;

/// <summary>
/// Constrains camera to level boundaries.
/// </summary>
public sealed class BoundariesExtension : ICameraExtension
{
    public string Name => "Boundaries";
    public bool Enabled { get; set; } = true;

    public Rect Bounds { get; set; }
    public float SoftEdge { get; set; } = 0f; // Distance from edge to start slowing

    private Camera2D? _camera;

    public void Initialize(Camera2D camera)
    {
        _camera = camera;
    }

    public void PreUpdate(float deltaTime)
    {
        // Nothing needed
    }

    public void Update(float deltaTime)
    {
        // Nothing needed (we apply in PostUpdate to override other extensions)
    }

    public void PostUpdate(float deltaTime)
    {
        if (_camera == null || !Enabled) return;

        // Calculate camera viewport bounds
        float halfWidth = _camera.ViewportWidth / (2 * _camera.Transform.Zoom);
        float halfHeight = _camera.ViewportHeight / (2 * _camera.Transform.Zoom);

        // Constrain camera position
        var pos = _camera.Transform.Position;

        float minX = Bounds.Left + halfWidth;
        float maxX = Bounds.Right - halfWidth;
        float minY = Bounds.Top + halfHeight;
        float maxY = Bounds.Bottom - halfHeight;

        // Clamp
        pos.X = MathHelper.Clamp(pos.X, minX, maxX);
        pos.Y = MathHelper.Clamp(pos.Y, minY, maxY);

        _camera.Transform.Position = pos;
    }
}
```

### Step 1.9: Implement Shake Extension

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Extensions/ShakeExtension.cs`

```csharp
using NexusCamera2D.Core;
using NexusCamera2D.Math;

namespace NexusCamera2D.Extensions;

/// <summary>
/// Camera shake extension.
/// </summary>
public sealed class ShakeExtension : ICameraExtension
{
    public string Name => "Shake";
    public bool Enabled { get; set; } = true;

    private Camera2D? _camera;
    private float _shakeIntensity = 0f;
    private float _shakeDuration = 0f;
    private float _shakeTimer = 0f;
    private float _shakeFrequency = 20f;
    private Vector2 _shakeOffset = Vector2.Zero;
    private Random _random = new();

    public void Initialize(Camera2D camera)
    {
        _camera = camera;
    }

    /// <summary>
    /// Triggers a camera shake.
    /// </summary>
    public void Shake(float intensity, float duration, float frequency = 20f)
    {
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _shakeFrequency = frequency;
        _shakeTimer = 0f;
    }

    public void PreUpdate(float deltaTime)
    {
        // Nothing needed
    }

    public void Update(float deltaTime)
    {
        if (_camera == null || !Enabled) return;

        if (_shakeTimer < _shakeDuration)
        {
            _shakeTimer += deltaTime;

            // Calculate decay (exponential)
            float decay = 1f - (_shakeTimer / _shakeDuration);
            float currentIntensity = _shakeIntensity * decay;

            // Generate shake offset (random direction)
            float angle = (float)_random.NextDouble() * MathF.PI * 2f;
            _shakeOffset = new Vector2(
                MathF.Cos(angle) * currentIntensity,
                MathF.Sin(angle) * currentIntensity
            );

            // Apply shake
            _camera.Transform.Position = _camera.Transform.Position + _shakeOffset;
        }
        else
        {
            _shakeOffset = Vector2.Zero;
        }
    }

    public void PostUpdate(float deltaTime)
    {
        if (_camera == null || !Enabled) return;

        // Remove shake offset after all other extensions
        _camera.Transform.Position = _camera.Transform.Position - _shakeOffset;
    }
}
```

### Step 1.10: Implement Zoom Extension

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Extensions/ZoomExtension.cs`

```csharp
using NexusCamera2D.Core;
using NexusCamera2D.Math;

namespace NexusCamera2D.Extensions;

/// <summary>
/// Smooth zoom extension.
/// </summary>
public sealed class ZoomExtension : ICameraExtension
{
    public string Name => "Zoom";
    public bool Enabled { get; set; } = true;

    public float MinZoom { get; set; } = 0.5f;
    public float MaxZoom { get; set; } = 3.0f;
    public float ZoomSpeed { get; set; } = 5.0f;

    private Camera2D? _camera;
    private float _targetZoom = 1.0f;

    public void Initialize(Camera2D camera)
    {
        _camera = camera;
        _targetZoom = camera.Transform.Zoom;
    }

    /// <summary>
    /// Sets target zoom level.
    /// </summary>
    public void SetTargetZoom(float zoom)
    {
        _targetZoom = MathHelper.Clamp(zoom, MinZoom, MaxZoom);
    }

    /// <summary>
    /// Zooms in by a delta.
    /// </summary>
    public void ZoomBy(float delta)
    {
        SetTargetZoom(_targetZoom + delta);
    }

    public void PreUpdate(float deltaTime)
    {
        // Nothing needed
    }

    public void Update(float deltaTime)
    {
        if (_camera == null || !Enabled) return;

        // Smooth zoom
        float currentZoom = _camera.Transform.Zoom;
        float newZoom = MathHelper.Lerp(currentZoom, _targetZoom, ZoomSpeed * deltaTime);

        _camera.Transform.Zoom = newZoom;
    }

    public void PostUpdate(float deltaTime)
    {
        // Nothing needed
    }
}
```

### Step 1.11: Implement Pixel Perfect Extension

**File**: `_lib/nexus-camera2d/src/NexusCamera2D.Core/Extensions/PixelPerfectExtension.cs`

```csharp
using NexusCamera2D.Core;
using NexusCamera2D.Math;

namespace NexusCamera2D.Extensions;

/// <summary>
/// Pixel-perfect rendering extension (snaps to pixel grid).
/// </summary>
public sealed class PixelPerfectExtension : ICameraExtension
{
    public string Name => "PixelPerfect";
    public bool Enabled { get; set; } = true;

    public int PixelsPerUnit { get; set; } = 16; // 16x16 tiles

    private Camera2D? _camera;

    public void Initialize(Camera2D camera)
    {
        _camera = camera;
    }

    public void PreUpdate(float deltaTime)
    {
        // Nothing needed
    }

    public void Update(float deltaTime)
    {
        // Nothing needed
    }

    public void PostUpdate(float deltaTime)
    {
        if (_camera == null || !Enabled) return;

        // Snap position to pixel grid
        var pos = _camera.Transform.Position;
        float pixelSize = 1f / PixelsPerUnit;

        pos.X = MathF.Round(pos.X / pixelSize) * pixelSize;
        pos.Y = MathF.Round(pos.Y / pixelSize) * pixelSize;

        _camera.Transform.Position = pos;
    }
}
```

## Phase 1 Completion Checklist

- [ ] Project structure created
- [ ] Math primitives implemented (Vector2, Rect, MathHelper)
- [ ] Camera core implemented (Camera2D, CameraTransform, CameraTarget)
- [ ] Extension interface implemented
- [ ] Damping helpers implemented
- [ ] FollowExtension implemented
- [ ] BoundariesExtension implemented
- [ ] ShakeExtension implemented
- [ ] ZoomExtension implemented
- [ ] PixelPerfectExtension implemented
- [ ] Solution builds without errors

**Verification Command**:
```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-camera2d
dotnet build
```

Expected output: `Build succeeded. 0 Warning(s). 0 Error(s).`

---

## Phase 2: DOTween Integration

Since you're using **DOTween** directly, here's how to integrate it with Nexus-Camera2D for advanced shake effects:

### DOTween Shake Adapter

**File**: `game-essential/core/src/PigeonPea.Game.Camera/Integration/DOTweenShakeAdapter.cs`

```csharp
using DG.Tweening;
using NexusCamera2D.Core;
using NexusCamera2D.Math;

namespace PigeonPea.Game.Camera.Integration;

/// <summary>
/// Adapts DOTween for advanced camera shake effects.
/// </summary>
public static class DOTweenShakeAdapter
{
    /// <summary>
    /// Shakes camera using DOTween (more complex shake patterns).
    /// </summary>
    public static Tweener ShakePosition(Camera2D camera, float duration, float strength, int vibrato = 10, float randomness = 90f)
    {
        var transform = camera.Transform;
        var originalPos = transform.Position;

        // Use DOTween's DOShake for advanced shake
        // We shake a dummy Vector2 and apply to camera
        var dummyVec = new Vector2(originalPos.X, originalPos.Y);

        return DOTween.Shake(
            () => new UnityEngine.Vector2(dummyVec.X, dummyVec.Y), // Getter (convert to Unity Vector2)
            v => // Setter
            {
                dummyVec = new Vector2(v.x, v.y);
                transform.Position = dummyVec;
            },
            duration,
            strength,
            vibrato,
            randomness
        );
    }

    /// <summary>
    /// Shakes camera rotation using DOTween.
    /// </summary>
    public static Tweener ShakeRotation(Camera2D camera, float duration, float strength, int vibrato = 10)
    {
        var transform = camera.Transform;

        return DOTween.To(
            () => transform.Rotation,
            r => transform.Rotation = r,
            transform.Rotation + strength,
            duration
        ).SetEase(Ease.OutElastic);
    }

    /// <summary>
    /// Punch zoom (quick zoom in/out).
    /// </summary>
    public static Tweener PunchZoom(Camera2D camera, float strength, float duration, int vibrato = 10)
    {
        var transform = camera.Transform;

        return DOTween.Punch(
            () => transform.Zoom,
            z => transform.Zoom = z,
            strength,
            duration,
            vibrato
        );
    }
}
```

### Usage Example

```csharp
// On player attack
DOTweenShakeAdapter.ShakePosition(camera, duration: 0.3f, strength: 5f, vibrato: 20);

// On explosion
DOTweenShakeAdapter.ShakePosition(camera, duration: 0.5f, strength: 15f, vibrato: 30);
DOTweenShakeAdapter.PunchZoom(camera, strength: 0.2f, duration: 0.5f);

// On critical hit
DOTweenShakeAdapter.ShakeRotation(camera, duration: 0.2f, strength: 5f);
```

---

## Remaining Phases Summary

**Phase 3: ECS Integration** (Week 1-2)
- CameraComponent (ECS component)
- CameraSystem (updates camera each frame)
- CameraTargetComponent (for entities camera follows)

**Phase 4: Parallax System** (Week 2)
- ParallaxExtension implementation
- ParallaxLayer rendering
- Integration with dungeon/map rendering

**Phase 5: Triggers** (Week 3)
- ICameraTrigger interface
- BoundaryTrigger (change behavior when entering area)
- ZoomTrigger (zoom when entering area)

**Phase 6: Testing** (Week 3)
- Unit tests for all extensions
- Integration tests with ECS
- Performance profiling

## Success Criteria

- [ ] NexusCamera2D.Core builds with zero external dependencies
- [ ] All unit tests passing (≥80% coverage)
- [ ] PigeonPea.Game.Camera integrates with Arch ECS
- [ ] Camera smoothly follows player
- [ ] Shake effects work correctly
- [ ] Boundaries constrain camera to level
- [ ] Zoom in/out works smoothly
- [ ] Pixel-perfect rendering enabled
- [ ] DOTween integration for advanced effects
- [ ] Parallax scrolling works for multi-layer backgrounds

## References

- **ProCamera2D**: https://www.procamera2d.com/
- **Unity Cinemachine**: https://unity.com/unity/features/editor/art-and-design/cinemachine
- **Camera2D Patterns**:
  - Celeste camera: https://maddythorson.medium.com/celeste-and-towerfall-physics-d24bd2ae0fc5
  - Platformer camera: https://www.gamedeveloper.com/design/scroll-back-the-theory-and-practice-of-cameras-in-side-scrollers
- **DOTween**: http://dotween.demigiant.com/documentation.php
- **Existing Patterns**:
  - Nexus-GAS: RFC-019
  - Nexus-GOAP: RFC-020
  - Nexus-Input: RFC-023

## Appendix: Quick Start Commands

```bash
# Build NexusCamera2D.Core
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-camera2d
dotnet build

# Run NexusCamera2D.Core tests
dotnet test

# Build PigeonPea.Game.Camera
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\game-essential\core\src\PigeonPea.Game.Camera
dotnet build

# Add to solution
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet
dotnet sln PigeonPea.sln add _lib\nexus-camera2d\src\NexusCamera2D.Core\NexusCamera2D.Core.csproj
dotnet sln PigeonPea.sln add game-essential\core\src\PigeonPea.Game.Camera\PigeonPea.Game.Camera.csproj
```

---

**End of RFC-024: Nexus-Camera2D Implementation Guide**

*This document provides complete implementation instructions for Phase 1-2. The system provides ProCamera2D-like modular camera extensions with smooth movement, shake, zoom, boundaries, and pixel-perfect rendering. DOTween is integrated for advanced shake patterns and cinematic effects.*
