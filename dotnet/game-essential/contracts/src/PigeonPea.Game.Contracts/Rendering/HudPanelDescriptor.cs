using System.Collections.Generic;

namespace PigeonPea.Game.Contracts.Rendering;

/// <summary>
/// Describes a logical HUD panel that can be hosted by an IGameHud implementation.
/// Panels are host-agnostic; host-specific HUDs decide how to render them.
/// </summary>
public sealed class HudPanelDescriptor
{
    /// <summary>
    /// Stable identifier for this panel (e.g., "inventory", "player-stats").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name (e.g., "Inventory").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Ordering hint for layout; lower values appear earlier.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Optional region hint (e.g., "left", "right", "bottom").
    /// Host HUDs may ignore this.
    /// </summary>
    public string? Region { get; set; }
}

/// <summary>
/// Provides HUD panel descriptors for a feature or plugin.
/// </summary>
public interface IHudPanelDescriptorProvider
{
    /// <summary>
    /// Gets the HUD panels contributed by this provider.
    /// </summary>
    IEnumerable<HudPanelDescriptor> GetPanels();
}
