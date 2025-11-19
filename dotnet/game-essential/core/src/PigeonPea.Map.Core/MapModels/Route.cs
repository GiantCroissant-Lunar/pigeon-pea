using System.Collections.Generic;
using System.Linq;

namespace PigeonPea.Map.Core;

public sealed class Route
{
    public int Id { get; }
    public string Name { get; }
    public IReadOnlyList<int> Cells { get; }

    internal Route(FantasyMapGenerator.Core.Models.Route route)
    {
        Id = route.Id;
        Name = route.Name;
        Cells = route.Path?.ToList() ?? new List<int>();
    }
}
