using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Config.Contracts.Services;
using PigeonPea.Config.Contracts.Services.Proxy;

namespace PigeonPea.Config.Contracts.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfigServiceProxy(this IServiceCollection services)
    {
        services.AddSingleton<IService, Service>();
        return services;
    }
}
