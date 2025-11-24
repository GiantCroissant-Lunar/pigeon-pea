using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Analytics.Contracts;

namespace PigeonPea.Plugins.Logging.Analytics;

/// <summary>
/// Extension methods for registering analytics logger sink.
/// </summary>
public static class AnalyticsLoggerExtensions
{
    /// <summary>
    /// Adds analytics logger sink to the logging builder.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="analytics">The analytics service.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddAnalyticsSink(
        this ILoggingBuilder builder,
        IAnalyticsService analytics,
        Action<AnalyticsLoggerOptions>? configureOptions = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (analytics == null)
            throw new ArgumentNullException(nameof(analytics));

        var options = new AnalyticsLoggerOptions();
        configureOptions?.Invoke(options);

        builder.Services.AddSingleton<IAnalyticsService>(analytics);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<ILoggerProvider, AnalyticsLoggerProvider>();

        return builder;
    }

    /// <summary>
    /// Adds analytics logger sink to the logging builder using DI.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddAnalyticsSink(
        this ILoggingBuilder builder,
        Action<AnalyticsLoggerOptions>? configureOptions = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        var options = new AnalyticsLoggerOptions();
        configureOptions?.Invoke(options);

        // Register options
        builder.Services.AddSingleton(options);

        // Register the provider with a factory that resolves IAnalyticsService from DI
        builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
        {
            var analytics = serviceProvider.GetRequiredService<IAnalyticsService>();
            return new AnalyticsLoggerProvider(analytics, options);
        });

        return builder;
    }
}
