using System.IO;
using System.Text.Json;
using FluentAssertions;
using PigeonPea.PluginSystem;
using Xunit;

namespace PigeonPea.PluginSystem.Tests;

public class ManifestParserTests
{
    [Fact]
    public void Parse_Valid_Manifest_Succeeds()
    {
        using var tmp = new TempDir();
        var json = "{\n  \"id\": \"test.plugin\",\n  \"name\": \"Test Plugin\",\n  \"version\": \"1.2.3\",\n  \"entryPoint\": { \"dotnet.console\": \"Plugin.dll,Plugin.Main\" },\n  \"dependencies\": [],\n  \"capabilities\": [\"foo\"]\n}";
        var path = Path.Combine(tmp.Path, "plugin.json");
        File.WriteAllText(path, json);

        var manifest = ManifestParser.Parse(path);
        manifest.Id.Should().Be("test.plugin");
        manifest.Name.Should().Be("Test Plugin");
        manifest.Version.Should().Be("1.2.3");
        manifest.EntryPoint.Should().ContainKey("dotnet.console");
        manifest.Capabilities.Should().Contain("foo");
    }

    private sealed class TempDir : System.IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString("N"));
        public TempDir() { Directory.CreateDirectory(Path); }
        public void Dispose() { try { Directory.Delete(Path, true); } catch { } }
    }
}
