# Avalonia HUD for Windows

A modern, Avalonia-based HUD and UI system for Windows applications in the PigeonPea game engine.

## Overview

This plugin provides a comprehensive Heads-Up Display (HUD) and user interface system built with Avalonia UI framework. It offers rich game interface capabilities including health bars, inventory management, character status display, notifications, and extensive theming support.

## Features

### Core UI Capabilities

- **HUD Display**: Complete game HUD with health, level, gold, and experience bars
- **Panel Management**: Toggleable panels for inventory, character stats, minimap, and debug info
- **Notification System**: Rich notifications with color-coded types and auto-dismissal
- **Theming Support**: Dark/light themes with customizable color schemes
- **Responsive Design**: Adaptive layout for different screen resolutions
- **MVVM Architecture**: ReactiveUI integration for clean separation of concerns

### HUD Elements

- **Player Stats**: Health bar, level indicator, gold counter, experience progress
- **Quick Actions**: Toggle buttons for inventory, character, minimap, and debug panels
- **Location Info**: Current zone/area display
- **Quest Display**: Active quest information
- **Debug Panel**: Performance metrics and debugging information (toggleable)

### Notification Types

- Info, Success, Warning, Error
- Achievement, Quest, Combat
- Item, Character, Chat, Network
- Auto-dismissal with configurable timeout (default: 5 seconds)

## Architecture

### Plugin Structure

```
PigeonPea.Plugins.UI.Windows.AvaloniaHUD/
├── AvaloniaHudPlugin.cs          # Plugin entry point
├── AvaloniaHudManager.cs         # Main UI manager
├── ViewModels/
│   └── HudViewModel.cs            # MVVM view model
├── Controls/
│   ├── GameHUD.axaml             # Main HUD XAML
│   └── GameHUD.axaml.cs          # Code-behind
├── Themes/
│   └── GameHUDTheme.axaml        # Theme definitions
├── plugin.json                  # Plugin manifest
└── README.md                    # This file
```

### Dual Interface Implementation

The plugin implements two key interfaces for maximum compatibility:

1. **IUserInterface** (Game Layer): Rich UI functionality
   - Panel management
   - Notification system
   - Theme support
   - Advanced capabilities

2. **IService** (App Layer): Basic HUD compatibility
   - Show/hide messages
   - Root control access
   - App-level integration

### MVVM Pattern

- **ViewModels**: ReactiveUI-based view models with property change notification
- **Views**: Avalonia XAML controls with data binding
- **Commands**: ReactiveUI commands for user interactions
- **Data Binding**: Two-way binding between UI and view models

## Usage

### Basic Setup

```csharp
// Get UI service from registry
var ui = registry.Get<IUserInterface>();

// Initialize with context
var context = new UIContext
{
    Width = 1920,
    Height = 1080,
    Theme = UITheme.CreateDarkTheme(),
    TargetFPS = 60,
    EnableAnimations = true
};
ui.Initialize(context);

// Update with game state
ui.Update(gameState);

// Show notifications
ui.ShowNotification("Quest completed!", NotificationType.Success);
ui.ShowNotification("Low health!", NotificationType.Warning);

// Manage panels
ui.ShowPanel("inventory");
ui.HidePanel("character");
```

### Advanced Usage

```csharp
// Get typed manager for advanced features
var hudManager = registry.Get<AvaloniaHudManager>();

// Toggle specific panels
hudManager.ToggleInventory();
hudManager.ToggleCharacter();
hudManager.ToggleMinimap();
hudManager.ToggleDebug();

// Access root control for embedding
var rootControl = ui.GetRootControl();
// Embed in host application window
```

## Configuration

### Plugin Configuration (plugin.json)

- **Default Settings**: 1920x1080, 60 FPS, dark theme
- **Panel States**: Configurable default visibility for each panel
- **Notification Settings**: Timeout duration, auto-dismiss behavior
- **Theme Options**: Dark/light/custom theme support

### Runtime Configuration

```csharp
// Update UI context
var context = new UIContext
{
    Theme = customTheme,
    TargetFPS = 120,
    EnableAnimations = false
};
ui.Initialize(context);
```

## Theming

### Built-in Themes

- **Dark Theme**: Default dark theme with high contrast
- **Light Theme**: Light theme for daylight usage
- **Custom Themes**: Support for user-defined themes

