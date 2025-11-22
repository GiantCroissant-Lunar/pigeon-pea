using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Profiling.Services;
using ServiceMetadata = PigeonPea.Contracts.Plugin.ServiceMetadata;

namespace PigeonPea.Plugins.Profiling.Sentry;

/// <summary>
/// Plugin that exposes a Sentry-based profiling service implementation.
/// </summary>
public class ProfilingPlugin : IPlugin
{
    private ILogger? _logger;
    private SentryProfilingService? _service;

    public string Id => "pigeon-pea.profiling.sentry";
    public string Name => "Sentry Profiling Plugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        // Configure options - in a real implementation, these would come from configuration
        var options = new SentryProfilingOptions
        {
            CreateTransactionsForOrphanScopes = bool.Parse(Environment.GetEnvironmentVariable("SENTRY_CREATE_TRANSACTIONS_FOR_ORPHAN_SCOPES") ?? "true"),
            TrackFramesAsTransactions = bool.Parse(Environment.GetEnvironmentVariable("SENTRY_TRACK_FRAMES_AS_TRANSACTIONS") ?? "false"),
            DefaultOperation = Environment.GetEnvironmentVariable("SENTRY_DEFAULT_OPERATION") ?? "profiling.scope",
            IncludeMarkersAsSpanEvents = bool.Parse(Environment.GetEnvironmentVariable("SENTRY_INCLUDE_MARKERS_AS_SPAN_EVENTS") ?? "true"),
            IncludeCountersAsSpanData = bool.Parse(Environment.GetEnvironmentVariable("SENTRY_INCLUDE_COUNTERS_AS_SPAN_DATA") ?? "true"),
            MaxScopeStats = int.Parse(Environment.GetEnvironmentVariable("SENTRY_MAX_SCOPE_STATS") ?? "1000"),
            FrameHistorySize = int.Parse(Environment.GetEnvironmentVariable("SENTRY_FRAME_HISTORY_SIZE") ?? "60")
        };

        _service = new SentryProfilingService(options);

        context.Registry.Register<PigeonPea.Contracts.Profiling.Services.IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 95,
                Name = "SentryProfilingService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("Sentry profiling service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Sentry profiling plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Sentry profiling plugin stopped");
        return Task.CompletedTask;
    }
}
