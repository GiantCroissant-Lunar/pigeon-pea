using Arch.Core;
using PigeonPea.Platform.Contracts.Dungeon.Models;

namespace PigeonPea.Platform.Contracts.Dungeon;

public class DungeonGenerationOptions
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int? Seed { get; set; }
}

public interface IDungeonGenerator
{
    /// <summary>
    /// Generates a dungeon and creates it as an entity in the provided world.
    /// </summary>
    /// <param name="world">The ECS world to create the dungeon entity in</param>
    /// <param name="options">Generation parameters (size, seed, etc.)</param>
    /// <returns>The created dungeon entity</returns>
    Entity Generate(World world, DungeonGenerationOptions options);
}
