namespace PigeonPea.Map.Core;

public sealed class Culture
{
    public int Id { get; }
    public string Name { get; }
    public string Code { get; }
    public string Color { get; }
    public int CenterCellId { get; }

    internal Culture(FantasyMapGenerator.Core.Models.Culture culture)
    {
        Id = culture.Id;
        Name = culture.Name;
        Code = culture.Code;
        Color = culture.Color;
        CenterCellId = culture.CenterCellId;
    }
}
