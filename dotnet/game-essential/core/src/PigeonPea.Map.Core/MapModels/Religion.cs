namespace PigeonPea.Map.Core;

public sealed class Religion
{
    public int Id { get; }
    public string Name { get; }
    public string Color { get; }
    public int CenterCellId { get; }

    internal Religion(FantasyMapGenerator.Core.Models.Religion religion)
    {
        Id = religion.Id;
        Name = religion.Name;
        Color = religion.Color;
        CenterCellId = religion.CenterCellId;
    }
}
