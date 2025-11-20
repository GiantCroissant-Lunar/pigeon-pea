using Arch.Core;

namespace PigeonPea.Scene.Contracts;

public class Scene
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public World World { get; init; }
    public SceneState State { get; set; }
    public Scene(string name, World world)
    {
        Id = Guid.NewGuid();
        Name = name;
        World = world;
        State = SceneState.Loading;
    }
}
