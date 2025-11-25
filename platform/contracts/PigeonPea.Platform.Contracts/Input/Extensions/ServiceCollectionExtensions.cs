using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Platform.Contracts.Input.Services;
using PigeonPea.Platform.Contracts.Input.Services.Proxy;

namespace PigeonPea.Platform.Contracts.Input.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInputServiceProxy(this IServiceCollection services)
    {
        services.AddSingleton<IService, Service>();
        return services;
    }
}
