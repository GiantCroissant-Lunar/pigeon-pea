using System;

namespace PigeonPea.Platform.Contracts.Scene;

public interface ISceneServiceProvider
{
    IServiceProvider GetServices(Scene scene);
}
