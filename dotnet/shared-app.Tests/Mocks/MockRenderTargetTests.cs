using PigeonPea.Shared.Tests.Mocks;
using Xunit;

namespace PigeonPea.Shared.Tests.Mocks;

/// <summary>
/// Tests for MockRenderTarget to ensure it properly tracks method calls.
/// </summary>
public class MockRenderTargetTests
{
    [Fact]
    public void MockRenderTargetConstructorSetsDefaultDimensions()
    {
        // Arrange & Act
        var target = new MockRenderTarget();

        // Assert
        Assert.Equal(80, target.Width);
        Assert.Equal(50, target.Height);
        Assert.Null(target.PixelWidth);
        Assert.Null(target.PixelHeight);
    }

    [Fact]
    public void MockRenderTargetConstructorSetsCustomDimensions()
    {
        // Arrange & Act
        var target = new MockRenderTarget(100, 60, 1920, 1080);

        // Assert
        Assert.Equal(100, target.Width);
        Assert.Equal(60, target.Height);
        Assert.Equal(1920, target.PixelWidth);
        Assert.Equal(1080, target.PixelHeight);
    }

    [Fact]
    public void PresentMarksPresentCalled()
    {
        // Arrange
        var target = new MockRenderTarget();
        Assert.False(target.PresentCalled);

        // Act
        target.Present();

        // Assert
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void PresentIncrementsPresentCallCount()
    {
        // Arrange
        var target = new MockRenderTarget();
        Assert.Equal(0, target.PresentCallCount);

        // Act
        target.Present();
        target.Present();
        target.Present();

        // Assert
        Assert.Equal(3, target.PresentCallCount);
    }

    [Fact]
    public void ResetClearsPresentState()
    {
        // Arrange
        var target = new MockRenderTarget();
        target.Present();
        target.Present();
        Assert.True(target.PresentCalled);
        Assert.Equal(2, target.PresentCallCount);

        // Act
        target.Reset();

        // Assert
        Assert.False(target.PresentCalled);
        Assert.Equal(0, target.PresentCallCount);
    }

    [Fact]
    public void MockRenderTargetCanBeUsedWithRenderer()
    {
        // Arrange
        var target = new MockRenderTarget(40, 25);
        var renderer = new MockRenderer();

        // Act
        renderer.Initialize(target);

        // Assert
        Assert.NotNull(renderer.RenderTarget);
        Assert.Equal(40, renderer.RenderTarget?.Width);
        Assert.Equal(25, renderer.RenderTarget?.Height);
    }
}
