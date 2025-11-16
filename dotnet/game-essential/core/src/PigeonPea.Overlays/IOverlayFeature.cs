using System.Collections.Generic;

namespace PigeonPea.Overlays;

public interface IOverlayFeature<out TPosition>
{
    string LayerId { get; }
    TPosition Position { get; }
    string Kind { get; }
    string Name { get; }
    IReadOnlyDictionary<string, object?> Metadata { get; }
}
