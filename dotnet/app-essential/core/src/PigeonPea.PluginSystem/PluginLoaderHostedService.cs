using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Hosted service that discovers and loads plugins on application startup.
/// </summary>
public class PluginLoaderHostedService : IHostedService
{
    private readonly ILogger<PluginLoaderHostedService> _logger;
    private readonly IPluginProfileLoader _profileLoader;

    public PluginLoaderHostedService(
        ILogger<PluginLoaderHostedService> logger,
        IPluginProfileLoader profileLoader)
    {
        _logger = logger;
        _profileLoader = profileLoader;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var profile = _profileLoader.Profile;
        _logger.LogInformation("Starting plugin discovery for profile {Profile}...", profile);
        var count = await _profileLoader.LoadAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Plugin discovery complete. Loaded {Count} plugins.", count);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // For MVP, no-op; individual plugins should stop via application lifecycle or explicit unload.
        return Task.CompletedTask;
    }
}
