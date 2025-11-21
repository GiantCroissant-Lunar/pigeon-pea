using System.Collections.Generic;
using PigeonPea.Shared.Scale;
using Xunit;

namespace PigeonPea.Shared.Tests.Scale;

public class ScaleConfigLoaderTests
{
    [Fact]
    public void GetDefaultScales_ReturnsNonEmptyList()
    {
        var configSet = ScaleConfigLoader.LoadFromDirectory("nonexistent");

        Assert.NotNull(configSet);
        Assert.NotEmpty(configSet.Scales);
        Assert.Contains(configSet.Scales, s => s.Id == "world");
    }

    [Fact]
    public void ScaleConfig_HasExpectedProperties()
    {
        var scale = new ScaleConfig(
            Id: "test",
            Environment: "world",
            MetersPerCell: 1000.0,
            MinZoom: 0.75,
            MaxZoom: 2.0,
            ChunkSizeCells: 32,
            Description: "Test scale");

        Assert.Equal("test", scale.Id);
        Assert.Equal("world", scale.Environment);
        Assert.Equal(1000.0, scale.MetersPerCell);
        Assert.Equal(0.75, scale.MinZoom);
        Assert.Equal(2.0, scale.MaxZoom);
    }

    [Fact]
    public void ScaleTransition_HasExpectedProperties()
    {
        var transition = new ScaleTransition(
            Id: "test-transition",
            FromScaleId: "world",
            ToScaleId: "town",
            Trigger: TransitionTrigger.ZoomThreshold,
            Threshold: 2.0,
            Direction: TransitionDirection.ZoomIn,
            Description: "Test transition");

        Assert.Equal("test-transition", transition.Id);
        Assert.Equal("world", transition.FromScaleId);
        Assert.Equal("town", transition.ToScaleId);
        Assert.Equal(TransitionTrigger.ZoomThreshold, transition.Trigger);
        Assert.Equal(2.0, transition.Threshold);
        Assert.Equal(TransitionDirection.ZoomIn, transition.Direction);
    }
}
