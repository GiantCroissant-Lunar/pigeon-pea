using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace PigeonPea.Windows.Tests.Visual;

/// <summary>
/// Unit tests for the <see cref="ImageComparator"/> class.
/// </summary>
public class ImageComparatorTests : IDisposable
{
    private readonly string _testOutputDir;
    private readonly List<string> _createdFiles = new();

    public ImageComparatorTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"ImageComparatorTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);
    }

    public void Dispose()
    {
        // Clean up test files
        foreach (var file in _createdFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        if (Directory.Exists(_testOutputDir))
        {
            Directory.Delete(_testOutputDir, true);
        }
    }

    [Fact]
    public void Compare_IdenticalImages_ReturnsMatch()
    {
        // Arrange
        var comparator = new ImageComparator();
        using var image1 = CreateSolidColorImage(100, 100, new Rgba32(255, 0, 0, 255));
        using var image2 = CreateSolidColorImage(100, 100, new Rgba32(255, 0, 0, 255));

        // Act
        var result = comparator.Compare(image1, image2);

        // Assert
        Assert.True(result.Match);
        Assert.Equal(1.0, result.Similarity);
        Assert.Equal(0, result.DifferentPixels);
        Assert.Equal(10000, result.TotalPixels);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Compare_DifferentDimensions_ReturnsNoMatch()
    {
        // Arrange
        var comparator = new ImageComparator();
        using var image1 = CreateSolidColorImage(100, 100, new Rgba32(255, 0, 0, 255));
        using var image2 = CreateSolidColorImage(50, 50, new Rgba32(255, 0, 0, 255));

        // Act
        var result = comparator.Compare(image1, image2);

        // Assert
        Assert.False(result.Match);
        Assert.Equal(0.0, result.Similarity);
        Assert.Contains("Dimensions differ", result.Reason);
    }

    [Fact]
    public void Compare_CompletelyDifferentImages_ReturnsZeroSimilarity()
    {
        // Arrange
        var comparator = new ImageComparator();
        using var image1 = CreateSolidColorImage(100, 100, new Rgba32(255, 0, 0, 255));
        using var image2 = CreateSolidColorImage(100, 100, new Rgba32(0, 255, 0, 255));

        // Act
        var result = comparator.Compare(image1, image2);

        // Assert
        Assert.False(result.Match);
        Assert.Equal(0.0, result.Similarity);
        Assert.Equal(10000, result.DifferentPixels);
        Assert.Equal(10000, result.TotalPixels);
    }

    [Fact]
    public void Compare_WithinTolerance_ReturnsMatch()
    {
        // Arrange
        var comparator = new ImageComparator { PixelTolerance = 10 };
        using var image1 = CreateSolidColorImage(100, 100, new Rgba32(100, 100, 100, 255));
        using var image2 = CreateSolidColorImage(100, 100, new Rgba32(105, 105, 105, 255));

        // Act
        var result = comparator.Compare(image1, image2);

        // Assert
        Assert.True(result.Match);
        Assert.Equal(1.0, result.Similarity);
        Assert.Equal(0, result.DifferentPixels);
    }

    [Fact]
    public void Compare_OutsideTolerance_ReturnsNoMatch()
    {
        // Arrange
        var comparator = new ImageComparator { PixelTolerance = 5 };
        using var image1 = CreateSolidColorImage(100, 100, new Rgba32(100, 100, 100, 255));
        using var image2 = CreateSolidColorImage(100, 100, new Rgba32(120, 120, 120, 255));

        // Act
        var result = comparator.Compare(image1, image2);

        // Assert
        Assert.False(result.Match);
        Assert.Equal(0.0, result.Similarity);
        Assert.Equal(10000, result.DifferentPixels);
    }

    [Fact]
    public void Compare_PartiallyDifferentImages_ReturnsCorrectSimilarity()
    {
        // Arrange
        var comparator = new ImageComparator();
        using var image1 = CreateSolidColorImage(100, 100, new Rgba32(255, 0, 0, 255));
        using var image2 = CreateSolidColorImage(100, 100, new Rgba32(255, 0, 0, 255));

        // Make 10% of pixels different
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                image2[x, y] = new Rgba32(0, 255, 0, 255);
            }
        }

        // Act
        var result = comparator.Compare(image1, image2);

        // Assert
        Assert.False(result.Match); // Below default 99% threshold
        Assert.Equal(0.9, result.Similarity, 2);
        Assert.Equal(1000, result.DifferentPixels);
        Assert.Equal(10000, result.TotalPixels);
    }

    [Fact]
    public void Compare_WithCustomThreshold_RespectsThreshold()
    {
        // Arrange
        var comparator = new ImageComparator { Threshold = 0.95 };
        using var image1 = CreateSolidColorImage(100, 100, new Rgba32(255, 0, 0, 255));
        using var image2 = CreateSolidColorImage(100, 100, new Rgba32(255, 0, 0, 255));

        // Make 4% of pixels different (96% similarity)
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                image2[x, y] = new Rgba32(0, 255, 0, 255);
            }
        }

        // Act
        var result = comparator.Compare(image1, image2);

        // Assert
        Assert.True(result.Match); // 96% > 95% threshold
        Assert.Equal(0.96, result.Similarity, 2);
    }

    [Fact]
    public void Compare_FromFilePaths_WorksCorrectly()
    {
        // Arrange
        var comparator = new ImageComparator();
        var path1 = CreateTempImageFile(new Rgba32(255, 0, 0, 255), 50, 50);
        var path2 = CreateTempImageFile(new Rgba32(255, 0, 0, 255), 50, 50);

        // Act
        var result = comparator.Compare(path1, path2);

        // Assert
        Assert.True(result.Match);
        Assert.Equal(1.0, result.Similarity);
    }

    [Fact]
    public void CompareWithDiff_GeneratesDiffImage()
    {
        // Arrange
        var comparator = new ImageComparator();
        var path1 = CreateTempImageFile(new Rgba32(255, 0, 0, 255), 50, 50);
        var path2 = CreateTempImageFile(new Rgba32(0, 255, 0, 255), 50, 50);
        var diffPath = Path.Combine(_testOutputDir, "diff.png");
        _createdFiles.Add(diffPath);

        // Act
        var result = comparator.CompareWithDiff(path1, path2, diffPath);

        // Assert
        Assert.False(result.Match);
        Assert.NotNull(result.DiffImagePath);
        Assert.True(File.Exists(diffPath));
    }

    [Fact]
    public void GenerateDiffImage_HighlightsDifferences()
    {
        // Arrange
        var comparator = new ImageComparator();
        using var image1 = CreateSolidColorImage(100, 100, new Rgba32(255, 255, 255, 255));
        using var image2 = CreateSolidColorImage(100, 100, new Rgba32(255, 255, 255, 255));

        // Make top-left corner different
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                image2[x, y] = new Rgba32(0, 0, 0, 255);
            }
        }

        var diffPath = Path.Combine(_testOutputDir, "diff.png");
        _createdFiles.Add(diffPath);

        // Act
        comparator.GenerateDiffImage(image1, image2, diffPath);

        // Assert
        Assert.True(File.Exists(diffPath));

        using var diffImage = Image.Load<Rgba32>(diffPath);
        Assert.Equal(100, diffImage.Width);
        Assert.Equal(100, diffImage.Height);

        // Check that differing pixels are red
        Assert.Equal(new Rgba32(255, 0, 0, 255), diffImage[0, 0]);

        // Check that matching pixels are grayscale
        var matchingPixel = diffImage[50, 50];
        Assert.Equal(matchingPixel.R, matchingPixel.G);
        Assert.Equal(matchingPixel.G, matchingPixel.B);
    }

    [Fact]
    public void GenerateDiffImage_WithDifferentDimensions_ThrowsException()
    {
        // Arrange
        var comparator = new ImageComparator();
        using var image1 = CreateSolidColorImage(100, 100, new Rgba32(255, 0, 0, 255));
        using var image2 = CreateSolidColorImage(50, 50, new Rgba32(255, 0, 0, 255));
        var diffPath = Path.Combine(_testOutputDir, "diff.png");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            comparator.GenerateDiffImage(image1, image2, diffPath));
    }

    [Fact]
    public void Threshold_CanBeModified()
    {
        // Arrange
        var comparator = new ImageComparator();

        // Act
        comparator.Threshold = 0.85;

        // Assert
        Assert.Equal(0.85, comparator.Threshold);
    }

    [Fact]
    public void PixelTolerance_CanBeModified()
    {
        // Arrange
        var comparator = new ImageComparator();

        // Act
        comparator.PixelTolerance = 15;

        // Assert
        Assert.Equal(15, comparator.PixelTolerance);
    }

    [Fact]
    public void Compare_AlphaChannelDifferences_AreDetected()
    {
        // Arrange
        var comparator = new ImageComparator { PixelTolerance = 0 };
        using var image1 = CreateSolidColorImage(50, 50, new Rgba32(255, 0, 0, 255));
        using var image2 = CreateSolidColorImage(50, 50, new Rgba32(255, 0, 0, 200));

        // Act
        var result = comparator.Compare(image1, image2);

        // Assert
        Assert.False(result.Match);
        Assert.Equal(0.0, result.Similarity);
    }

    [Fact]
    public void Compare_SinglePixelImages_ReturnsMatch()
    {
        // Arrange
        var comparator = new ImageComparator();
        using var image1 = CreateSolidColorImage(1, 1, new Rgba32(128, 128, 128, 255));
        using var image2 = CreateSolidColorImage(1, 1, new Rgba32(128, 128, 128, 255));

        // Act
        var result = comparator.Compare(image1, image2);

        // Assert
        Assert.True(result.Match);
        Assert.Equal(1.0, result.Similarity);
        Assert.Equal(1, result.TotalPixels);
    }

    [Fact]
    public void CompareWithDiff_MatchingImages_DoesNotGenerateDiff()
    {
        // Arrange
        var comparator = new ImageComparator();
        var path1 = CreateTempImageFile(new Rgba32(255, 0, 0, 255), 50, 50);
        var path2 = CreateTempImageFile(new Rgba32(255, 0, 0, 255), 50, 50);
        var diffPath = Path.Combine(_testOutputDir, "diff.png");

        // Act
        var result = comparator.CompareWithDiff(path1, path2, diffPath);

        // Assert
        Assert.True(result.Match);
        Assert.Null(result.DiffImagePath);
        Assert.False(File.Exists(diffPath));
    }

    [Fact]
    public void CompareWithDiff_DimensionMismatch_DoesNotGenerateDiff()
    {
        // Arrange
        var comparator = new ImageComparator();
        var path1 = CreateTempImageFile(new Rgba32(255, 0, 0, 255), 100, 100);
        var path2 = CreateTempImageFile(new Rgba32(255, 0, 0, 255), 50, 50);
        var diffPath = Path.Combine(_testOutputDir, "diff.png");

        // Act
        var result = comparator.CompareWithDiff(path1, path2, diffPath);

        // Assert
        Assert.False(result.Match);
        Assert.Null(result.DiffImagePath);
        Assert.False(File.Exists(diffPath));
    }

    private Image<Rgba32> CreateSolidColorImage(int width, int height, Rgba32 color)
    {
        var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                accessor.GetRowSpan(y).Fill(color);
            }
        });
        return image;
    }

    private string CreateTempImageFile(Rgba32 color, int width, int height)
    {
        var path = Path.Combine(_testOutputDir, $"test_{Guid.NewGuid()}.png");
        using var image = CreateSolidColorImage(width, height, color);
        image.SaveAsPng(path);
        _createdFiles.Add(path);
        return path;
    }
}
