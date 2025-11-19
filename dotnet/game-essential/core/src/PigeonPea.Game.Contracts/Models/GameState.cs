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
}
