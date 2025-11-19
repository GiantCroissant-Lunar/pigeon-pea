namespace PigeonPea.Map.Core;

public sealed class Burg
{
    public int Id { get; }
    public string Name { get; }
    public int CellId { get; }
    public Point Position { get; }
    public int StateId { get; }
    public int CultureId { get; }
    public bool IsCapital { get; }
    public bool IsPort { get; }
    public double Population { get; }

    internal Burg(FantasyMapGenerator.Core.Models.Burg burg)
    {
        Id = burg.Id;
        Name = burg.Name;
        CellId = burg.CellId;
        Position = new Point(burg.Position.X, burg.Position.Y);
        StateId = burg.StateId;
        CultureId = burg.CultureId;
        IsCapital = burg.IsCapital;
        IsPort = burg.IsPort;
        Population = burg.Population;
    }
}
