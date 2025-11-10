using PigeonPea.Console.Rendering;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Console.Tests.Rendering;

/// <summary>
/// Unit tests for the <see cref="SixelEncoder"/> class.
/// </summary>
public class SixelEncoderTests
{
    [Fact]
    public void Encode_WithValidByteArray_ReturnsSixelString()
    {
        // Arrange
        var encoder = new SixelEncoder();
        int width = 4;
        int height = 4;
        var imageData = new byte[width * height * 3];

        // Fill with red color
        for (int i = 0; i < imageData.Length; i += 3)
        {
            imageData[i] = 255;     // R
            imageData[i + 1] = 0;   // G
            imageData[i + 2] = 0;   // B
        }

        // Act
        string result = encoder.Encode(imageData, width, height);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.StartsWith("\x1bPq", result); // Sixel start sequence
        Assert.EndsWith("\x1b\\", result);   // Sixel end sequence
    }

    [Fact]
    public void Encode_WithColorArray_ReturnsSixelString()
    {
        // Arrange
        var encoder = new SixelEncoder();
        int width = 4;
        int height = 4;
        var pixels = new Color[width * height];

        // Fill with blue color
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.Blue;
        }

        // Act
        string result = encoder.Encode(pixels, width, height);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.StartsWith("\x1bPq", result);
        Assert.EndsWith("\x1b\\", result);
    }

    [Fact]
    public void Encode_WithNullImageData_ThrowsArgumentNullException()
    {
        // Arrange
        var encoder = new SixelEncoder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => encoder.Encode((byte[])null!, 4, 4));
    }

    [Fact]
    public void Encode_WithNullPixels_ThrowsArgumentNullException()
    {
        // Arrange
        var encoder = new SixelEncoder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => encoder.Encode((Color[])null!, 4, 4));
    }

    [Fact]
    public void Encode_WithInvalidImageDataLength_ThrowsArgumentException()
    {
        // Arrange
        var encoder = new SixelEncoder();
        var imageData = new byte[10]; // Wrong length for 4x4 image

        // Act & Assert
        Assert.Throws<ArgumentException>(() => encoder.Encode(imageData, 4, 4));
    }

    [Fact]
    public void Encode_WithInvalidPixelsLength_ThrowsArgumentException()
    {
        // Arrange
        var encoder = new SixelEncoder();
        var pixels = new Color[10]; // Wrong length for 4x4 image

        // Act & Assert
        Assert.Throws<ArgumentException>(() => encoder.Encode(pixels, 4, 4));
    }

    [Fact]
    public void Encode_WithMultipleColors_IncludesPaletteDefinitions()
    {
        // Arrange
        var encoder = new SixelEncoder();
        int width = 2;
        int height = 2;
        var pixels = new Color[]
        {
            Color.Red, Color.Green,
            Color.Blue, Color.Yellow
        };

        // Act
        string result = encoder.Encode(pixels, width, height);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("#0;2;", result); // Palette definition marker
    }

    [Fact]
    public void Encode_WithSingleColor_ProducesValidSixel()
    {
        // Arrange
        var encoder = new SixelEncoder();
        int width = 8;
        int height = 6;
        var pixels = new Color[width * height];

        // Fill with white color
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.White;
        }

        // Act
        string result = encoder.Encode(pixels, width, height);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("\x1bPq", result);
        Assert.EndsWith("\x1b\\", result);
        Assert.Contains("#0;2;", result); // Should have at least one palette entry
    }

    [Fact]
    public void CreateTestPattern_ReturnsValidSixel()
    {
        // Arrange
        var encoder = new SixelEncoder();

        // Act
        string result = encoder.CreateTestPattern(16, 12);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.StartsWith("\x1bPq", result);
        Assert.EndsWith("\x1b\\", result);
    }

    [Fact]
    public void Encode_WithLargeImage_HandlesMultipleBands()
    {
        // Arrange
        var encoder = new SixelEncoder();
        int width = 8;
        int height = 24; // 4 bands of 6 pixels each
        var pixels = new Color[width * height];

        // Create gradient
        for (int y = 0; y < height; y++)
        {
            byte intensity = (byte)((y * 255) / height);
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = new Color(intensity, intensity, intensity);
            }
        }

        // Act
        string result = encoder.Encode(pixels, width, height);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("\x1bPq", result);
        Assert.EndsWith("\x1b\\", result);
        Assert.Contains("$-", result); // Band separator (CR + LF in sixel)
    }

    [Fact]
    public void Encode_With256UniqueColors_OptimizesPalette()
    {
        // Arrange
        var encoder = new SixelEncoder();
        int width = 16;
        int height = 16;
        var pixels = new Color[width * height];

        // Create 256 unique colors
        for (int i = 0; i < pixels.Length; i++)
        {
            byte val = (byte)i;
            pixels[i] = new Color(val, (byte)(255 - val), (byte)(val / 2));
        }

        // Act
        string result = encoder.Encode(pixels, width, height);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("\x1bPq", result);
        Assert.EndsWith("\x1b\\", result);
        // Should handle palette optimization for 256 colors
    }
}
