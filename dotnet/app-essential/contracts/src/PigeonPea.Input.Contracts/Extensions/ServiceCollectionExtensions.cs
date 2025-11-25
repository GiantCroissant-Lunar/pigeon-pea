using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Input.Contracts.Services;
using PigeonPea.Input.Contracts.Services.Proxy;

namespace PigeonPea.Input.Contracts.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInputServiceProxy(this IServiceCollection services)
    {
        services.AddSingleton<IService, Service>();
        return services;
    }
}
