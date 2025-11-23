using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Recording.Services;
using PigeonPea.Plugins.Recording.FFmpeg.Configuration;
using ServiceMetadata = PigeonPea.Contracts.Plugin.ServiceMetadata;

namespace PigeonPea.Plugins.Recording.FFmpeg;

/// <summary>
/// Plugin that provides FFmpeg video recording capabilities for Avalonia GUI applications.
/// Supports cross-platform screen capture with platform-specific optimizations.
/// </summary>
public class RecordingPlugin : IPlugin
{
    private ILogger? _logger;
    private FFmpegRecordingService? _recordingService;

    public string Id => "pigeon-pea.recording.ffmpeg";
    public string Name => "FFmpeg Recording Plugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        // Check FFmpeg availability
        var ffmpegAvailable = FFmpegRecordingService.IsFFmpegAvailable();
        if (!ffmpegAvailable)
        {
            _logger.LogWarning("FFmpeg is not available on this system. Plugin functionality will be limited.");

            var ffmpegVersion = FFmpegRecordingService.GetFFmpegVersion();
            if (ffmpegVersion != null)
            {
                _logger.LogInformation("FFmpeg version: {Version}", ffmpegVersion);
            }
        }
        else
        {
            var ffmpegVersion = FFmpegRecordingService.GetFFmpegVersion();
            _logger.LogInformation("FFmpeg detected: {Version}", ffmpegVersion ?? "Unknown version");
        }

        // Create FFmpeg recording service with platform defaults
        var options = FFmpegRecordingOptions.GetPlatformDefaults();
        var recordingServiceLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<FFmpegRecordingService>.Instance;
        _recordingService = new FFmpegRecordingService(recordingServiceLogger, options);

        // Register visual recorder service
        context.Registry.Register<IVisualRecorder>(
            _recordingService,
            new ServiceMetadata
            {
                Priority = 95, // Higher priority than asciinema for GUI apps
                Name = "FFmpegVisualRecorder",
                Version = Version,
                PluginId = Id
            });

        // Log strategy information
        var strategyInfo = _recordingService.GetStrategyInfo();
        var requirements = _recordingService.GetRequirements();

        _logger.LogInformation("FFmpeg recording service registered with strategy: {Strategy}", strategyInfo);
        _logger.LogInformation("Requirements: {Requirements}", string.Join(", ", requirements));

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("FFmpeg recording plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("FFmpeg recording plugin stopping");

        // Ensure recording is stopped when plugin stops
        if (_recordingService?.IsRecording == true)
        {
            try
            {
                _logger?.LogWarning("Plugin stopping while recording is active - stopping recording");
                _recordingService.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping recording during plugin shutdown");
            }
        }

        _logger?.LogInformation("FFmpeg recording plugin stopped");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            _recordingService?.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error disposing FFmpeg recording plugin");
        }
    }
}
