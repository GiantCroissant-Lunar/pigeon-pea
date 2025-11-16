namespace PigeonPea.Map.Core;

public sealed class State
{
    public int Id { get; }
    public string Name { get; }
    public string FullName { get; }
    public string Color { get; }
    public int CapitalBurgId { get; }
    public int CenterCellId { get; }
    public int CultureId { get; }
    public int CellCount { get; }
    public double RuralPopulation { get; }
    public double UrbanPopulation { get; }

    internal State(FantasyMapGenerator.Core.Models.State state)
    {
        Id = state.Id;
        Name = state.Name;
        FullName = state.FullName;
        Color = state.Color;
        CapitalBurgId = state.CapitalBurgId;
        CenterCellId = state.CenterCellId;
        CultureId = state.CultureId;
        CellCount = state.CellCount;
        RuralPopulation = state.RuralPopulation;
        UrbanPopulation = state.UrbanPopulation;
    }
}
