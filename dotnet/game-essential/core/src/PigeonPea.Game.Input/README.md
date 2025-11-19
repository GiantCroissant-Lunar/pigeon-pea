# PigeonPea.Game.Input

Platform-specific input integration for the PigeonPea game engine.

## Overview

This package provides input system integration between the NexusInput core library and the PigeonPea game engine. It implements the console keyboard device adapter and provides seamless integration with the GameWorld ECS system.

## Components

### Devices

#### ConsoleKeyboardDevice
- Implements `IInputDevice` for console keyboard input
- Maps console keys to input control paths
- Supports WASD, arrow keys, space, escape, etc.
- Non-blocking key polling with proper state management

### Events

#### MoveInputEvent
- Triggered when player requests movement
- Contains direction vector and timestamp
- Published via MessagePipe for decoupled handling

#### AttackInputEvent
- Triggered when player requests attack action
- Contains timestamp for action timing
- Used for combat system integration

#### InteractInputEvent
- Triggered when player requests interaction
- Contains timestamp for action timing
- Used for item pickup and object interaction

### Integration

#### GameWorldInputIntegration
- Main integration class connecting NexusInput to GameWorld
- Registers devices and loads input action assets
- Sets up callbacks for input actions
- Manages input map switching (Gameplay ↔ UI)

#### InputWorldExtensions
- Extension methods for GameWorld
- Provides fluent API for adding input system
- Simplifies integration setup

## Usage

### Basic Setup

```csharp
// Create game world
var gameWorld = new GameWorld(80, 50);

// Add input system integration
var inputIntegration = gameWorld.AddInputSystem();

// Game loop
while (running)
{
    inputIntegration.Update(deltaTime);
    gameWorld.Update(deltaTime);
    // Render...
}
```

### With MessagePipe Events

```csharp
// Setup MessagePipe
var services = new ServiceCollection()
    .AddMessagePipe()
    .BuildServiceProvider();

var movePublisher = services.GetRequiredService<IPublisher<MoveInputEvent>>();
var attackPublisher = services.GetRequiredService<IPublisher<AttackInputEvent>>();
var interactPublisher = services.GetRequiredService<IPublisher<InteractInputEvent>>();

// Create integration with event publishers
var inputIntegration = gameWorld.AddInputSystem(
    movePublisher, attackPublisher, interactPublisher);

// Subscribe to events
services.GetRequiredService<ISubscriber<MoveInputEvent>>()
    .Subscribe(evt => Console.WriteLine($"Moved: {evt.Direction}"));
```

### Input Map Switching

```csharp
// Switch to UI input map
inputIntegration.EnableUIInput();

// Switch back to gameplay
inputIntegration.EnableGameplayInput();
```

## Input Actions

The system uses JSON-based input action definitions:

- **Move**: Vector2 input for player movement
- **Attack**: Button input for combat
- **Interact**: Button input for item pickup/interaction
- **Inventory**: Button input for inventory management
- **Pause**: Button input for game pause/menu

### Default Bindings

| Action | Primary | Secondary |
|---------|----------|-----------|
| Move | WASD | Arrow Keys |
| Attack | Space | - |
| Interact | E | - |
| Inventory | I | - |
| Pause | Escape | - |

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ Console Input   │───▶│ ConsoleKeyboard  │───▶│ NexusInput.Core │
│ (System.Console)│    │ Device          │    │ InputSystem     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                         │
                                                         ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ GameWorld       │◀───│ GameWorldInput  │◀───│ Input Events    │
│ (ECS System)   │    │ Integration     │    │ (MessagePipe)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## Testing

### Unit Tests
- ConsoleKeyboardDeviceTests: Device functionality and key mapping
- Integration tests for event publishing
- Mock-based testing for input scenarios

### Demo Application
- `InputDemoConsoleApp`: Full integration demonstration
- Shows real-time input handling with GameWorld
- Event logging and status display

## Dependencies

- **NexusInput.Core**: Core input system library
- **PigeonPea.Shared**: Core game engine components
- **MessagePipe**: Event messaging system
- **TheSadRogue.Primitives**: Geometry and math types

## Configuration

Input actions are defined in `Assets/DefaultPlayerControls.inputactions`:

```json
{
  "name": "DefaultPlayerControls",
  "maps": [
    {
      "name": "Gameplay",
      "actions": [...],
      "bindings": [...]
    }
  ]
}
```

The JSON file is embedded as a resource and loaded automatically.

## Future Enhancements

- **Mouse Support**: Add console mouse input device
- **Gamepad Support**: Add gamepad device adapter
- **Rebinding**: Runtime input binding configuration
- **Input Recording**: Playback system for testing/demo
- **Accessibility**: Alternative input schemes and assistive features

## Thread Safety

- Input device updates are thread-safe
- Event publishing uses MessagePipe's built-in synchronization
- GameWorld integration maintains proper locking

## Performance

- Minimal allocation during input processing
- Efficient key state tracking
- Cached input action lookups
- Optimized for 60+ FPS gameplay

## Troubleshooting

### Common Issues

1. **Input not responding**: Check that input integration is updated in game loop
2. **Wrong key mappings**: Verify input action JSON configuration
3. **Missing events**: Ensure MessagePipe services are properly configured
4. **Build errors**: Verify all project references and package versions

### Debug Information

Enable console logging to see input events:
```csharp
services.GetRequiredService<ISubscriber<MoveInputEvent>>()
    .Subscribe(evt => Console.WriteLine($"[DEBUG] Move: {evt.Direction}"));
