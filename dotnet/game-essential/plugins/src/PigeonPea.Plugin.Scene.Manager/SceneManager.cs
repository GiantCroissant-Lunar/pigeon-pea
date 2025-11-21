using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Extensions.Logging;
using PigeonPea.Shared.Scale;
using SceneContracts = PigeonPea.Scene.Contracts;

namespace PigeonPea.Plugin.Scene.Manager;

public class SceneManager : SceneContracts.ISceneManager
{
    private readonly Dictionary<Guid, SceneContracts.Scene> _scenes = new();
    private readonly IScaleManager? _scaleManager;
    private readonly ILogger<SceneManager>? _logger;
    private Guid? _activeSceneId;

    public SceneManager(IScaleManager? scaleManager = null, ILogger<SceneManager>? logger = null)
    {
        _scaleManager = scaleManager;
        _logger = logger;

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
        var scene = new SceneContracts.Scene(sceneName, World.Create())
        {
            State = SceneContracts.SceneState.Active
        };

        _scenes[scene.Id] = scene;

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

    public Task TransitionToSceneAsync(string sceneName, SceneContracts.ITransitionEffect transitionEffect)
    {
        // TODO: Implement transition effects
        return LoadSceneAsync(sceneName, SceneContracts.SceneLoadMode.Single);
    }
}
