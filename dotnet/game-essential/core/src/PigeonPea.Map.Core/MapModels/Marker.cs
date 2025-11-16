namespace PigeonPea.Map.Core;

public sealed class Marker
{
    public int Id { get; }
    public string Type { get; }
    public int CellId { get; }
    public Point Position { get; }
    public string Icon { get; }
    public string Name { get; }
    public string Description { get; }

    internal Marker(FantasyMapGenerator.Core.Models.Marker marker)
    {
        Id = marker.Id;
        Type = marker.Type.ToString();
        CellId = marker.CellId;
        Position = new Point(marker.Position.X, marker.Position.Y);
        Icon = marker.Icon;
        Name = marker.Name;
        Description = marker.Description;
    }
}
