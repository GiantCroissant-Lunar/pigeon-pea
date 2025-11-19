using System;

namespace PigeonPea.Map.Core;

public sealed class MapGenerationHooks
{
    public Action<MapData>? AfterBaseMapGenerated { get; init; }
    public Action<MapData>? AfterPoliticalMapGenerated { get; init; }
    public Action<MapData>? AfterFinalization { get; init; }
}
