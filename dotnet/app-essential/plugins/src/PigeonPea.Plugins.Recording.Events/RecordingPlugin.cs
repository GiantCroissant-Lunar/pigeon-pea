using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Recording.Services;
using ServiceMetadata = PigeonPea.Contracts.Plugin.ServiceMetadata;

namespace PigeonPea.Plugins.Recording.Events;

/// <summary>
/// Plugin that provides event recording capabilities for deterministic game logic replay.
/// </summary>
public class RecordingPlugin : IPlugin
{
    private ILogger? _logger;
    private RecordingService? _recordingService;
    private EventRecordingService? _eventRecorder;

    public string Id => "pigeon-pea.recording.events";
    public string Name => "Event Recording Plugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        // Create core services with proper logger types
        var eventRecorderLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EventRecordingService>.Instance;
        var recordingServiceLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RecordingService>.Instance;
        
        _eventRecorder = new EventRecordingService(eventRecorderLogger);
        _recordingService = new RecordingService(recordingServiceLogger, _eventRecorder);

        // Register unified recording service
        context.Registry.Register<PigeonPea.Contracts.Recording.Services.IService>(
            _recordingService,
            new ServiceMetadata
            {
                Priority = 100,
                Name = "EventRecordingService",
                Version = Version,
                PluginId = Id
            });

        // Register event recorder service separately for direct access
        context.Registry.Register<PigeonPea.Contracts.Recording.Services.IEventRecorder>(
            _recordingService,
            new ServiceMetadata
            {
                Priority = 100,
                Name = "EventRecorder",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("Event recording services registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Event recording plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Event recording plugin stopped");
        return Task.CompletedTask;
    }
}
