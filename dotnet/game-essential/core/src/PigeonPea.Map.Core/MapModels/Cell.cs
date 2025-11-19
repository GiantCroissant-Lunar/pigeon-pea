namespace PigeonPea.Map.Core;

public sealed class Cell
{
    public int Id { get; }
    public double Height { get; }
    public int Biome { get; }
    public Point Center { get; }

    internal Cell(FantasyMapGenerator.Core.Models.Cell c)
    {
        Id = c.Id;
        Height = c.Height;
        Biome = c.Biome;
        Center = new Point(c.Center.X, c.Center.Y);
    }
}
