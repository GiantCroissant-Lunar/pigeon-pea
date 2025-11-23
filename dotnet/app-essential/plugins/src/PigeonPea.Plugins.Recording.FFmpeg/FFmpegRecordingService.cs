using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Recording.Services;
using PigeonPea.Contracts.Recording.Models;
using PigeonPea.Plugins.Recording.FFmpeg.Configuration;
using PigeonPea.Plugins.Recording.FFmpeg.PlatformCapture;
using PigeonPea.Plugins.Recording.FFmpeg.Exporters;

namespace PigeonPea.Plugins.Recording.FFmpeg;

/// <summary>
/// FFmpeg-based visual recording service for GUI applications.
/// </summary>
public class FFmpegRecordingService : IVisualRecorder, IService, IDisposable
{
    private readonly ILogger<FFmpegRecordingService> _logger;
    private readonly FFmpegRecordingOptions _options;
    private readonly ICaptureStrategy _strategy;
    private Process? _ffmpegProcess;
    private bool _isRecording;
    private readonly object _lockObject = new object();
    private string? _currentSessionId;
    private readonly Dictionary<string, string> _sessions = new();

    public bool IsRecording
    {
        get
        {
            lock (_lockObject)
            {
                return _isRecording && _ffmpegProcess != null && !_ffmpegProcess.HasExited;
            }
        }
    }

    public FFmpegRecordingService(ILogger<FFmpegRecordingService> logger, FFmpegRecordingOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? FFmpegRecordingOptions.GetPlatformDefaults();

        if (!_options.IsValid())
        {
            throw new ArgumentException("Invalid FFmpeg recording options", nameof(options));
        }

        _strategy = SelectCaptureStrategy();

        _logger.LogInformation("FFmpeg recording service initialized with strategy: {Strategy}", _strategy.GetCaptureMethodName());
        _logger.LogDebug("Recording options: {Options}", _options.GetDescription());
    }

