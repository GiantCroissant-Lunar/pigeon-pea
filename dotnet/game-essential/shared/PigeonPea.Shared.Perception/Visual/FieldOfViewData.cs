namespace PigeonPea.Shared.Perception.Visual;

public sealed class FieldOfViewData
{
    public (int X, int Y) CenterPosition { get; set; }
    public float ViewDistance { get; set; }
    public HashSet<(int X, int Y)> VisibleTiles { get; set; } = new();

    public bool IsVisible(int x, int y) => VisibleTiles.Contains((x, y));

    public bool IsVisible((int X, int Y) position) => VisibleTiles.Contains(position);
}
