namespace PigeonPea.Platform.Plugin.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Platform.Contracts.Core;

/// <summary>
/// Extension methods for registering plugin services with DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add plugin system core services to the DI container.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration root (optional, will be resolved from DI if null)</param>
    public static IServiceCollection AddPluginCore(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Core plugin services
        services.AddSingleton<ServiceRegistry>();
        services.AddSingleton<EventBus>();
        
        services.AddSingleton<IRegistry>(sp =>
        {
            var registry = sp.GetRequiredService<ServiceRegistry>();
            var eventBus = sp.GetRequiredService<EventBus>();

            // Register EventBus in the ServiceRegistry so plugins can access it via IRegistry
            registry.Register<IEventBus>(eventBus, new ServiceMetadata
            {
                Priority = 1000, // Framework service
                Name = "EventBus",
                Version = "1.0.0"
            });

            return registry;
        });
        
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<EventBus>());

        // Plugin host
        services.AddSingleton<IPluginHost>(sp => 
            new PluginHost(sp.GetRequiredService<ILoggerFactory>(), sp));

        return services;
    }
}
