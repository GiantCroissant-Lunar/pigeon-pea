using Arch.Core;

namespace PigeonPea.Game.Contracts.Inventory.Services;

/// <summary>
/// Game-level inventory service contract.
/// Implementations are provided by plugins and operate on Arch.Core entities.
/// </summary>
public interface IService
{
    /// <summary>
    /// Tries to add an item stack to the entity's inventory.
    /// </summary>
    /// <param name="entity">Target entity.</param>
    /// <param name="definitionId">Item definition identifier (e.g. "health_potion_small").</param>
    /// <param name="quantity">Number of items to add.</param>
    /// <returns>True if the entire quantity was added; false if operation failed or was partial.</returns>
    bool TryAddItem(Entity entity, string definitionId, int quantity);

    /// <summary>
    /// Tries to remove an item stack from the entity's inventory.
    /// </summary>
    /// <param name="entity">Target entity.</param>
    /// <param name="definitionId">Item definition identifier.</param>
    /// <param name="quantity">Number of items to remove.</param>
    /// <returns>True if the entire quantity was removed; false otherwise.</returns>
    bool TryRemoveItem(Entity entity, string definitionId, int quantity);

    /// <summary>
    /// Tries to move an item stack between inventory slots.
    /// </summary>
    /// <param name="entity">Target entity.</param>
    /// <param name="fromSlotIndex">Zero-based index of the source slot.</param>
    /// <param name="toSlotIndex">Zero-based index of the destination slot.</param>
    /// <returns>True if the move succeeded; false otherwise.</returns>
    bool TryMoveItem(Entity entity, int fromSlotIndex, int toSlotIndex);

    /// <summary>
    /// Tries to equip an item from the inventory into an equipment slot.
    /// </summary>
    /// <param name="entity">Target entity.</param>
    /// <param name="fromSlotIndex">Inventory slot index holding the item.</param>
    /// <param name="equipmentSlotId">Equipment slot identifier (e.g. "Head", "Weapon").</param>
    /// <returns>True if the item was equipped; false otherwise.</returns>
    bool TryEquip(Entity entity, int fromSlotIndex, string equipmentSlotId);

    /// <summary>
    /// Tries to unequip an item from an equipment slot back into the inventory.
    /// </summary>
    /// <param name="entity">Target entity.</param>
    /// <param name="equipmentSlotId">Equipment slot identifier.</param>
    /// <returns>True if the item was unequipped; false otherwise.</returns>
    bool TryUnequip(Entity entity, string equipmentSlotId);

    /// <summary>
    /// Gets a read-only snapshot view of the entity's inventory and equipment.
    /// </summary>
    /// <param name="entity">Target entity.</param>
    /// <returns>Inventory view DTO describing slots, stacks, and summary information.</returns>
    InventoryView GetInventory(Entity entity);
}

/// <summary>
/// Read-only DTO describing an entity's inventory state.
/// </summary>
public sealed class InventoryView
{
    public int MaxSlots { get; init; }
    public float MaxWeight { get; init; }
    public float CurrentWeight { get; init; }
    public IReadOnlyList<InventorySlotView> Slots { get; init; } = Array.Empty<InventorySlotView>();
}

/// <summary>
/// Read-only DTO describing a single inventory slot.
/// </summary>
public sealed class InventorySlotView
{
    public int SlotIndex { get; init; }
    public string? DefinitionId { get; init; }
    public int Quantity { get; init; }
}
