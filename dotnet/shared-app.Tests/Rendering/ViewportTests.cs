using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Shared.Tests.Rendering;

/// <summary>
/// Unit tests for the <see cref="Viewport"/> struct.
/// </summary>
public class ViewportTests
{
    [Fact]
    public void ViewportConstructorInitializesCorrectly()
    {
        // Arrange & Act
        var viewport = new Viewport(10, 20, 80, 24);

        // Assert
        Assert.Equal(10, viewport.X);
        Assert.Equal(20, viewport.Y);
        Assert.Equal(80, viewport.Width);
        Assert.Equal(24, viewport.Height);
    }

    [Fact]
    public void ViewportBoundsReturnsCorrectRectangle()
    {
        // Arrange
        var viewport = new Viewport(5, 10, 100, 50);

        // Act
        var bounds = viewport.Bounds;

        // Assert
        Assert.Equal(5, bounds.X);
        Assert.Equal(10, bounds.Y);
        Assert.Equal(100, bounds.Width);
        Assert.Equal(50, bounds.Height);
    }

    [Fact]
    public void ViewportContainsPointInsideReturnsTrue()
    {
        // Arrange
        var viewport = new Viewport(10, 20, 80, 24);

        // Act & Assert
        Assert.True(viewport.Contains(10, 20)); // Top-left corner
        Assert.True(viewport.Contains(50, 30)); // Middle
        Assert.True(viewport.Contains(89, 43)); // Bottom-right (one before edge)
    }

    [Fact]
    public void ViewportContainsPointOutsideReturnsFalse()
    {
        // Arrange
        var viewport = new Viewport(10, 20, 80, 24);

        // Act & Assert
        Assert.False(viewport.Contains(9, 20));   // Left of viewport
        Assert.False(viewport.Contains(10, 19));  // Above viewport
        Assert.False(viewport.Contains(90, 30));  // Right of viewport (at X + Width)
        Assert.False(viewport.Contains(50, 44));  // Below viewport (at Y + Height)
        Assert.False(viewport.Contains(0, 0));    // Far outside
        Assert.False(viewport.Contains(100, 100)); // Far outside
    }

    [Fact]
    public void ViewportContainsBoundaryConditionsWorksCorrectly()
    {
        // Arrange
        var viewport = new Viewport(0, 0, 10, 10);

        // Act & Assert - Inclusive boundaries
        Assert.True(viewport.Contains(0, 0));   // Minimum corner
        Assert.True(viewport.Contains(9, 9));   // Maximum valid point

        // Act & Assert - Exclusive boundaries
        Assert.False(viewport.Contains(10, 0));  // Width boundary
        Assert.False(viewport.Contains(0, 10));  // Height boundary
        Assert.False(viewport.Contains(10, 10)); // Both boundaries
    }

    [Fact]
    public void ViewportContainsNegativeCoordinatesWorksCorrectly()
    {
        // Arrange
        var viewport = new Viewport(-10, -10, 20, 20);

        // Act & Assert
        Assert.True(viewport.Contains(-10, -10)); // Minimum corner
        Assert.True(viewport.Contains(0, 0));     // Center
        Assert.True(viewport.Contains(9, 9));     // Maximum valid point
        Assert.False(viewport.Contains(-11, 0));  // Outside left
        Assert.False(viewport.Contains(0, -11));  // Outside top
        Assert.False(viewport.Contains(10, 0));   // Outside right
        Assert.False(viewport.Contains(0, 10));   // Outside bottom
    }

    [Fact]
    public void ViewportDefaultValueHasExpectedDefaults()
    {
        // Arrange & Act
        var viewport = default(Viewport);

        // Assert
        Assert.Equal(0, viewport.X);
        Assert.Equal(0, viewport.Y);
        Assert.Equal(0, viewport.Width);
        Assert.Equal(0, viewport.Height);
    }

    [Fact]
    public void ViewportSetPropertiesModifiesCorrectly()
    {
        // Arrange
        var viewport = new Viewport(10, 20, 80, 24);

        // Act
        viewport.X = 5;
        viewport.Y = 15;
        viewport.Width = 100;
        viewport.Height = 30;

        // Assert
        Assert.Equal(5, viewport.X);
        Assert.Equal(15, viewport.Y);
        Assert.Equal(100, viewport.Width);
        Assert.Equal(30, viewport.Height);
    }

    [Fact]
    public void ViewportContainsSingleCellViewportWorksCorrectly()
    {
        // Arrange
        var viewport = new Viewport(5, 5, 1, 1);

        // Act & Assert
        Assert.True(viewport.Contains(5, 5));   // Only valid position
        Assert.False(viewport.Contains(4, 5));  // Left
        Assert.False(viewport.Contains(6, 5));  // Right
        Assert.False(viewport.Contains(5, 4));  // Above
        Assert.False(viewport.Contains(5, 6));  // Below
    }
}
