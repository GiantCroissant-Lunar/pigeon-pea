namespace PigeonPea.Game.Contracts.Models;

/// <summary>
/// Minimal placeholder for game state consumed by renderers.
/// This will be expanded in future phases.
/// </summary>
public class GameState
{
    public PigeonPea.Dungeon.Contracts.Models.DungeonView? Dungeon { get; set; }

    public int PlayerX { get; set; }

    public int PlayerY { get; set; }

    public PigeonPea.Game.Contracts.Stats.Models.StatsView? Stats { get; set; }

    public PigeonPea.Game.Contracts.Avatar.Models.AvatarView? Avatar { get; set; }

    public PigeonPea.Game.Contracts.Inventory.Services.InventoryView? Inventory { get; set; }
}
