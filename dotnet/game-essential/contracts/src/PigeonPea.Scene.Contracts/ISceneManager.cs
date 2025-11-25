using Arch.Core;

namespace PigeonPea.Scene.Contracts;

public interface ISceneManager
{
    Task<Scene> LoadSceneAsync(string sceneName, SceneLoadMode mode);
    Task UnloadSceneAsync(Guid sceneId);
    Scene? GetActiveScene();
    IEnumerable<Scene> GetAllScenes();
    Scene? GetSceneById(Guid sceneId);
    Task TransitionToSceneAsync(string sceneName, ITransitionEffect transitionEffect);
}

public interface ITransitionEffect
{
    // Placeholder for future transition effects
}
