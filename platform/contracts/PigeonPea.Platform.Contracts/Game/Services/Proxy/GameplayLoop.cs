using System;
using Arch.Core;
using PigeonPea.Platform.Contracts.Game.Services;
using PigeonPea.Platform.Contracts.Core;
using PigeonPea.Platform.Contracts.Core.Attributes;

namespace PigeonPea.Platform.Contracts.Game.Services.Proxy;

/// <summary>
/// Proxy implementation of IGameplayLoop.
/// </summary>
[RealizeService(typeof(IGameplayLoop))]
public partial class GameplayLoop : IGameplayLoop
{
    private readonly IRegistry _registry;

    public GameplayLoop(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }
}
