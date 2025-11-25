using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Recording.Contracts;

namespace PigeonPea.Plugin.Logging.Recording;

/// <summary>
/// Extension methods for registering the recording logger sink.
/// </summary>
public static class RecordingLoggerExtensions
{
    /// <summary>
    /// Adds the recording logger sink to the logging builder.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="recorder">The event recorder service.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddRecordingSink(
        this ILoggingBuilder builder,
        IEventRecorder recorder,
        Action<RecordingLoggerOptions>? configureOptions = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (recorder == null)
            throw new ArgumentNullException(nameof(recorder));

        var options = new RecordingLoggerOptions();
        configureOptions?.Invoke(options);

        builder.Services.AddSingleton<IEventRecorder>(recorder);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<ILoggerProvider, RecordingLoggerProvider>();

        return builder;
    }

    /// <summary>
    /// Adds the recording logger sink to the logging builder using DI.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddRecordingSink(
        this ILoggingBuilder builder,
        Action<RecordingLoggerOptions>? configureOptions = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        var options = new RecordingLoggerOptions();
        configureOptions?.Invoke(options);

        // Register options
        builder.Services.AddSingleton(options);

        // Register the provider with a factory that resolves IEventRecorder from DI
        builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
        {
            var recorder = serviceProvider.GetRequiredService<IEventRecorder>();
            return new RecordingLoggerProvider(recorder, options);
        });

        return builder;
    }
}
