using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Hosted service that discovers and loads plugins on application startup.
/// </summary>
public class PluginLoaderHostedService : IHostedService
{
    private readonly ILogger<PluginLoaderHostedService> _logger;
    private readonly PluginLoader _loader;
    private readonly IConfiguration _configuration;
    private readonly Contracts.Plugin.IPluginHost _host;

    public PluginLoaderHostedService(
        ILogger<PluginLoaderHostedService> logger,
        PluginLoader loader,
        IConfiguration configuration,
        Contracts.Plugin.IPluginHost host)
    {
        _logger = logger;
        _loader = loader;
        _configuration = configuration;
        _host = host;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var profile = _configuration["PluginSystem:Profile"] ?? _host.Profile;
        var paths = _loader.GetConfiguredPluginPaths();
        _logger.LogInformation("Starting plugin discovery for profile {Profile}...", profile);
        var count = await _loader.DiscoverAndLoadAsync(paths, profile, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Plugin discovery complete. Loaded {Count} plugins.", count);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // For MVP, no-op; individual plugins should stop via application lifecycle or explicit unload.
        return Task.CompletedTask;
    }
}
