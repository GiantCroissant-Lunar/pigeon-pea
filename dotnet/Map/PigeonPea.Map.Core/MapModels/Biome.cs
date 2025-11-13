namespace PigeonPea.Map.Core;

public sealed class Biome
{
    public int Id { get; }
    public string Name { get; }
    public string Color { get; }

    internal Biome(FantasyMapGenerator.Core.Models.Biome b)
    {
        Id = b.Id;
        Name = b.Name;
        Color = b.Color;
    }
}