### Theme Structure

```csharp
var theme = new UITheme
{
    Id = "custom",
    Name = "Custom Theme",
    Colors = new Dictionary<string, string>
    {
        ["primary"] = "#2196F3",
        ["secondary"] = "#FF9800",
        ["background"] = "#1A1A1A",
        ["surface"] = "#222222",
        ["text"] = "#FFFFFF",
        ["text-secondary"] = "#9E9E9E"
    },
    Typography = new UITypography
    {
        FontFamily = "Inter",
        FontSize = 12,
        FontWeight = "Normal"
    },
    Layout = new UILayout
    {
        CornerRadius = 4,
        Spacing = 8,
        Padding = 12
    }
};
```

## Integration

### With Rendering Systems

The HUD overlays on top of rendering systems:

1. **Rendering Layer**: Game world (maps, sprites, entities)
2. **UI Layer**: HUD elements (health, menus, notifications)

Both layers work independently but can be coordinated through game state updates.

### With Input Systems

```csharp
// Handle UI-related input
inputSystem.BindAction("toggle_inventory", hudManager.ToggleInventory);
inputSystem.BindAction("toggle_character", hudManager.ToggleCharacter);
inputSystem.BindAction("toggle_minimap", hudManager.ToggleMinimap);
```

## Dependencies

### Framework Dependencies

- **.NET 8.0 Windows**: Target framework
- **Avalonia UI 11.2.2**: UI framework
- **ReactiveUI 20.1.1**: MVVM framework
- **Serilog**: Logging framework

### Project Dependencies

- **PigeonPea.Contracts**: App-level contracts
- **PigeonPea.Game.Contracts**: Game-level contracts
- **PigeonPea.Plugin**: Plugin system

## Performance

### Optimization Features

- **Efficient Data Binding**: ReactiveUI for minimal UI updates
- **Notification Management**: Automatic cleanup and size limits
- **Resource Management**: Proper disposal and cleanup
- **Thread Safety**: UI thread marshaling for cross-thread operations

### Memory Usage

- **Base Memory**: ~2MB for core UI components
- **Per Notification**: ~1KB per active notification
- **Theme Resources**: ~100KB per loaded theme
- **Total Typical**: 5-10MB depending on active features

## Testing

### Test Coverage

- **Unit Tests**: View model logic and command handling
- **Integration Tests**: Plugin system integration
- **UI Tests**: Control rendering and interaction
- **Performance Tests**: Memory usage and rendering performance

### Running Tests

```bash
dotnet test --framework net8.0-windows
```

## Troubleshooting

### Common Issues

1. **UI Not Visible**: Check initialization and context settings
2. **Notifications Not Showing**: Verify notification timeout settings
3. **Theme Not Applying**: Ensure theme resources are properly loaded
4. **Performance Issues**: Disable animations or reduce update frequency

### Debug Mode

Enable debug panel to see:
- Current FPS
- Memory usage
- Active UI objects
- Last update timestamp

## Development

### Building

```bash
dotnet build --configuration Release
```

### Testing

```bash
dotnet test --configuration Debug
```

### Packaging

```bash
dotnet pack --configuration Release
```

## Contributing

### Development Setup

1. Install .NET 8.0 SDK
2. Install Visual Studio 2022 or later
3. Clone repository and open solution
4. Restore NuGet packages

### Code Style

- **C# Conventions**: Microsoft C# coding conventions
- **XAML Style**: Avalonia XAML coding guidelines
- **Documentation**: XML documentation for public APIs
- **Testing**: Unit tests with xUnit

## License

MIT License - see LICENSE file for details.

## Changelog

### v1.0.0 (2025-11-17)

- Initial release
- Avalonia-based HUD implementation
- Rich UI capabilities (HUD, menus, dialogs, notifications)
- Theme support with dark/light modes
- Player stats display (health, level, gold, experience)
- Panel management (inventory, character, minimap, debug)
- ReactiveUI integration for MVVM pattern
- Plugin system integration with dual interface support

## Support

- **Documentation**: See inline XML documentation
- **Examples**: Check test projects for usage examples
- **Issues**: Report via GitHub issue tracker
- **Community**: Join discussions for questions and feedback
