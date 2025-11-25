using Microsoft.Extensions.Logging;
using PigeonPea.Diagnostic.Contracts;
using System.Collections.Concurrent;

namespace PigeonPea.Plugin.Logging.Diagnostic;

/// <summary>
/// Logger provider for the diagnostic service.
/// </summary>
[ProviderAlias("Diagnostic")]
public class DiagnosticLoggerProvider : ILoggerProvider
{
    private readonly IDiagnosticService _diagnostic;
    private readonly DiagnosticLoggerOptions _options;
    private readonly ConcurrentDictionary<string, DiagnosticLogger> _loggers = new();

    /// <summary>
    /// Initializes a new instance of the DiagnosticLoggerProvider class.
    /// </summary>
    /// <param name="diagnostic">The diagnostic service.</param>
    /// <param name="options">The configuration options.</param>
    public DiagnosticLoggerProvider(IDiagnosticService diagnostic, DiagnosticLoggerOptions? options = null)
    {
        _diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
        _options = options ?? new DiagnosticLoggerOptions();
    }

    /// <summary>
    /// Creates a new ILogger instance.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>A new DiagnosticLogger instance.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new DiagnosticLogger(_diagnostic, name, _options));
    }

    /// <summary>
    /// Disposes the provider and all created loggers.
    /// </summary>
    public void Dispose()
    {
        foreach (var logger in _loggers.Values)
        {
            if (logger is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _loggers.Clear();
    }
}
