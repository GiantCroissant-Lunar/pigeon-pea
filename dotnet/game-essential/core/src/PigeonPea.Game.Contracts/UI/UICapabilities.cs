using System;

namespace PigeonPea.Game.Contracts.UI;

/// <summary>
/// Flags indicating UI capabilities supported by implementations.
/// </summary>
[Flags]
public enum UICapabilities
{
    /// <summary>
    /// No UI capabilities.
    /// </summary>
    None = 0,

    /// <summary>
    /// Basic HUD display (health bars, status indicators).
    /// </summary>
    HUD = 1 << 0,

    /// <summary>
    /// Menu system (main menu, pause menu, settings).
    /// </summary>
    Menus = 1 << 1,

    /// <summary>
    /// Dialog system (message boxes, confirmations).
    /// </summary>
    Dialogs = 1 << 2,

    /// <summary>
    /// Tooltip system (hover information, help text).
    /// </summary>
    Tooltips = 1 << 3,

    /// <summary>
    /// Notification system (toast messages, alerts).
    /// </summary>
    Notifications = 1 << 4,

    /// <summary>
    /// Inventory display (character inventory, item management).
    /// </summary>
    Inventory = 1 << 5,

    /// <summary>
    /// Character status display (stats, skills, abilities).
    /// </summary>
    CharacterStatus = 1 << 6,

    /// <summary>
    /// Minimap display.
    /// </summary>
    Minimap = 1 << 7,

    /// <summary>
    /// Custom controls support (user-defined UI elements).
    /// </summary>
    CustomControls = 1 << 8,

    /// <summary>
    /// Animation support (transitions, effects).
    /// </summary>
    Animations = 1 << 9,

    /// <summary>
    /// Theme support (multiple visual themes).
    /// </summary>
    Theming = 1 << 10,

    /// <summary>
    /// Localization support (multiple languages).
    /// </summary>
    Localization = 1 << 11,

    /// <summary>
    /// Accessibility features (screen readers, high contrast).
    /// </summary>
    Accessibility = 1 << 12,

    /// <summary>
    /// Input binding support (customizable controls).
    /// </summary>
    InputBinding = 1 << 13,

    /// <summary>
    /// All standard UI capabilities.
    /// </summary>
    AllStandard = HUD | Menus | Dialogs | Tooltips | Notifications |
               Inventory | CharacterStatus | Minimap | CustomControls |
               Animations | Theming | Localization | Accessibility | InputBinding
}
