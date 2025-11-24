using FluentAssertions;
using PigeonPea.Resource.Contracts;
using Xunit;

namespace PigeonPea.PluginSystem.Tests;

public class ResourceContractsTests
{
    [Fact]
    public void LoadProgressHasAllProperties()
    {
        var progress = new LoadProgress("texture1", 1024, 2048, 50.0f);

        progress.ResourceId.Should().Be("texture1");
        progress.BytesLoaded.Should().Be(1024);
        progress.TotalBytes.Should().Be(2048);
        progress.PercentComplete.Should().Be(50.0f);
    }

    [Fact]
    public void LoadProgressSupportsUnknownTotalBytes()
    {
        var progress = new LoadProgress("data1", 512, null, 0.0f);

        progress.TotalBytes.Should().BeNull();
        progress.BytesLoaded.Should().Be(512);
    }

    [Fact]
    public void ResourceMetadataHasAllProperties()
    {
        var metadata = new ResourceMetadata("sound1", "audio", 4096, "/sounds/effect.wav");

        metadata.ResourceId.Should().Be("sound1");
        metadata.ResourceType.Should().Be("audio");
        metadata.SizeBytes.Should().Be(4096);
        metadata.Path.Should().Be("/sounds/effect.wav");
    }

    [Fact]
    public void ResourceLoadResultHasAllProperties()
    {
        var resource = "test resource";
        var metadata = new ResourceMetadata("res1", "text", 100, "/data/test.txt");

        var result = new ResourceLoadResult<string>(resource, metadata, 150);

        result.Resource.Should().Be(resource);
        result.Metadata.Should().Be(metadata);
        result.LoadTimeMs.Should().Be(150);
    }
}
