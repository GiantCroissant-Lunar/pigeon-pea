using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Diagnostic.Services;
using ServiceMetadata = PigeonPea.Contracts.Plugin.ServiceMetadata;

namespace PigeonPea.Plugins.Diagnostic.Sentry;

/// <summary>
/// Plugin that exposes a Sentry-based diagnostic service implementation.
/// </summary>
public class DiagnosticPlugin : IPlugin
{
    private ILogger? _logger;
    private SentryDiagnosticService? _service;

    public string Id => "pigeon-pea.diagnostic.sentry";
    public string Name => "Sentry Diagnostic Plugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        // Configure options - in a real implementation, these would come from configuration
        var options = new SentryDiagnosticOptions
        {
            Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN") ?? "",
            Environment = Environment.GetEnvironmentVariable("SENTRY_ENVIRONMENT") ?? "development",
            Release = Environment.GetEnvironmentVariable("SENTRY_RELEASE") ?? "1.0.0",
            Debug = bool.Parse(Environment.GetEnvironmentVariable("SENTRY_DEBUG") ?? "false"),
            TracesSampleRate = double.Parse(Environment.GetEnvironmentVariable("SENTRY_TRACES_SAMPLE_RATE") ?? "1.0"),
            AutoSessionTracking = bool.Parse(Environment.GetEnvironmentVariable("SENTRY_AUTO_SESSION_TRACKING") ?? "true"),
            MaxBreadcrumbs = int.Parse(Environment.GetEnvironmentVariable("SENTRY_MAX_BREADCRUMBS") ?? "100"),
            MaxRecentErrors = int.Parse(Environment.GetEnvironmentVariable("SENTRY_MAX_RECENT_ERRORS") ?? "100"),
            MemoryWarningThreshold = long.Parse(Environment.GetEnvironmentVariable("SENTRY_MEMORY_WARNING_THRESHOLD") ?? "500000000"),
            CaptureWarningsAsEvents = bool.Parse(Environment.GetEnvironmentVariable("SENTRY_CAPTURE_WARNINGS") ?? "false")
        };

        _service = new SentryDiagnosticService(options);

        context.Registry.Register<PigeonPea.Contracts.Diagnostic.Services.IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 90,
                Name = "SentryDiagnosticService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("Sentry diagnostic service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Sentry diagnostic plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Sentry diagnostic plugin stopped");
        return Task.CompletedTask;
    }
}
