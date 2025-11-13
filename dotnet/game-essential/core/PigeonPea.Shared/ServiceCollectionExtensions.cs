using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Shared.ViewModels;

namespace PigeonPea.Shared;

/// <summary>
/// Extension methods for configuring dependency injection services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MessagePipe and related services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPigeonPeaServices(this IServiceCollection services)
    {
        // Add MessagePipe for event-driven communication
        services.AddMessagePipe();

        // Add view models as transient services
        services.AddTransient<PlayerViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<MessageLogViewModel>();
        services.AddTransient<MapViewModel>();

        return services;
    }
}
