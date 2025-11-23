using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Recording.Services;
using ServiceMetadata = PigeonPea.Contracts.Plugin.ServiceMetadata;

namespace PigeonPea.Plugins.Recording.Asciinema;

/// <summary>
/// Plugin that provides asciinema recording capabilities for Terminal.Gui TUI applications.
/// Supports dual-strategy approach: native asciinema binary (Linux/macOS) and pure C# fallback (Windows/universal).
/// </summary>
public class RecordingPlugin : IPlugin, IDisposable
{
    private ILogger? _logger;
    private AsciinemaRecordingService? _recordingService;

    public string Id => "pigeon-pea.recording.asciinema";
    public string Name => "Asciinema Recording Plugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        // Create asciinema recording service with proper logger type
        var recordingServiceLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AsciinemaRecordingService>.Instance;
        _recordingService = new AsciinemaRecordingService(recordingServiceLogger);

        // Register visual recorder service
        context.Registry.Register<IVisualRecorder>(
            _recordingService,
            new ServiceMetadata
            {
                Priority = 90, // Slightly lower than events plugin to allow both to coexist
                Name = "AsciinemaVisualRecorder",
                Version = Version,
                PluginId = Id
            });

        // Log strategy information
        var strategyInfo = _recordingService.GetStrategyInfo();
        var binaryAvailable = AsciinemaRecordingService.IsAsciinemaBinaryAvailable();
        
        _logger.LogInformation("Asciinema recording service registered with strategy: {Strategy}", strategyInfo);
        _logger.LogInformation("Asciinema binary available: {BinaryAvailable}", binaryAvailable);

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Asciinema recording plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Asciinema recording plugin stopping");
        
        // Ensure recording is stopped when plugin stops
        if (_recordingService != null && ((IVisualRecorder)_recordingService).IsRecording)
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

        _logger?.LogInformation("Asciinema recording plugin stopped");
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
            _logger?.LogError(ex, "Error disposing asciinema recording plugin");
        }
    }
}
