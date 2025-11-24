using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Input.Contracts;
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

        services.AddSingleton<PigeonPea.Game.Contracts.Stats.Services.IService>(sp =>
        {
            var registry = sp.GetRequiredService<PigeonPea.Contracts.Plugin.IRegistry>();
            return new PigeonPea.Game.Contracts.Stats.Services.Proxy.Service(registry);
        });

        services.AddSingleton<PigeonPea.Game.Contracts.Combat.Services.IService>(sp =>
        {
            var registry = sp.GetRequiredService<PigeonPea.Contracts.Plugin.IRegistry>();
            return new PigeonPea.Game.Contracts.Combat.Services.Proxy.Service(registry);
        });

        // Register the main gameplay loop
        services.AddSingleton<IGameplayLoop, GameplayLoop>();

        return services;
    }
}
