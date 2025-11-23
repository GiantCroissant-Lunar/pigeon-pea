using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Diagnostic.Services;

namespace PigeonPea.Plugins.Logging.Diagnostic;

/// <summary>
/// Extension methods for registering diagnostic logger provider.
/// </summary>
public static class DiagnosticLoggerExtensions
{
    /// <summary>
    /// Adds a diagnostic logger provider to the logging builder.
    /// </summary>
    /// <param name="builder">The logging builder to configure.</param>
    /// <param name="diagnostic">The diagnostic service instance.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddDiagnostic(
        this ILoggingBuilder builder,
        IDiagnosticService diagnostic,
        DiagnosticLoggerOptions? options = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (diagnostic == null)
            throw new ArgumentNullException(nameof(diagnostic));

        var loggerOptions = options ?? new DiagnosticLoggerOptions();
        builder.AddProvider(new DiagnosticLoggerProvider(diagnostic, loggerOptions));

        return builder;
    }

    /// <summary>
    /// Adds a diagnostic logger provider to the logging builder using dependency injection.
    /// </summary>
    /// <param name="builder">The logging builder to configure.</param>
    /// <param name="configureOptions">Optional action to configure options.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddDiagnostic(
        this ILoggingBuilder builder,
        Action<DiagnosticLoggerOptions>? configureOptions = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        // Register the diagnostic service if not already registered
        builder.Services.AddScoped<IDiagnosticService, DiagnosticService>();

        // Configure options if provided
        if (configureOptions != null)
        {
            builder.Services.Configure(configureOptions);
        }
        else
        {
            builder.Services.Configure<DiagnosticLoggerOptions>(options => { });
        }

        // Register the logger provider
        builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
        {
            var diagnostic = serviceProvider.GetRequiredService<IDiagnosticService>();
            var options = serviceProvider.GetService<DiagnosticLoggerOptions>() ?? new DiagnosticLoggerOptions();
            return new DiagnosticLoggerProvider(diagnostic, options);
        });

        return builder;
    }
}

/// <summary>
/// Default implementation of IDiagnosticService for testing and basic scenarios.
/// </summary>
internal class DiagnosticService : IDiagnosticService
{
    private bool _isEnabled = true;
    private readonly List<IDiagnosticSession> _activeSessions = new();
    private readonly DiagnosticStatus _status = new();

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public void ReportError(Exception error, Dictionary<string, object>? context = null, DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        _status.ErrorCount++;
        _status.LastErrorTimestamp = DateTimeOffset.UtcNow;

        if (severity == DiagnosticSeverity.Critical)
        {
            _status.IsHealthy = false;
            _status.HealthScore = Math.Max(0, _status.HealthScore - 10);
        }
        else
        {
            _status.HealthScore = Math.Max(0, _status.HealthScore - 2);
        }

        // In a real implementation, this would send to error tracking service
        Console.WriteLine($"[DIAGNOSTIC] Error reported: {error.Message} (Severity: {severity})");
    }

    public void ReportWarning(string message, Dictionary<string, object>? context = null)
    {
        _status.WarningCount++;
        _status.HealthScore = Math.Max(0, _status.HealthScore - 0.5);

        // In a real implementation, this would send to warning tracking service
        Console.WriteLine($"[DIAGNOSTIC] Warning reported: {message}");
    }

    public void ReportInfo(string message, Dictionary<string, object>? context = null)
    {
        // In a real implementation, this might send to info tracking service
        Console.WriteLine($"[DIAGNOSTIC] Info reported: {message}");
    }

    public void ReportHealthMetric(string metricName, double value, string? unit = null)
    {
        _status.HealthMetrics[metricName] = value;

        // In a real implementation, this would send to health monitoring service
        Console.WriteLine($"[DIAGNOSTIC] Health metric: {metricName} = {value} {unit}");
    }

    public IDiagnosticSession CreateSession(string sessionName, Dictionary<string, object>? context = null)
    {
        var session = new DiagnosticSession(sessionName, context);
        _activeSessions.Add(session);
        _status.ActiveSessionCount = _activeSessions.Count;

        return session;
    }

    public DiagnosticStatus GetStatus()
    {
        _status.ActiveSessionCount = _activeSessions.Count;

        // Calculate overall health based on error/warning ratio
        var totalEvents = _status.ErrorCount + _status.WarningCount;
        if (totalEvents > 0)
        {
            var errorRatio = (double)_status.ErrorCount / totalEvents;
            _status.HealthScore = Math.Max(0, 100 - (errorRatio * 100));
        }

        _status.IsHealthy = _status.HealthScore >= 70 && _status.ErrorCount == 0;

        return _status;
    }

    public Task FlushAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would flush pending diagnostic data
        Console.WriteLine("[DIAGNOSTIC] Flushing diagnostic data...");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Default implementation of IDiagnosticSession for testing and basic scenarios.
/// </summary>
internal class DiagnosticSession : IDiagnosticSession
{
    private readonly Dictionary<string, object> _context = new();

    public DiagnosticSession(string sessionName, Dictionary<string, object>? context)
    {
        SessionId = Guid.NewGuid().ToString("N")[..8];
        SessionName = sessionName;
        StartTime = DateTimeOffset.UtcNow;

        if (context != null)
        {
            foreach (var kvp in context)
            {
                _context[kvp.Key] = kvp.Value;
            }
        }
    }

    public string SessionId { get; }
    public string SessionName { get; }
    public DateTimeOffset StartTime { get; }

    public void AddContext(string key, object value)
    {
        _context[key] = value;
    }

    public void ReportError(Exception error, Dictionary<string, object>? additionalContext = null)
    {
        var context = new Dictionary<string, object>(_context);
        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        Console.WriteLine($"[DIAGNOSTIC SESSION {SessionId}] Error: {error.Message}");
    }

    public void ReportWarning(string message, Dictionary<string, object>? additionalContext = null)
    {
        var context = new Dictionary<string, object>(_context);
        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        Console.WriteLine($"[DIAGNOSTIC SESSION {SessionId}] Warning: {message}");
    }

    public void Dispose()
    {
        var duration = DateTimeOffset.UtcNow - StartTime;
        Console.WriteLine($"[DIAGNOSTIC SESSION {SessionId}] Ended after {duration.TotalSeconds:F2}s");
    }
}
