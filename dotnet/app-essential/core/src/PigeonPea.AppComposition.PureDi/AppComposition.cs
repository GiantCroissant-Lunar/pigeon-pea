using System;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;
using PigeonPea.Contracts.DependencyInjection;
using PigeonPea.PluginSystem;

namespace PigeonPea.AppComposition.PureDi;

public partial class AppComposition
{
    static void Setup() =>
        DI.Setup(nameof(AppComposition));
}

public sealed class PureDiAppServiceRootFactory : IAppServiceRootFactory
{
    public IAppServiceRoot Build(IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        var fallbackFactory = new MsDiAppServiceRootFactory();
        return fallbackFactory.Build(services);
    }
}
