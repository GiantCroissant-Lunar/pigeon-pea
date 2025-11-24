namespace PigeonPea.Shared.Inventory.Items;

/// <summary>
/// Runtime instance of an item. Refers back to an ItemDefinition and tracks stack quantity.
/// Additional per-instance state (durability, rolled modifiers, etc.) can be added over time.
/// </summary>
public sealed class ItemInstance
{
    /// <summary>
    /// Identifier of the ItemDefinition this instance refers to.
    /// </summary>
    public string DefinitionId { get; init; } = string.Empty;

    /// <summary>
    /// Number of items in this stack. Always &gt;= 1.
    /// </summary>
    public int Quantity { get; set; }
}
