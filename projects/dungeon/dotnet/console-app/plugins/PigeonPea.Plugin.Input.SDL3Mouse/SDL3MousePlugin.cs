using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Input.Core.Controls;

namespace PigeonPea.Plugin.Input.SDL3Mouse;

public sealed class SDL3MousePlugin : IPlugin
{
    private ILogger? _logger;
    private SDL3MouseDevice? _device;

    public string Id => "PigeonPea.Plugin.Input.SDL3Mouse";
    public string Name => "SDL3 Mouse Input Device";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger?.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        _device = new SDL3MouseDevice();

        context.Registry.Register<IInputDevice>(_device, new ServiceMetadata
        {
            Priority = 90,
            Name = "SDL3MouseDevice",
            Version = Version,
            PluginId = Id
        });

        _logger?.LogInformation("SDL3 mouse input device registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("SDL3Mouse plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("SDL3Mouse plugin stopped");
        _device?.Dispose();
        return Task.CompletedTask;
    }
}
