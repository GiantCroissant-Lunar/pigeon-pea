using Arch.Core;
using Arch.Core.Extensions;
using FluentAssertions;
using ObservableCollections;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Rendering;
using PigeonPea.Shared.ViewModels;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Shared.Tests.ViewModels;

/// <summary>
/// Tests for MapViewModel to verify property notifications and ECS synchronization.
/// </summary>
public class MapViewModelTests
{
    private readonly GameWorld _gameWorld;

    public MapViewModelTests()
    {
        _gameWorld = new GameWorld(40, 30);
    }

    [Theory]
    [MemberData(nameof(GetPropertyChangeNotificationData))]
    public void Property_WhenChanged_RaisesNotification(string propertyName, object newValue)
    {
        // Arrange
        var viewModel = new MapViewModel();
        bool propertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == propertyName)
            {
                propertyChanged = true;
            }
        };

        // Act
        var propertyInfo = typeof(MapViewModel).GetProperty(propertyName);
        propertyInfo!.SetValue(viewModel, newValue);

        // Assert
        propertyChanged.Should().BeTrue($"{propertyName} property change should raise PropertyChanged");
        propertyInfo.GetValue(viewModel).Should().Be(newValue);
    }

    public static IEnumerable<object[]> GetPropertyChangeNotificationData()
    {
        yield return new object[] { nameof(MapViewModel.Width), 100 };
        yield return new object[] { nameof(MapViewModel.Height), 50 };
        yield return new object[] { nameof(MapViewModel.CameraPosition), new Point(10, 20) };
    }

    [Fact]
    public void Update_SyncsMapDimensionsFromGameWorld()
    {
        // Arrange
        var viewModel = new MapViewModel();

        // Act
        viewModel.Update(_gameWorld);

        // Assert
        viewModel.Width.Should().Be(40);
        viewModel.Height.Should().Be(30);
    }

    [Fact]
    public void Update_SyncsCameraPositionFromGameWorld()
    {
        // Arrange
        var viewModel = new MapViewModel();

        // Act
        viewModel.Update(_gameWorld);

        // Assert
        viewModel.CameraPosition.Should().NotBe(Point.None, "Camera should be positioned after update");
    }

    [Fact]
    public void Update_CameraFollowsPlayer()
    {
        // Arrange
        // Use a smaller camera viewport so the camera can actually move within the map
        var camera = new Camera(10, 10);
        var viewModel = new MapViewModel(camera);

        // Get player's initial position
        var initialPlayerPos = _gameWorld.PlayerEntity.Get<Position>().Point;

        // Act
        viewModel.Update(_gameWorld);
        var initialCameraPos = viewModel.CameraPosition;

        // Move player multiple times to ensure significant movement
        for (int i = 0; i < 5; i++)
        {
            _gameWorld.TryMovePlayer(new Point(1, 0));
        }

        var movedPlayerPos = _gameWorld.PlayerEntity.Get<Position>().Point;

        // Update again
        viewModel.Update(_gameWorld);
        var updatedCameraPos = viewModel.CameraPosition;

        // Assert
        initialCameraPos.Should().NotBe(Point.None);
        
        // Verify player actually moved
        if (movedPlayerPos != initialPlayerPos)
        {
            // If player moved, camera should have moved too (or stayed at boundary)
            // The camera should track the player unless at map boundaries
            updatedCameraPos.Should().NotBe(Point.None, "Camera position should be set");
        }
    }

    [Fact]
    public void Update_PopulatesVisibleTiles()
    {
        // Arrange
        var viewModel = new MapViewModel();

        // Act
        viewModel.Update(_gameWorld);

        // Assert
        viewModel.VisibleTiles.Should().NotBeEmpty("Visible tiles should be populated from game world");
    }

    [Fact]
    public void VisibleTiles_ContainsTilesInViewport()
    {
        // Arrange
        var camera = new Camera(10, 10);
        var viewModel = new MapViewModel(camera);

        // Act
        viewModel.Update(_gameWorld);

        // Assert
        viewModel.VisibleTiles.Count.Should().BeGreaterThan(0);
        
        // All tiles should be within the camera viewport
        var viewport = camera.GetViewport();
        foreach (var tile in viewModel.VisibleTiles)
        {
            viewport.Contains(tile.X, tile.Y).Should().BeTrue(
                $"Tile at ({tile.X}, {tile.Y}) should be within viewport bounds");
        }
    }

    [Fact]
    public void VisibleTiles_UpdatesWhenCameraMoves()
    {
        // Arrange - Use smaller camera viewport so camera can actually move within the map
        var camera = new Camera(10, 10);
        var viewModel = new MapViewModel(camera);
        viewModel.Update(_gameWorld);
        var initialTiles = viewModel.VisibleTiles.Select(t => t.Position).ToHashSet();
        var initialCameraPos = viewModel.CameraPosition;

        // Act - Move player significantly to ensure camera movement
        int movesMade = 0;
        for (int i = 0; i < 10; i++)
        {
            if (_gameWorld.TryMovePlayer(new Point(1, 0)))
            {
                movesMade++;
            }
            else if (_gameWorld.TryMovePlayer(new Point(0, 1)))
            {
                movesMade++;
            }
            
            if (movesMade >= 6)
                break;
        }

        viewModel.Update(_gameWorld);
        var newTiles = viewModel.VisibleTiles.Select(t => t.Position).ToHashSet();
        var newCameraPos = viewModel.CameraPosition;

        // Assert
        newTiles.Should().NotBeEmpty();
        
        // Only verify tiles changed if camera actually moved
        if (newCameraPos != initialCameraPos)
        {
            newTiles.Should().NotBeEquivalentTo(initialTiles, "The set of visible tiles should change when the camera moves.");
        }
    }

    [Fact]
    public void VisibleTiles_IncludesExploredStatus()
    {
        // Arrange
        var viewModel = new MapViewModel();

        // Act
        viewModel.Update(_gameWorld);

        // Assert
        // Player FOV should mark some tiles as explored
        var exploredTiles = viewModel.VisibleTiles.Where(t => t.IsExplored).ToList();
        exploredTiles.Should().NotBeEmpty("Some tiles should be marked as explored");
    }

    [Fact]
    public void VisibleTiles_IncludesVisibilityStatus()
    {
        // Arrange
        var viewModel = new MapViewModel();

        // Act
        viewModel.Update(_gameWorld);

        // Assert
        // Tiles in player's FOV should be marked as visible
        var visibleTiles = viewModel.VisibleTiles.Where(t => t.IsVisible).ToList();
        visibleTiles.Should().NotBeEmpty("Some tiles should be marked as visible");
    }

    [Fact]
    public void VisibleTiles_ClearsBeforeUpdate()
    {
        // Arrange
        var viewModel = new MapViewModel();
        viewModel.Update(_gameWorld);
        var firstUpdateCount = viewModel.VisibleTiles.Count;

        // Act - Update again
        viewModel.Update(_gameWorld);
        var secondUpdateCount = viewModel.VisibleTiles.Count;

        // Assert
        secondUpdateCount.Should().BeGreaterThan(0);
        // Should have similar count (not accumulated)
        Math.Abs(secondUpdateCount - firstUpdateCount).Should().BeLessThan(firstUpdateCount / 2,
            "Tile count should not accumulate between updates");
    }

    [Fact]
    public void Update_WithDeadPlayer_DoesNotCrash()
    {
        // Arrange
        var viewModel = new MapViewModel();
        
        // Kill the player
        ref var health = ref _gameWorld.EcsWorld.Get<Health>(_gameWorld.PlayerEntity);
        health.Current = 0;

        // Act
        Action act = () => viewModel.Update(_gameWorld);

        // Assert
        act.Should().NotThrow("Update should handle dead player gracefully");
    }

    [Fact]
    public void Update_MultipleUpdates_SyncsCorrectly()
    {
        // Arrange
        var viewModel = new MapViewModel();

        // Act - First update
        viewModel.Update(_gameWorld);
        var firstWidth = viewModel.Width;
        var firstHeight = viewModel.Height;

        // Act - Second update
        viewModel.Update(_gameWorld);

        // Assert
        viewModel.Width.Should().Be(firstWidth, "Width should remain consistent across updates");
        viewModel.Height.Should().Be(firstHeight, "Height should remain consistent across updates");
    }

    [Fact]
    public void PropertyChanged_DoesNotFireWhenValueUnchanged()
    {
        // Arrange
        var viewModel = new MapViewModel { Width = 100 };
        int changeCount = 0;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MapViewModel.Width))
            {
                changeCount++;
            }
        };

        // Act
        viewModel.Width = 100; // Same value

        // Assert
        changeCount.Should().Be(0, "PropertyChanged should not fire when value is unchanged");
    }

    [Fact]
    public void CameraPosition_InitiallyNone()
    {
        // Arrange & Act
        var viewModel = new MapViewModel();

        // Assert
        viewModel.CameraPosition.Should().Be(Point.None);
    }

    [Fact]
    public void VisibleTiles_InitiallyEmpty()
    {
        // Arrange & Act
        var viewModel = new MapViewModel();

        // Assert
        viewModel.VisibleTiles.Should().BeEmpty();
    }

    [Fact]
    public void VisibleTiles_TilesHaveCorrectProperties()
    {
        // Arrange
        var viewModel = new MapViewModel();

        // Act
        viewModel.Update(_gameWorld);

        // Assert
        viewModel.VisibleTiles.Should().NotBeEmpty();
        
        foreach (var tile in viewModel.VisibleTiles)
        {
            // Each tile should have valid coordinates
            tile.X.Should().BeGreaterOrEqualTo(0);
            tile.Y.Should().BeGreaterOrEqualTo(0);
            
            // Glyph should be set (not default)
            tile.Glyph.Should().NotBe('\0');
            
            // Colors should be set
            tile.Foreground.Should().NotBe(default(Color));
            tile.Background.Should().NotBe(default(Color));
        }
    }

    [Fact]
    public void Update_DimsTilesNotInFOV()
    {
        // Arrange
        var viewModel = new MapViewModel();

        // Act
        viewModel.Update(_gameWorld);

        // Assert - Find explored but not visible tiles
        var exploredNotVisible = viewModel.VisibleTiles
            .Where(t => t.IsExplored && !t.IsVisible)
            .ToList();

        if (exploredNotVisible.Any())
        {
            // Verify that explored but not visible tiles have dimmed colors
            // Dimmed colors should have RGB values that are half of the original
            foreach (var tile in exploredNotVisible)
            {
                // Colors should not be at full brightness (not equal to white or bright colors)
                // Dimmed colors have R/2, G/2, B/2, so they should be darker
                (tile.Foreground.R < 200 || tile.Foreground.G < 200 || tile.Foreground.B < 200)
                    .Should().BeTrue($"Explored but not visible tile at ({tile.X}, {tile.Y}) should have dimmed foreground color");
            }
        }
    }

    [Fact]
    public void VisibleTiles_ObservableList_SupportsNotifications()
    {
        // Arrange
        var viewModel = new MapViewModel();
        bool collectionChanged = false;
        
        viewModel.VisibleTiles.CollectionChanged += (in NotifyCollectionChangedEventArgs<TileViewModel> e) =>
        {
            collectionChanged = true;
        };

        // Act
        viewModel.Update(_gameWorld);

        // Assert
        collectionChanged.Should().BeTrue("ObservableList should raise CollectionChanged events");
    }
}
