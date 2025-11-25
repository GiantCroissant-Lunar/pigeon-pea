using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PigeonPea.Plugin.Recording.Asciinema.Strategies;

/// <summary>
/// Stub implementation - TerminalBufferRecorder functionality disabled to focus on native binary strategy.
/// This fallback strategy requires complex terminal emulation APIs that are not readily available.
/// </summary>
public sealed class TerminalBufferRecorder : IRecordingStrategy
{
    private readonly ILogger _logger;
    private bool _isRecording;

    public bool IsRecording => _isRecording;

    public TerminalBufferRecorder(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(string outputPath)
    {
        throw new NotSupportedException(
            "TerminalBufferRecorder is not implemented. Please install the asciinema binary for recording functionality. " +
            "Visit https://asciinema.org/ for installation instructions.");
    }

    public Task StopAsync()
    {
        if (!_isRecording)
        {
            _logger.LogWarning("Terminal buffer recorder is not recording");
            return Task.CompletedTask;
        }

        _isRecording = false;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _isRecording = false;
    }
}
