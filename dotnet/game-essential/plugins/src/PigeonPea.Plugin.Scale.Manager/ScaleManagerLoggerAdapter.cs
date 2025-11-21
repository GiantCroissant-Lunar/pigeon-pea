using System;
using Microsoft.Extensions.Logging;

namespace PigeonPea.Plugin.Scale.Manager;

internal sealed class ScaleManagerLoggerAdapter : ILogger<ScaleManager>
{
    private readonly ILogger _logger;

    public ScaleManagerLoggerAdapter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
