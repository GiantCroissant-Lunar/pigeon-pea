using Arch.Core.Extensions;
using PigeonPea.Game.Inventory.Components;
using PigeonPea.Shared;

namespace PigeonPea.Game.Inventory;

/// <summary>
/// Extension helpers for integrating the shared GameWorld with the game-level
/// inventory component backed by PigeonPea.Shared.Inventory.
/// </summary>
public static class GameWorldInventoryExtensions
{
    /// <summary>
    /// Ensures that the player entity in the given GameWorld has an InventoryComponent
    /// attached. If the component is already present, it is left unchanged.
    /// </summary>
    public static void EnsurePlayerInventory(this GameWorld world, int maxSlots, float maxWeight)
    {
        var player = world.PlayerEntity;

        if (!player.Has<InventoryComponent>())
        {
            player.Add(new InventoryComponent(maxSlots, maxWeight));
        }
    }
}
