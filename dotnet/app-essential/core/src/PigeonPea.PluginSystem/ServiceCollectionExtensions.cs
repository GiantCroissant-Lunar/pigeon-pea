using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.PluginSystem;

/// <summary>
/// DI extensions to register the plugin system.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPluginSystem(this IServiceCollection services, IConfiguration configuration)
    {
        // Core singletons
        services.AddSingleton<PluginRegistry>();
        services.AddSingleton<ServiceRegistry>();
        services.AddSingleton<EventBus>();

        // Exposed interfaces
        services.AddSingleton<IRegistry>(sp =>
        {
            var registry = sp.GetRequiredService<ServiceRegistry>();
            var eventBus = sp.GetRequiredService<EventBus>();

            // Register EventBus as a framework service with high priority
            registry.Register<IEventBus>(eventBus, new ServiceMetadata
            {
                Priority = 1000,
                Name = "EventBus",
                Version = "1.0.0"
            });

            return registry;
        });

        // Host abstraction
        services.AddSingleton<IPluginHost>(sp =>
        {
            var profile = configuration["PluginSystem:Profile"] ?? "dotnet.console";
            return new PluginHost(profile, sp);
        });

        // Plugin loader
        services.AddSingleton<PluginLoader>();

        // Profile-specific loader abstraction (default .NET implementation)
        services.AddSingleton<IPluginProfileLoader>(sp =>
        {
            var loader = sp.GetRequiredService<PluginLoader>();
            var host = sp.GetRequiredService<IPluginHost>();
            return new DotNetPluginProfileLoader(loader, host.Profile);
        });

        return services;
    }
}
