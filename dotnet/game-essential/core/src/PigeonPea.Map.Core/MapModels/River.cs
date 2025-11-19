using System.Collections.Generic;
using System.Linq;

namespace PigeonPea.Map.Core;

public sealed class River
{
    public IReadOnlyList<int> Cells { get; }

    internal River(FantasyMapGenerator.Core.Models.River r)
    {
        Cells = r.Cells?.ToList() ?? new List<int>();
    }
}
