namespace PigeonPea.Shared.Inventory.Items;

/// <summary>
/// Data-driven template describing an item type.
/// This is engine-agnostic and does not reference ECS or plugins.
/// </summary>
public sealed class ItemDefinition
{
    /// <summary>
    /// Unique identifier, e.g. "health_potion_small".
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description / flavor text.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// High-level item category.
    /// </summary>
    public ItemType Type { get; init; } = ItemType.Unknown;

    /// <summary>
    /// Rarity/quality tier.
    /// </summary>
    public ItemRarity Rarity { get; init; } = ItemRarity.Common;

    /// <summary>
    /// Maximum number of items in a single stack. 1 = non-stackable.
    /// </summary>
    public int MaxStack { get; init; } = 1;

    /// <summary>
    /// Weight of a single unit of this item.
    /// </summary>
    public float Weight { get; init; }

    /// <summary>
    /// Optional icon/asset identifier for UIs.
    /// </summary>
    public string? IconId { get; init; }
}
