using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Recording.Models;
using PigeonPea.Contracts.Recording.Services;
using PigeonPea.Plugins.Recording.Asciinema.Strategies;
using PigeonPea.Plugins.Recording.Asciinema.Exporters;

namespace PigeonPea.Plugins.Recording.Asciinema;

/// <summary>
/// Main asciinema recording service that implements IVisualRecorder and uses dual-strategy approach.
/// Automatically selects between asciinema binary and pure C# fallback based on availability.
/// </summary>
public sealed class AsciinemaRecordingService : IVisualRecorder, IService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IRecordingStrategy _strategy;
    private readonly Dictionary<string, string> _sessions;
    private bool _disposed;

    private bool IsRecordingStrategy => _strategy.IsRecording;

    /// <summary>
    /// Gets whether the visual recorder is currently recording (IVisualRecorder interface implementation).
    /// </summary>
    bool IVisualRecorder.IsRecording => IsRecordingStrategy;

    public AsciinemaRecordingService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strategy = SelectStrategy();
        _sessions = new Dictionary<string, string>();
    }

    /// <summary>
    /// Selects the appropriate recording strategy based on system capabilities.
    /// </summary>
    /// <returns>The selected recording strategy.</returns>
    private IRecordingStrategy SelectStrategy()
    {
        // Try to use asciinema binary first
        if (AsciinemaBinaryRecorder.IsAvailable())
        {
            _logger.LogInformation("Using asciinema binary for recording");
            return new AsciinemaBinaryRecorder(_logger);
        }

        // Fallback to pure C# implementation
        _logger.LogInformation("Asciinema binary not found, using pure C# fallback");
        return new TerminalBufferRecorder(_logger);
    }

    /// <summary>
    /// Starts visual recording to the specified output path.
    /// </summary>
    /// <param name="outputPath">Destination path for the visual recording.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync(string outputPath)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AsciinemaRecordingService));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
        }

        if (IsRecordingStrategy)
        {
            _logger.LogWarning("Asciinema recording is already in progress");
            return;
        }

        try
        {
            _logger.LogInformation("Starting asciinema recording to {OutputPath}", outputPath);
            await _strategy.StartAsync(outputPath);
            _logger.LogInformation("Asciinema recording started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start asciinema recording");
            throw;
        }
    }

    /// <summary>
    /// Stops the current visual recording session.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AsciinemaRecordingService));
        }

        if (!IsRecordingStrategy)
        {
            _logger.LogWarning("No asciinema recording is in progress");
            return;
        }

        try
        {
            _logger.LogInformation("Stopping asciinema recording");
            await _strategy.StopAsync();
            _logger.LogInformation("Asciinema recording stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop asciinema recording");
            throw;
        }
    }

    /// <summary>
    /// Gets information about the current recording strategy.
    /// </summary>
    /// <returns>Information about the active recording strategy.</returns>
    public string GetStrategyInfo()
    {
        return _strategy switch
        {
            AsciinemaBinaryRecorder => "Asciinema Binary (native)",
            TerminalBufferRecorder => "Terminal Buffer (pure C# fallback)",
            _ => "Unknown strategy"
        };
    }

    /// <summary>
    /// Checks if the asciinema binary is available on this system.
    /// </summary>
    /// <returns>True if asciinema binary is available, false otherwise.</returns>
    public static bool IsAsciinemaBinaryAvailable()
    {
        return AsciinemaBinaryRecorder.IsAvailable();
    }

    // IService interface implementation

    /// <summary>
    /// Starts a new recording session of the specified type.
    /// </summary>
    /// <param name="type">Type of recording to start.</param>
    /// <param name="options">Configuration options for recording.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Unique session identifier.</returns>
    public async Task<string> StartRecordingAsync(RecordingType type, RecordingOptions options, CancellationToken ct = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsciinemaRecordingService));

        var sessionId = Guid.NewGuid().ToString();
        var outputPath = options.OutputPath;

        _sessions[sessionId] = outputPath;

        await StartAsync(outputPath);
        return sessionId;
    }

    /// <summary>
    /// Stops an active recording session.
    /// </summary>
    /// <param name="sessionId">Session identifier to stop.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task StopRecordingAsync(string sessionId, CancellationToken ct = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsciinemaRecordingService));

        if (_sessions.ContainsKey(sessionId))
        {
            await StopAsync();
            _sessions.Remove(sessionId);
        }
    }

    /// <summary>
    /// Checks if a session is currently active and recording.
    /// </summary>
    /// <param name="sessionId">Session identifier to check.</param>
    /// <returns>True if session is active and recording.</returns>
    public bool IsSessionRecording(string sessionId)
    {
        if (_disposed)
            return false;

        return _sessions.ContainsKey(sessionId) && _strategy.IsRecording;
    }

    /// <summary>
    /// Checks if a session is currently active and recording (IService interface implementation).
    /// </summary>
    /// <param name="sessionId">Session identifier to check.</param>
    /// <returns>True if session is active and recording.</returns>
    public bool IsRecording(string sessionId)
    {
        return IsSessionRecording(sessionId);
    }


    /// <summary>
    /// Gets all currently active recording sessions.
    /// </summary>
    /// <returns>Collection of active session identifiers.</returns>
    public IEnumerable<string> GetActiveSessions()
    {
        if (_disposed)
            return Enumerable.Empty<string>();

        return _sessions.Keys.ToList();
    }

    /// <summary>
    /// Loads metadata about a recording file.
    /// </summary>
    /// <param name="path">Path to recording file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Metadata about the recording.</returns>
    public async Task<RecordingMetadata> LoadRecordingAsync(string path, CancellationToken ct = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsciinemaRecordingService));

        var exporter = new AsciinemaExporter(_logger);
        var asciinemaMetadata = await exporter.GetMetadataAsync(path);
        
        if (asciinemaMetadata == null)
            throw new FileNotFoundException($"Could not load metadata for recording file: {path}");

        return new RecordingMetadata
        {
            FilePath = path,
            Type = RecordingType.Visual,
            Format = RecordingFormat.Asciinema,
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(asciinemaMetadata.Timestamp).DateTime,
            Duration = TimeSpan.FromSeconds(asciinemaMetadata.DurationSeconds),
            EventCount = asciinemaMetadata.FrameCount,
            Metadata = new Dictionary<string, object>
            {
                ["Version"] = asciinemaMetadata.Version,
                ["Width"] = asciinemaMetadata.Width,
                ["Height"] = asciinemaMetadata.Height,
                ["FrameCount"] = asciinemaMetadata.FrameCount,
                ["FileSizeBytes"] = asciinemaMetadata.FileSizeBytes
            }
        };
    }

    /// <summary>
    /// Plays back an event recording with the specified options.
    /// </summary>
    /// <param name="path">Path to event recording file.</param>
    /// <param name="options">Playback configuration options.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task PlayRecordingAsync(string path, PlaybackOptions options, CancellationToken ct = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsciinemaRecordingService));

        throw new NotSupportedException("Asciinema recording does not support event playback. Use an external asciinema player.");
    }

    /// <summary>
    /// Exports a recording session to the specified format.
    /// </summary>
    /// <param name="sessionId">Session identifier to export.</param>
    /// <param name="outputPath">Destination path for the exported file.</param>
    /// <param name="format">Target export format.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task ExportAsync(string sessionId, string outputPath, RecordingFormat format, CancellationToken ct = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsciinemaRecordingService));

        if (!_sessions.ContainsKey(sessionId))
            throw new ArgumentException($"Session {sessionId} not found", nameof(sessionId));

        if (format != RecordingFormat.Asciinema)
            throw new NotSupportedException($"Export format {format} not supported by asciinema recorder");

        var sourcePath = _sessions[sessionId];
        if (File.Exists(sourcePath))
        {
            File.Copy(sourcePath, outputPath, true);
        }
        else
        {
            throw new FileNotFoundException($"Source recording file not found: {sourcePath}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (IsRecordingStrategy)
            {
                _logger.LogWarning("Disposing while recording is active - stopping recording");
                StopAsync().GetAwaiter().GetResult();
            }

            if (_strategy is IDisposable disposableStrategy)
            {
                disposableStrategy.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal of asciinema recording service");
        }
        finally
        {
            _disposed = true;
        }
    }
}
