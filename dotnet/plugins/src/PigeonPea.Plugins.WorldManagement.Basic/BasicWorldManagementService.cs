using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.WorldManagement.Models;
using PigeonPea.Game.Contracts.WorldManagement.Services;

namespace PigeonPea.Plugins.WorldManagement.Basic;

public class BasicWorldManagementService : IService, IPlugin
{
    private readonly Dictionary<string, World> _worlds = new();

    public string Id => "pigeon-pea.plugins.worldmanagement.basic";
    public string Name => "Basic World Management Service";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        context.Registry.Register<IService>(this);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct = default)
    {
        foreach (var world in _worlds.Values)
        {
            world.Dispose();
        }
        _worlds.Clear();
        return Task.CompletedTask;
    }

    public World CreateWorld(string worldId, WorldConfig config)
    {
        if (_worlds.ContainsKey(worldId))
        {
            throw new ArgumentException($"World {worldId} already exists");
        }

        var world = World.Create();
        _worlds[worldId] = world;
        return world;
    }

    public void DestroyWorld(string worldId)
    {
        if (_worlds.TryGetValue(worldId, out var world))
        {
            world.Dispose();
            _worlds.Remove(worldId);
        }
    }

    public World? GetWorld(string worldId)
    {
        return _worlds.TryGetValue(worldId, out var world) ? world : null;
    }

    public void TransferEntity(Entity entity, World sourceWorld, World targetWorld)
    {
        // Basic implementation: just create a new entity.
        // In a real implementation, we would copy all components.
        // Arch doesn't have a built-in "Move" between worlds.

        var newEntity = targetWorld.Create();

        // TODO: Copy components using reflection or known types
        // For now, this is a placeholder
    }
}

