using FmgDungeon = FantasyMapGenerator.Core.Models.Dungeon;

namespace PigeonPea.Map.Core;

public sealed class Dungeon
{
    public int Id { get; }
    public string Name { get; }
    public Point Origin { get; }
    public int Width { get; }
    public int Height { get; }
    public int? AnchorCellId { get; }

    internal Dungeon(FmgDungeon dungeon)
    {
        Id = dungeon.Id;
        Name = dungeon.Name;
        Origin = new Point(dungeon.Origin.X, dungeon.Origin.Y);
        Width = dungeon.Width;
        Height = dungeon.Height;
        AnchorCellId = dungeon.AnchorCellId;
    }
}
