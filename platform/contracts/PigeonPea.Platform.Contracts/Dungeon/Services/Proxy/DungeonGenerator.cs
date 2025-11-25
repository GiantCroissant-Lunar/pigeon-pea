using System;
using Arch.Core;
using PigeonPea.Platform.Contracts.Dungeon;
using PigeonPea.Platform.Contracts.Core;
using PigeonPea.Platform.Contracts.Core.Attributes;

namespace PigeonPea.Platform.Contracts.Dungeon.Services.Proxy;

/// <summary>
/// Proxy implementation of IDungeonGenerator.
/// </summary>
[RealizeService(typeof(IDungeonGenerator))]
public partial class DungeonGenerator : IDungeonGenerator
{
    private readonly IRegistry _registry;

    public DungeonGenerator(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }
}
