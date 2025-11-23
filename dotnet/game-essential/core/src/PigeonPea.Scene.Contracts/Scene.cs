using Arch.Core;

namespace PigeonPea.Scene.Contracts;

public class Scene
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public World World { get; init; }
    public SceneState State { get; set; }
    public SceneRole Role { get; init; }
    public Guid? ParentSceneId { get; init; }

    public Scene(string name, World world, SceneRole role = SceneRole.Main, Guid? parentSceneId = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        World = world;
        State = SceneState.Loading;
        Role = role;
        ParentSceneId = parentSceneId;
    }
}
