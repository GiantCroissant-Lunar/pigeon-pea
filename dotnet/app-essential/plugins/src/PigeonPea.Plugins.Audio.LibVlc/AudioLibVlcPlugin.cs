using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Audio.Services;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.Plugins.Audio.LibVlc;

public class AudioLibVlcPlugin : IPlugin
{
    private ILogger? _logger;
    private LibVlcAudioService? _service;

    public string Id => "audio-libvlc";
    public string Name => "LibVLCSharp Audio Service";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        _service = new LibVlcAudioService(_logger);

        context.Registry.Register<IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 200,
                Name = "LibVlcAudioService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("LibVLCSharp audio service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("AudioLibVlc plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("AudioLibVlc plugin stopped");
        _service?.Dispose();
        return Task.CompletedTask;
    }
}
