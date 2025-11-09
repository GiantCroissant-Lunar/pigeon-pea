using Arch.Core;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Shared.Tests.Rendering;

/// <summary>
/// Unit tests for the <see cref="Camera"/> class.
/// </summary>
public class CameraTests : IDisposable
{
    private readonly World _world;

    public CameraTests()
    {
        _world = World.Create();
    }

    public void Dispose()
    {
        _world.Dispose();
    }

    [Fact]
    public void Camera_DefaultConstructor_InitializesCorrectly()
    {
        // Arrange & Act
        var camera = new Camera();

        // Assert
        Assert.Equal(Point.None, camera.Position);
        Assert.Null(camera.FollowTarget);
        Assert.Equal(80, camera.ViewportWidth);
        Assert.Equal(24, camera.ViewportHeight);
    }

    [Fact]
    public void Camera_ConstructorWithDimensions_InitializesCorrectly()
    {
        // Arrange & Act
        var camera = new Camera(100, 50);

        // Assert
        Assert.Equal(Point.None, camera.Position);
        Assert.Null(camera.FollowTarget);
        Assert.Equal(100, camera.ViewportWidth);
        Assert.Equal(50, camera.ViewportHeight);
    }

    [Fact]
    public void Camera_Follow_SetsFollowTarget()
    {
        // Arrange
        var camera = new Camera();
        var entity = _world.Create();

        // Act
        camera.Follow(entity);

        // Assert
        Assert.Equal(entity, camera.FollowTarget);
    }

    [Fact]
    public void Camera_Unfollow_ClearsFollowTarget()
    {
        // Arrange
        var camera = new Camera();
        var entity = _world.Create();
        camera.Follow(entity);

        // Act
        camera.Unfollow();

        // Assert
        Assert.Null(camera.FollowTarget);
    }

    [Fact]
    public void Camera_Update_WithNoFollowTarget_DoesNotChangePosition()
    {
        // Arrange
        var camera = new Camera();
        var initialPosition = new Point(10, 10);
        camera.Position = initialPosition;
        var mapBounds = new Rectangle(0, 0, 100, 100);

        // Act
        camera.Update(_world, mapBounds);

        // Assert
        Assert.Equal(initialPosition, camera.Position);
    }

    [Fact]
    public void Camera_Update_WithFollowTarget_CentersOnTarget()
    {
        // Arrange
        var camera = new Camera(20, 10);
        var entity = _world.Create(new Position(50, 50));
        camera.Follow(entity);
        var mapBounds = new Rectangle(0, 0, 100, 100);

        // Act
        camera.Update(_world, mapBounds);

        // Assert
        // Camera should center on entity (50, 50)
        // X: 50 - 20/2 = 40, Y: 50 - 10/2 = 45
        Assert.Equal(40, camera.Position.X);
        Assert.Equal(45, camera.Position.Y);
    }

    [Fact]
    public void Camera_Update_ClampsToMapBounds_Left()
    {
        // Arrange
        var camera = new Camera(20, 10);
        var entity = _world.Create(new Position(5, 50)); // Near left edge
        camera.Follow(entity);
        var mapBounds = new Rectangle(0, 0, 100, 100);

        // Act
        camera.Update(_world, mapBounds);

        // Assert
        // Camera should be clamped to X = 0
        Assert.Equal(0, camera.Position.X);
    }

    [Fact]
    public void Camera_Update_ClampsToMapBounds_Top()
    {
        // Arrange
        var camera = new Camera(20, 10);
        var entity = _world.Create(new Position(50, 3)); // Near top edge
        camera.Follow(entity);
        var mapBounds = new Rectangle(0, 0, 100, 100);

        // Act
        camera.Update(_world, mapBounds);

        // Assert
        // Camera should be clamped to Y = 0
        Assert.Equal(0, camera.Position.Y);
    }

