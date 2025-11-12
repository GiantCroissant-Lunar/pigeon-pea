using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PigeonPea.Contracts.Plugin;
using PigeonPea.PluginSystem;
using Xunit;

namespace PigeonPea.PluginSystem.Tests;

public class PluginLoaderTests
{
    [Fact]
    public async Task DiscoverAndLoadAsync_NoPlugins_ReturnsZero()
    {
        using var tmp = new TempDir();
        var configDict = new Dictionary<string, string?>
        {
            {"PluginSystem:Profile", "dotnet.console"}
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

        var registry = new ServiceRegistry();
        var pluginRegistry = new PluginRegistry();
        var hostMock = new Mock<IPluginHost>();
        hostMock.SetupGet(h => h.Profile).Returns("dotnet.console");

        var loader = new PluginLoader(
            NullLogger<PluginLoader>.Instance,
            NullLoggerFactory.Instance,
            configuration,
            registry,
            pluginRegistry,
            hostMock.Object
        );

        var count = await loader.DiscoverAndLoadAsync(new[] { tmp.Path }, "dotnet.console", CancellationToken.None);
        count.Should().Be(0);
    }

    private sealed class TempDir : System.IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString("N"));
        public TempDir() { System.IO.Directory.CreateDirectory(Path); }
        public void Dispose() { try { System.IO.Directory.Delete(Path, true); } catch { } }
    }
}
