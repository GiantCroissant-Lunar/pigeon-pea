namespace PigeonPea.Game.Contracts.Scenes.Models;

public sealed class DungeonBootstrapOptions
{
    public int Width { get; init; }
    public int Height { get; init; }
    public string? DungeonGeneratorId { get; init; }
    public int? Seed { get; init; }
}
