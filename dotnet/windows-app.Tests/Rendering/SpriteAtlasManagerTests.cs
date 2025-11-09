using PigeonPea.Windows.Rendering;
using SkiaSharp;
using Xunit;

namespace PigeonPea.Windows.Tests.Rendering;

/// <summary>
/// Unit tests for the <see cref="SpriteAtlasManager"/> class.
/// </summary>
public class SpriteAtlasManagerTests : IDisposable
{
    private readonly SpriteAtlasManager _manager;
    private readonly string _testAtlasPath;
    private readonly string _testDefinitionPath;

    public SpriteAtlasManagerTests()
    {
        _manager = new SpriteAtlasManager();
        
        // Use paths relative to the test assembly location
        var testDataDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData");
        _testAtlasPath = Path.Combine(testDataDir, "test-atlas.png");
        _testDefinitionPath = Path.Combine(testDataDir, "test-atlas.json");
    }

    public void Dispose()
    {
        _manager?.Dispose();
    }

    [Fact]
    public void LoadAtlas_WithValidFiles_LoadsSuccessfully()
    {
        // Act
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);

        // Assert
        Assert.Equal(1, _manager.AtlasCount);
        Assert.Equal(4, _manager.SpriteCount);
    }

    [Fact]
    public void LoadAtlas_WithMissingAtlasFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var invalidPath = "nonexistent.png";

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() =>
            _manager.LoadAtlas(invalidPath, _testDefinitionPath));
        Assert.Contains("Atlas file not found", ex.Message);
    }

    [Fact]
    public void LoadAtlas_WithMissingDefinitionFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var invalidPath = "nonexistent.json";

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() =>
            _manager.LoadAtlas(_testAtlasPath, invalidPath));
        Assert.Contains("Definition file not found", ex.Message);
    }

    [Fact]
    public void HasSprite_WithLoadedSprite_ReturnsTrue()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);

        // Act & Assert
        Assert.True(_manager.HasSprite(1));
        Assert.True(_manager.HasSprite(2));
        Assert.True(_manager.HasSprite(3));
        Assert.True(_manager.HasSprite(4));
    }

    [Fact]
    public void HasSprite_WithNonexistentSprite_ReturnsFalse()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);

        // Act & Assert
        Assert.False(_manager.HasSprite(999));
        Assert.False(_manager.HasSprite(0));
        Assert.False(_manager.HasSprite(-1));
    }

    [Fact]
    public void GetSprite_WithLoadedSprite_ReturnsNonNullImage()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);

        // Act
        var sprite1 = _manager.GetSprite(1);
        var sprite2 = _manager.GetSprite(2);
        var sprite3 = _manager.GetSprite(3);
        var sprite4 = _manager.GetSprite(4);

        // Assert
        Assert.NotNull(sprite1);
        Assert.NotNull(sprite2);
        Assert.NotNull(sprite3);
        Assert.NotNull(sprite4);
    }

    [Fact]
    public void GetSprite_WithNonexistentSprite_ReturnsNull()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);

        // Act
        var sprite = _manager.GetSprite(999);

        // Assert
        Assert.Null(sprite);
    }

    [Fact]
    public void GetSprite_ReturnsCorrectDimensions()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);

        // Act
        var sprite = _manager.GetSprite(1);

        // Assert
        Assert.NotNull(sprite);
        Assert.Equal(16, sprite.Width);
        Assert.Equal(16, sprite.Height);
    }

    [Fact]
    public void GetLoadedSpriteIds_WithLoadedAtlas_ReturnsAllIds()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);

        // Act
        var ids = _manager.GetLoadedSpriteIds().ToList();

        // Assert
        Assert.Equal(4, ids.Count);
        Assert.Contains(1, ids);
        Assert.Contains(2, ids);
        Assert.Contains(3, ids);
        Assert.Contains(4, ids);
    }

    [Fact]
    public void GetLoadedSpriteIds_WithNoAtlas_ReturnsEmptyCollection()
    {
        // Act
        var ids = _manager.GetLoadedSpriteIds();

        // Assert
        Assert.Empty(ids);
    }

    [Fact]
    public void AtlasCount_WithNoAtlas_ReturnsZero()
    {
        // Act & Assert
        Assert.Equal(0, _manager.AtlasCount);
    }

    [Fact]
    public void SpriteCount_WithNoSprites_ReturnsZero()
    {
        // Act & Assert
        Assert.Equal(0, _manager.SpriteCount);
    }

    [Fact]
    public void LoadAtlas_WithMultipleAtlases_LoadsAll()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);

        // Create a second test atlas in a temp location
        var tempAtlasPath = Path.Combine(Path.GetTempPath(), $"test-atlas-{Guid.NewGuid()}.png");
        var tempDefinitionPath = Path.Combine(Path.GetTempPath(), $"test-atlas-{Guid.NewGuid()}.json");
        
        try
        {
            // Copy the test files to temp location (different path = different atlas)
            File.Copy(_testAtlasPath, tempAtlasPath);
            File.Copy(_testDefinitionPath, tempDefinitionPath);

            // Act - Load the second atlas from different path
            _manager.LoadAtlas(tempAtlasPath, tempDefinitionPath);

            // Assert
            Assert.Equal(2, _manager.AtlasCount);
            Assert.Equal(4, _manager.SpriteCount); // Still 4 because IDs are the same (sprites get overwritten)
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempAtlasPath))
                File.Delete(tempAtlasPath);
            if (File.Exists(tempDefinitionPath))
                File.Delete(tempDefinitionPath);
        }
    }

    [Fact]
    public void LoadAtlas_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJsonPath = Path.Combine(Path.GetTempPath(), "invalid.json");
        File.WriteAllText(invalidJsonPath, "{ invalid json }");

        try
        {
            // Act & Assert
            Assert.Throws<System.Text.Json.JsonException>(() =>
                _manager.LoadAtlas(_testAtlasPath, invalidJsonPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(invalidJsonPath))
                File.Delete(invalidJsonPath);
        }
    }

    [Fact]
    public void LoadAtlas_WithSpriteBeyondAtlasBounds_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidDefinitionPath = Path.Combine(Path.GetTempPath(), "invalid-bounds.json");
        var invalidDefinition = """
        [
          {
            "Id": 999,
            "Name": "out-of-bounds",
            "X": 100,
            "Y": 100,
            "Width": 16,
            "Height": 16
          }
        ]
        """;
        File.WriteAllText(invalidDefinitionPath, invalidDefinition);

        try
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _manager.LoadAtlas(_testAtlasPath, invalidDefinitionPath));
            Assert.Contains("exceed atlas dimensions", ex.Message);
        }
        finally
        {
            // Cleanup
            if (File.Exists(invalidDefinitionPath))
                File.Delete(invalidDefinitionPath);
        }
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);

        // Act & Assert - should not throw
        _manager.Dispose();
        _manager.Dispose();
    }

    [Fact]
    public void HasSprite_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);
        _manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _manager.HasSprite(1));
    }

    [Fact]
    public void GetSprite_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);
        _manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _manager.GetSprite(1));
    }

    [Fact]
    public void LoadAtlas_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath));
    }

    [Fact]
    public void GetLoadedSpriteIds_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);
        _manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _manager.GetLoadedSpriteIds());
    }

    [Fact]
    public void AtlasCount_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);
        _manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = _manager.AtlasCount);
    }

    [Fact]
    public void SpriteCount_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);
        _manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = _manager.SpriteCount);
    }

    [Fact]
    public void GetSprite_VerifySpriteColorsAreCorrect()
    {
        // Arrange
        _manager.LoadAtlas(_testAtlasPath, _testDefinitionPath);

        // Act
        var redSprite = _manager.GetSprite(1);
        var greenSprite = _manager.GetSprite(2);
        var blueSprite = _manager.GetSprite(3);
        var yellowSprite = _manager.GetSprite(4);

        // Assert - Verify sprites were loaded
        Assert.NotNull(redSprite);
        Assert.NotNull(greenSprite);
        Assert.NotNull(blueSprite);
        Assert.NotNull(yellowSprite);

        // Convert to bitmaps to check pixel colors
        using var redBitmap = SKBitmap.FromImage(redSprite);
        using var greenBitmap = SKBitmap.FromImage(greenSprite);
        using var blueBitmap = SKBitmap.FromImage(blueSprite);
        using var yellowBitmap = SKBitmap.FromImage(yellowSprite);

        // Check center pixel of each sprite
        var redPixel = redBitmap.GetPixel(8, 8);
        var greenPixel = greenBitmap.GetPixel(8, 8);
        var bluePixel = blueBitmap.GetPixel(8, 8);
        var yellowPixel = yellowBitmap.GetPixel(8, 8);

        // Verify colors (allow for some encoding tolerance)
        // Note: ImageSharp's Color.Green is (0, 128, 0) not (0, 255, 0)
        Assert.True(redPixel.Red > 200 && redPixel.Green < 100 && redPixel.Blue < 100, 
            $"Red sprite should be predominantly red. Got R:{redPixel.Red} G:{redPixel.Green} B:{redPixel.Blue}");
        Assert.True(greenPixel.Green > 100 && greenPixel.Red < 50 && greenPixel.Blue < 50, 
            $"Green sprite should be predominantly green. Got R:{greenPixel.Red} G:{greenPixel.Green} B:{greenPixel.Blue}");
        Assert.True(bluePixel.Blue > 200 && bluePixel.Red < 100 && bluePixel.Green < 100, 
            $"Blue sprite should be predominantly blue. Got R:{bluePixel.Red} G:{bluePixel.Green} B:{bluePixel.Blue}");
        Assert.True(yellowPixel.Red > 200 && yellowPixel.Green > 200 && yellowPixel.Blue < 100, 
            $"Yellow sprite should be red+green. Got R:{yellowPixel.Red} G:{yellowPixel.Green} B:{yellowPixel.Blue}");
    }
}
