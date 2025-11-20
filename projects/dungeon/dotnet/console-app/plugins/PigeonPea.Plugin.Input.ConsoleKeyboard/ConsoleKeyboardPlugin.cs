using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Input.Core.Controls;

namespace PigeonPea.Plugin.Input.ConsoleKeyboard;

public sealed class ConsoleKeyboardPlugin : IPlugin
{
    private ILogger? _logger;
    private ConsoleKeyboardDevice? _device;

    public string Id => "PigeonPea.Plugin.Input.ConsoleKeyboard";
    public string Name => "Console Keyboard Input Device";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger?.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        _device = new ConsoleKeyboardDevice();

        context.Registry.Register<IInputDevice>(_device, new ServiceMetadata
        {
            Priority = 100,
            Name = "ConsoleKeyboardDevice",
            Version = Version,
            PluginId = Id
        });

        _logger?.LogInformation("Console keyboard input device registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("ConsoleKeyboard plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("ConsoleKeyboard plugin stopped");
        return Task.CompletedTask;
    }
}
