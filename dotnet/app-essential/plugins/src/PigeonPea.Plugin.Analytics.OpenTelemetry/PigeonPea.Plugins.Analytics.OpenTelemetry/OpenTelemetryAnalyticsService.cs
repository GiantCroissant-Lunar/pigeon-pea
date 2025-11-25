using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PigeonPea.Analytics.Contracts;

namespace PigeonPea.Plugin.Analytics.OpenTelemetry;

/// <summary>
/// OpenTelemetry-based analytics service implementation.
/// </summary>
public class OpenTelemetryAnalyticsService : IService, IDisposable
{
    private static readonly ActivitySource ActivitySource =
        new("PigeonPea.Analytics", "1.0.0");

    private static readonly Meter Meter =
        new("PigeonPea.Analytics", "1.0.0");

    private readonly ConcurrentDictionary<string, Counter<long>> _counters = new();
    private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new();

    private TracerProvider? _tracerProvider;
    private MeterProvider? _meterProvider;
    private bool _isEnabled = true;
    private string? _userId;
    private readonly ConcurrentDictionary<string, object> _userProperties = new();

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryAnalyticsService.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    public OpenTelemetryAnalyticsService(OpenTelemetryAnalyticsOptions options)
    {
        ConfigureProviders(options);
    }

    private void ConfigureProviders(OpenTelemetryAnalyticsOptions options)
    {
        var tracerBuilder = Sdk.CreateTracerProviderBuilder()
            .AddSource("PigeonPea.Analytics")
            .SetSampler(new TraceIdRatioBasedSampler(options.SampleRate));

        var meterBuilder = Sdk.CreateMeterProviderBuilder()
            .AddMeter("PigeonPea.Analytics");

        if (options.UseConsoleExporter)
        {
            tracerBuilder.AddConsoleExporter();
            meterBuilder.AddConsoleExporter();
        }

        if (!string.IsNullOrEmpty(options.OtlpEndpoint))
        {
            tracerBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
            meterBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
        }

        if (!string.IsNullOrEmpty(options.PrometheusEndpoint))
        {
            meterBuilder.AddPrometheusHttpListener(o =>
                o.UriPrefixes = new[] { options.PrometheusEndpoint });
        }

        _tracerProvider = tracerBuilder.Build();
        _meterProvider = meterBuilder.Build();
    }

    /// <inheritdoc />
    public void TrackEvent(string eventName, IDictionary<string, object>? properties = null)
    {
        if (!_isEnabled) return;

        using var activity = ActivitySource.StartActivity(eventName, ActivityKind.Internal);
        if (activity == null) return;

        // Add user context
        if (_userId != null)
            activity.SetTag("user.id", _userId);

        foreach (var prop in _userProperties)
            activity.SetTag($"user.{prop.Key}", prop.Value?.ToString());

        // Add event properties
        if (properties != null)
        {
            foreach (var prop in properties)
                activity.SetTag(prop.Key, prop.Value?.ToString());
        }
    }

    /// <inheritdoc />
    public void TrackMetric(string metricName, double value, IDictionary<string, string>? dimensions = null)
    {
        if (!_isEnabled) return;

        var histogram = GetOrCreateHistogram(metricName);

        if (dimensions != null)
        {
            var tags = dimensions.Select(d => new KeyValuePair<string, object?>(d.Key, d.Value)).ToArray();
            histogram.Record(value, tags);
        }
        else
        {
            histogram.Record(value);
        }
    }

    /// <inheritdoc />
    public void IncrementCounter(string counterName, long value = 1)
    {
        if (!_isEnabled) return;

        var counter = GetOrCreateCounter(counterName);
        counter.Add(value);
    }

    /// <inheritdoc />
    public void TrackTiming(string operationName, TimeSpan duration, IDictionary<string, string>? dimensions = null)
    {
        TrackMetric($"{operationName}.duration_ms", duration.TotalMilliseconds, dimensions);
    }

    /// <inheritdoc />
    public ITimedOperation BeginTimedOperation(string operationName)
    {
        return new OpenTelemetryTimedOperation(ActivitySource, operationName, _isEnabled);
    }

    /// <inheritdoc />
    public void SetUserProperty(string propertyName, object value)
    {
        _userProperties[propertyName] = value;
    }

    /// <inheritdoc />
    public void SetUserId(string? userId)
    {
        _userId = userId;
    }

    /// <inheritdoc />
    public void Flush()
    {
        _tracerProvider?.ForceFlush();
        _meterProvider?.ForceFlush();
    }

    /// <inheritdoc />
    public bool IsEnabled => _isEnabled;

    /// <inheritdoc />
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
    }

    private Counter<long> GetOrCreateCounter(string name)
    {
        return _counters.GetOrAdd(name, Meter.CreateCounter<long>(name));
    }

    private Histogram<double> GetOrCreateHistogram(string name)
    {
        return _histograms.GetOrAdd(name, Meter.CreateHistogram<double>(name));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _tracerProvider?.Dispose();
        _meterProvider?.Dispose();
    }
}

/// <summary>
/// OpenTelemetry-based timed operation implementation.
/// </summary>
internal class OpenTelemetryTimedOperation : ITimedOperation
{
    private readonly Activity? _activity;
    private bool _failed;
    private Exception? _exception;

    public OpenTelemetryTimedOperation(ActivitySource source, string name, bool enabled)
    {
        if (enabled)
        {
            _activity = source.StartActivity(name, ActivityKind.Internal);
        }
    }

    /// <inheritdoc />
    public void AddProperty(string key, object value)
    {
        _activity?.SetTag(key, value?.ToString());
    }

    /// <inheritdoc />
    public void SetFailed(Exception? exception = null)
    {
        _failed = true;
        _exception = exception;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_activity == null) return;

        if (_failed)
        {
            _activity.SetStatus(ActivityStatusCode.Error, _exception?.Message);
            if (_exception != null)
            {
                _activity.RecordException(_exception);
            }
        }
        else
        {
            _activity.SetStatus(ActivityStatusCode.Ok);
        }

        _activity.Dispose();
    }
}
