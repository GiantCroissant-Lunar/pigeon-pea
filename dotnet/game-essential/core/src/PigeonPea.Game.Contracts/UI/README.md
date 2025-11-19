# Game UI Contracts

This namespace contains contracts and types for game user interface and HUD systems in the PigeonPea game engine.

## Overview

The UI contracts provide a standardized way for plugins to implement user interfaces, HUDs, and menu systems. This complements the rendering contracts by focusing on the user interaction layer rather than the visual game world rendering.

## Key Components

### IUserInterface
The main interface for UI/HUD implementations. Provides methods for:
- Initialization with UI context
- Game state updates
- Panel management (show/hide)
- Notification display
- Root control access for embedding

### UIContext
Configuration context for UI initialization including:
- Service provider integration
- Display dimensions
- Theme configuration
- Performance settings
- Custom data support

### UICapabilities
Flags enum defining supported UI features:
- Basic HUD display
- Menu systems
- Dialog systems
- Tooltips
- Notifications
- Inventory display
- Character status
- Minimap
- Custom controls
- Animations
- Theming
- Localization
- Accessibility
- Input binding

### UITheme
Theme configuration class with:
- Color schemes (primary, secondary, background, etc.)
- Typography settings
- Layout properties
- Built-in light/dark theme factories

### NotificationType
Enumeration of notification types:
- Info, Success, Warning, Error
- System, Achievement, Quest
- Combat, Item, Character
- Chat, Network

## Usage Example

```csharp
// Create UI context
var uiContext = new UIContext
{
    Width = 1920,
    Height = 1080,
    Theme = UITheme.CreateDarkTheme(),
    TargetFPS = 60,
    EnableAnimations = true
};

// Initialize UI implementation
var ui = registry.Get<IUserInterface>();
ui.Initialize(uiContext);

// Update UI with game state
ui.Update(gameState);

// Show notification
ui.ShowNotification("Quest completed!", NotificationType.Success);

// Get root control for embedding
var rootControl = ui.GetRootControl();
```

## Integration with Rendering

UI systems are designed to work alongside rendering systems:

1. **Rendering Layer**: Handles game world rendering (maps, sprites, entities)
2. **UI Layer**: Handles user interface elements (HUD, menus, dialogs)

The UI layer typically overlays the rendering layer, providing interactive elements while the rendering system handles the game world visualization.

## Plugin Implementation

UI plugins should:
1. Implement `IUserInterface` for game-specific functionality
2. Implement `PigeonPea.Contracts.Hud.Services.IService` for app-level compatibility
3. Register both interfaces in the plugin system
4. Return appropriate root controls for the host framework

This dual-implementation approach allows:
- Game code to use rich `IUserInterface` features
- App infrastructure to use basic `IService` compatibility
- Single plugin to serve both layers of the application

## Dependencies

- `PigeonPea.Game.Contracts.Models` - For GameState integration
- `PigeonPea.Contracts.Hud.Services` - For app-level compatibility
- System dependency injection support
