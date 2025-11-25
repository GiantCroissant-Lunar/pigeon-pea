using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Platform.Contracts.Config.Services;
using PigeonPea.Platform.Contracts.Config.Services.Proxy;

namespace PigeonPea.Platform.Contracts.Config.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfigServiceProxy(this IServiceCollection services)
    {
        services.AddSingleton<IService, Service>();
        return services;
    }
}
