using System.Collections.Generic;
using System.Linq;

namespace PigeonPea.Map.Core;

public sealed class River
{
    public IReadOnlyList<int> Cells { get; }

    /// <summary>
    /// Optional smoothed river path provided by FMG for nicer rendering.
    /// Falls back to the raw cell path when empty.
    /// </summary>
    public IReadOnlyList<Point> MeanderedPath { get; }

    internal River(FantasyMapGenerator.Core.Models.River r)
    {
        Cells = r.Cells?.ToList() ?? new List<int>();
        MeanderedPath = r.MeanderedPath?.Select(p => new Point(p.X, p.Y)).ToList()
                         ?? new List<Point>();
    }
}