    public async Task StartAsync(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

        lock (_lockObject)
        {
            if (_isRecording)
            {
                _logger.LogWarning("Recording is already in progress");
                throw new InvalidOperationException("Recording is already in progress");
            }

            if (!IsFFmpegAvailable())
            {
                _logger.LogError("FFmpeg is not available on this system");
                throw new InvalidOperationException("FFmpeg is not available. Please install FFmpeg and ensure it's in your PATH.");
            }

            try
            {
                var args = _strategy.BuildFFmpegArgs(outputPath);
                _logger.LogInformation("Starting FFmpeg recording to {Path} with args: {Args}", outputPath, args);

                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = !_options.ShowFFmpegOutput,
                    RedirectStandardError = !_options.ShowFFmpegOutput,
                    CreateNoWindow = !_options.ShowFFmpegOutput
                };

                _ffmpegProcess = Process.Start(psi);

                if (_ffmpegProcess == null)
                {
                    throw new InvalidOperationException("Failed to start FFmpeg process");
                }

                _isRecording = true;

                // Log FFmpeg output if enabled
                if (_options.ShowFFmpegOutput)
                {
                    _ = Task.Run(() => LogFFmpegOutput());
                }

                _logger.LogInformation("FFmpeg recording started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start FFmpeg recording");
                Cleanup();
                throw;
            }
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        Process? processToStop = null;

        lock (_lockObject)
        {
            if (!_isRecording || _ffmpegProcess == null)
            {
                _logger.LogWarning("No recording is in progress");
                return;
            }

            processToStop = _ffmpegProcess;
        }

        try
        {
            _logger.LogInformation("Stopping FFmpeg recording");

            // Send 'q' to gracefully stop FFmpeg
            if (!processToStop.HasExited)
            {
                try
                {
                    await processToStop.StandardInput.WriteLineAsync("q");
                    await processToStop.StandardInput.FlushAsync();

                    // Wait for graceful shutdown
                    if (!processToStop.WaitForExit(5000))
                    {
                        _logger.LogWarning("FFmpeg did not exit gracefully, forcing termination");
                        processToStop.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during graceful FFmpeg shutdown, forcing termination");
                    processToStop.Kill(entireProcessTree: true);
                }
            }

            await processToStop.WaitForExitAsync();

            var exitCode = processToStop.ExitCode;
            if (exitCode == 0)
            {
                _logger.LogInformation("FFmpeg recording stopped successfully");
            }
            else
            {
                _logger.LogWarning("FFmpeg exited with code: {ExitCode}", exitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping FFmpeg recording");
        }
        finally
        {
            lock (_lockObject)
            {
                Cleanup();
            }
        }
    }

    /// <summary>
    /// Gets information about the current capture strategy.
    /// </summary>
    /// <returns>Capture strategy information.</returns>
    public string GetStrategyInfo()
    {
        return _strategy.GetCaptureMethodName();
    }

    /// <summary>
    /// Gets the requirements for the current capture strategy.
    /// </summary>
    /// <returns>List of requirements.</returns>
    public IEnumerable<string> GetRequirements()
    {
        return _strategy.GetRequirements();
    }

    /// <summary>
    /// Checks if FFmpeg is available on the system.
    /// </summary>
    /// <returns>True if FFmpeg is available, false otherwise.</returns>
    public static bool IsFFmpegAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit();
                return process.ExitCode == 0;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets FFmpeg version information.
    /// </summary>
    /// <returns>FFmpeg version string or null if not available.</returns>
    public static string? GetFFmpegVersion()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    var lines = output.Split('\n');
                    var versionLine = lines.FirstOrDefault(l => l.Contains("ffmpeg version"));
                    return versionLine?.Trim();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private ICaptureStrategy SelectCaptureStrategy()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsCaptureStrategy(_options);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxCaptureStrategy(_options);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacCaptureStrategy(_options);
        }
        else
        {
            throw new PlatformNotSupportedException($"Video recording is not supported on this platform: {RuntimeInformation.OSDescription}");
        }
    }

    private async Task LogFFmpegOutput()
    {
        if (_ffmpegProcess == null) return;

        try
        {
            // Only log if streams are redirected
            if (_options.ShowFFmpegOutput &&
                _ffmpegProcess.StartInfo.RedirectStandardOutput &&
                _ffmpegProcess.StartInfo.RedirectStandardError)
            {
                var stdoutTask = _ffmpegProcess.StandardOutput.ReadToEndAsync();
                var stderrTask = _ffmpegProcess.StandardError.ReadToEndAsync();

                await Task.WhenAll(stdoutTask, stderrTask);

                var stdout = await stdoutTask;
                var stderr = await stderrTask;

                if (!string.IsNullOrEmpty(stdout))
                {
                    _logger.LogInformation("FFmpeg stdout: {Output}", stdout);
                }

                if (!string.IsNullOrEmpty(stderr))
                {
                    if (stderr.Contains("error", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError("FFmpeg stderr: {Output}", stderr);
                    }
                    else
                    {
                        _logger.LogWarning("FFmpeg stderr: {Output}", stderr);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging FFmpeg output");
        }
    }

    private void Cleanup()
    {
        _isRecording = false;
        _currentSessionId = null;

        try
        {
            _ffmpegProcess?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing FFmpeg process");
        }

        _ffmpegProcess = null;
    }

    // IService Interface Implementation

    public async Task<string> StartRecordingAsync(RecordingType type, RecordingOptions options, CancellationToken ct = default)
    {
        if (type != RecordingType.Visual)
        {
            throw new NotSupportedException($"FFmpeg recording service only supports visual recordings, not {type}");
        }

        var sessionId = Guid.NewGuid().ToString("N")[..8];
        var outputPath = options.OutputPath ?? $"recording_{sessionId}.mp4";

        lock (_lockObject)
        {
            if (_isRecording)
            {
                throw new InvalidOperationException("Recording is already in progress");
            }

            _currentSessionId = sessionId;
            _sessions[sessionId] = outputPath;
        }

        try
        {
            await StartAsync(outputPath);
            _logger.LogInformation("Started recording session {SessionId} to {OutputPath}", sessionId, outputPath);
            return sessionId;
        }
        catch
        {
            lock (_lockObject)
            {
                _sessions.Remove(sessionId);
                _currentSessionId = null;
            }
            throw;
        }
    }

    public async Task StopRecordingAsync(string sessionId, CancellationToken ct = default)
    {
        lock (_lockObject)
        {
            if (string.IsNullOrEmpty(sessionId) || !_sessions.ContainsKey(sessionId))
            {
                throw new ArgumentException($"Session {sessionId} not found", nameof(sessionId));
            }

            if (_currentSessionId != sessionId)
            {
                throw new InvalidOperationException($"Session {sessionId} is not the active recording session");
            }
        }

        try
        {
            await StopAsync();
            _logger.LogInformation("Stopped recording session {SessionId}", sessionId);
        }
        finally
        {
            lock (_lockObject)
            {
                _sessions.Remove(sessionId);
                _currentSessionId = null;
            }
        }
    }

    bool IService.IsRecording(string sessionId)
    {
        lock (_lockObject)
        {
            return _currentSessionId == sessionId && IsRecording;
        }
    }

    public IEnumerable<string> GetActiveSessions()
    {
        lock (_lockObject)
        {
            if (_currentSessionId != null && IsRecording)
            {
                return new[] { _currentSessionId };
            }
            return Array.Empty<string>();
        }
    }

    public Task<RecordingMetadata> LoadRecordingAsync(string path, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("Recording file not found", path);

        try
        {
            var fileInfo = new FileInfo(path);
            var metadata = new RecordingMetadata
            {
                FilePath = path,
                CreatedAt = fileInfo.CreationTime,
                Type = RecordingType.Visual,
                Format = DetermineFormat(path),
                Metadata = new Dictionary<string, object>
                {
                    ["FileSize"] = fileInfo.Length,
                    ["LastModified"] = fileInfo.LastWriteTime
                }
            };

            return Task.FromResult(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recording metadata for {Path}", path);
            throw;
        }
    }

    public Task PlayRecordingAsync(string path, PlaybackOptions options, CancellationToken ct = default)
    {
        throw new NotSupportedException("FFmpeg recording service does not support playback. Use a video player to play the recorded file.");
    }

    public async Task ExportAsync(string sessionId, string outputPath, RecordingFormat format, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

        string sourcePath;
        lock (_lockObject)
        {
            if (!_sessions.TryGetValue(sessionId, out sourcePath))
            {
                throw new ArgumentException($"Session {sessionId} not found", nameof(sessionId));
            }
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Source recording file not found", sourcePath);
        }

        try
        {
            var videoExporterLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<VideoExporter>.Instance;
            var exporter = new VideoExporter(videoExporterLogger);
            await exporter.ConvertFormatAsync(sourcePath, outputPath, format, null, ct);
            _logger.LogInformation("Exported session {SessionId} to {OutputPath} as {Format}", sessionId, outputPath, format);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export session {SessionId} to {OutputPath}", sessionId, outputPath);
            throw;
        }
    }

    private static RecordingFormat DetermineFormat(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => RecordingFormat.Mp4,
            ".webm" => RecordingFormat.Webm,
            _ => RecordingFormat.Mp4 // Default to MP4 for unknown formats
        };
    }

    public void Dispose()
    {
        if (IsRecording)
        {
            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping recording during disposal");
            }
        }

        Cleanup();
    }
}
