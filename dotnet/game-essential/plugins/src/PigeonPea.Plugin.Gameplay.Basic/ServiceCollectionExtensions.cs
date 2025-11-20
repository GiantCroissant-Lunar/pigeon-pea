using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Input.Services;
using PigeonPea.Game.Contracts.Services;
using PigeonPea.Plugin.Gameplay.Basic.Systems;

namespace PigeonPea.Plugin.Gameplay.Basic;

/// <summary>
/// Extension methods for IServiceCollection to register basic gameplay services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the basic gameplay plugin services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGameplayBasic(this IServiceCollection services)
    {
        // Register systems
        services.AddSingleton<PlayerInputSystem>();
        services.AddSingleton<MovementSystem>();

        // Register the main gameplay loop
        services.AddSingleton<IGameplayLoop, GameplayLoop>();

        return services;
    }
}