    [Fact]
    public void Camera_Update_ClampsToMapBounds_Right()
    {
        // Arrange
        var camera = new Camera(20, 10);
        var entity = _world.Create(new Position(95, 50)); // Near right edge
        camera.Follow(entity);
        var mapBounds = new Rectangle(0, 0, 100, 100);

        // Act
        camera.Update(_world, mapBounds);

        // Assert
        // Camera should be clamped to X = 80 (100 - 20)
        Assert.Equal(80, camera.Position.X);
    }

    [Fact]
    public void Camera_Update_ClampsToMapBounds_Bottom()
    {
        // Arrange
        var camera = new Camera(20, 10);
        var entity = _world.Create(new Position(50, 96)); // Near bottom edge
        camera.Follow(entity);
        var mapBounds = new Rectangle(0, 0, 100, 100);

        // Act
        camera.Update(_world, mapBounds);

        // Assert
        // Camera should be clamped to Y = 90 (100 - 10)
        Assert.Equal(90, camera.Position.Y);
    }

    [Fact]
    public void Camera_Update_WithDeadEntity_DoesNotCrash()
    {
        // Arrange
        var camera = new Camera();
        var entity = _world.Create(new Position(50, 50));
        camera.Follow(entity);
        _world.Destroy(entity); // Kill the entity
        var mapBounds = new Rectangle(0, 0, 100, 100);
        var initialPosition = camera.Position;

        // Act
        camera.Update(_world, mapBounds);

        // Assert - Camera position should not change
        Assert.Equal(initialPosition, camera.Position);
    }

    [Fact]
    public void Camera_Update_WithEntityWithoutPosition_DoesNotCrash()
    {
        // Arrange
        var camera = new Camera();
        var entity = _world.Create(); // Entity without Position component
        camera.Follow(entity);
        var mapBounds = new Rectangle(0, 0, 100, 100);
        var initialPosition = camera.Position;

        // Act
        camera.Update(_world, mapBounds);

        // Assert - Camera position should not change
        Assert.Equal(initialPosition, camera.Position);
    }

    [Fact]
    public void Camera_GetViewport_ReturnsViewportAtCameraPosition()
    {
        // Arrange
        var camera = new Camera(80, 24);
        camera.Position = new Point(10, 20);

        // Act
        var viewport = camera.GetViewport();

        // Assert
        Assert.Equal(10, viewport.X);
        Assert.Equal(20, viewport.Y);
        Assert.Equal(80, viewport.Width);
        Assert.Equal(24, viewport.Height);
    }

    [Fact]
    public void Camera_Update_WithSmallMap_HandlesGracefully()
    {
        // Arrange
        var camera = new Camera(50, 30);
        var entity = _world.Create(new Position(10, 10));
        camera.Follow(entity);
        // Map is smaller than viewport
        var mapBounds = new Rectangle(0, 0, 30, 20);

        // Act
        camera.Update(_world, mapBounds);

        // Assert
        // Camera should be clamped at (0, 0) since map is smaller
        Assert.Equal(0, camera.Position.X);
        Assert.Equal(0, camera.Position.Y);
    }

    [Fact]
    public void Camera_Update_WithNegativeMapBounds_WorksCorrectly()
    {
        // Arrange
        var camera = new Camera(20, 10);
        var entity = _world.Create(new Position(0, 0));
        camera.Follow(entity);
        var mapBounds = new Rectangle(-50, -50, 100, 100);

        // Act
        camera.Update(_world, mapBounds);

        // Assert
        // Camera should center on (0, 0): X = 0 - 10 = -10, Y = 0 - 5 = -5
        Assert.Equal(-10, camera.Position.X);
        Assert.Equal(-5, camera.Position.Y);
    }

    [Fact]
    public void Camera_Position_CanBeSetDirectly()
    {
        // Arrange
        var camera = new Camera();

        // Act
        camera.Position = new Point(42, 84);

        // Assert
        Assert.Equal(42, camera.Position.X);
        Assert.Equal(84, camera.Position.Y);
    }
}
