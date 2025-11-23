using System;

namespace PigeonPea.Scene.Contracts;

public interface ISceneServiceProvider
{
    IServiceProvider GetServices(Scene scene);
}
