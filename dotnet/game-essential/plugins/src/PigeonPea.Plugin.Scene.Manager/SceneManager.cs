using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Shared.Scale;
using SceneContracts = PigeonPea.Scene.Contracts;

namespace PigeonPea.Plugin.Scene.Manager;

public class SceneManager : SceneContracts.ISceneManager, SceneContracts.ISceneServiceProvider
{
    private readonly Dictionary<Guid, SceneContracts.Scene> _scenes = new();
    private readonly IScaleManager? _scaleManager;
    private readonly ILogger<SceneManager>? _logger;
    private readonly IServiceProvider? _hostServices;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly Dictionary<Guid, IServiceProvider> _sceneServices = new();
    private readonly Dictionary<Guid, IServiceScope> _sceneScopes = new();
    private Guid? _activeSceneId;

    public SceneManager(IScaleManager? scaleManager = null, ILogger<SceneManager>? logger = null, IServiceProvider? hostServices = null)
    {
        _scaleManager = scaleManager;
        _logger = logger;
        _hostServices = hostServices;
        _scopeFactory = _hostServices?.GetService<IServiceScopeFactory>();

        if (_scaleManager != null)
        {
            _scaleManager.ScaleChanged += OnScaleChanged;
        }
    }

    private void OnScaleChanged(object? sender, ScaleChangedEventArgs e)
    {
        if (e.PreviousScale.Environment == e.NewScale.Environment)
        {
            return;
        }

        _logger?.LogInformation(
            "Scale environment changed: {PreviousEnv} → {NewEnv}, triggering scene transition",
            e.PreviousScale.Environment, e.NewScale.Environment);

        var sceneName = e.NewScale.Environment switch
        {
            "dungeon" => "DungeonScene",
            "world" => "WorldMapScene",
            "vehicle" => "VehicleScene",
            _ => "WorldMapScene"
        };

        Task.Run(async () =>
        {
            try
            {
                await LoadSceneAsync(sceneName, SceneContracts.SceneLoadMode.Single);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to transition to scene {SceneName}", sceneName);
            }
        });
    }

    public async Task<SceneContracts.Scene> LoadSceneAsync(string sceneName, SceneContracts.SceneLoadMode mode)
    {
        SceneContracts.SceneRole role;
        Guid? parentSceneId = null;

        if (mode == SceneContracts.SceneLoadMode.Single)
        {
            role = SceneContracts.SceneRole.Main;
        }
        else
        {
            role = SceneContracts.SceneRole.Sub;
            parentSceneId = _activeSceneId;
        }

        var scene = new SceneContracts.Scene(sceneName, World.Create(), role, parentSceneId)
        {
            State = SceneContracts.SceneState.Active
        };

        _scenes[scene.Id] = scene;

        if (_scopeFactory != null)
        {
            if (role == SceneContracts.SceneRole.Main)
            {
                var scope = _scopeFactory.CreateScope();
                _sceneScopes[scene.Id] = scope;
                _sceneServices[scene.Id] = scope.ServiceProvider;
            }
            else if (parentSceneId.HasValue && _sceneScopes.TryGetValue(parentSceneId.Value, out var parentScope))
            {
                _sceneServices[scene.Id] = parentScope.ServiceProvider;
            }
        }
        else if (_hostServices != null)
        {
            if (role == SceneContracts.SceneRole.Main)
            {
                _sceneServices[scene.Id] = _hostServices;
            }
            else if (parentSceneId.HasValue && _sceneServices.TryGetValue(parentSceneId.Value, out var parentServices))
            {
                _sceneServices[scene.Id] = parentServices;
            }
        }

        if (mode == SceneContracts.SceneLoadMode.Single)
        {
            var toUnload = _scenes.Keys.Where(id => id != scene.Id).ToList();
            foreach (var id in toUnload)
            {
                await UnloadSceneAsync(id).ConfigureAwait(false);
            }
        }

        _activeSceneId = scene.Id;
        return scene;
    }

    public Task UnloadSceneAsync(Guid sceneId)
    {
        if (_scenes.TryGetValue(sceneId, out var scene))
        {
            scene.State = SceneContracts.SceneState.Unloading;
            // TODO: Clean up world and entities
            _scenes.Remove(sceneId);

            if (_sceneScopes.TryGetValue(sceneId, out var scope))
            {
                scope.Dispose();
                _sceneScopes.Remove(sceneId);
            }

            if (_sceneServices.ContainsKey(sceneId))
            {
                _sceneServices.Remove(sceneId);
            }

            if (_activeSceneId == sceneId)
            {
                _activeSceneId = null;
            }
        }
        return Task.CompletedTask;
    }

    public SceneContracts.Scene? GetActiveScene()
    {
        if (_activeSceneId.HasValue && _scenes.TryGetValue(_activeSceneId.Value, out var scene))
        {
            return scene;
        }
        return null;
    }

    public IEnumerable<SceneContracts.Scene> GetAllScenes()
    {
        return _scenes.Values;
    }

    public SceneContracts.Scene? GetSceneById(Guid sceneId)
    {
        _scenes.TryGetValue(sceneId, out var scene);
        return scene;
    }

    public IServiceProvider GetServices(SceneContracts.Scene scene)
    {
        if (_sceneServices.TryGetValue(scene.Id, out var services))
        {
            return services;
        }

        if (_hostServices != null)
        {
            return _hostServices;
        }

        throw new InvalidOperationException($"No services available for scene '{scene.Name}' ({scene.Id}).");
    }

    public Task TransitionToSceneAsync(string sceneName, SceneContracts.ITransitionEffect transitionEffect)
    {
        // TODO: Implement transition effects
        return LoadSceneAsync(sceneName, SceneContracts.SceneLoadMode.Single);
    }
}
