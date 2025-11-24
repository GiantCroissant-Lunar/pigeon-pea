using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Profiling.Contracts;

namespace PigeonPea.Plugins.Logging.Profiling;

/// <summary>
/// Extension methods for registering profiling logger sink.
/// </summary>
public static class ProfilingLoggerExtensions
{
    /// <summary>
    /// Adds profiling logger sink to the logging builder.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="profiling">The profiling service.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddProfilingSink(
        this ILoggingBuilder builder,
        IProfilingService profiling,
        Action<ProfilingLoggerOptions>? configureOptions = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (profiling == null)
            throw new ArgumentNullException(nameof(profiling));

        var options = new ProfilingLoggerOptions();
        configureOptions?.Invoke(options);

        builder.Services.AddSingleton<IProfilingService>(profiling);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<ILoggerProvider, ProfilingLoggerProvider>();

        return builder;
    }

    /// <summary>
    /// Adds profiling logger sink to the logging builder using DI.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddProfilingSink(
        this ILoggingBuilder builder,
        Action<ProfilingLoggerOptions>? configureOptions = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        var options = new ProfilingLoggerOptions();
        configureOptions?.Invoke(options);

        // Register options
        builder.Services.AddSingleton(options);

        // Register the provider with a factory that resolves IProfilingService from DI
        builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
        {
            var profiling = serviceProvider.GetRequiredService<IProfilingService>();
            return new ProfilingLoggerProvider(profiling, options);
        });

        return builder;
    }
}
