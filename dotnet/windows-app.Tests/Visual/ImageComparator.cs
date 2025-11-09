using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PigeonPea.Windows.Tests.Visual;

/// <summary>
/// Provides functionality for pixel-by-pixel image comparison.
/// </summary>
public class ImageComparator
{
    /// <summary>
    /// Gets or sets the similarity threshold (0.0 to 1.0). Images with similarity
    /// greater than or equal to this value are considered matching.
    /// Default is 0.99 (99% similarity).
    /// </summary>
    public double Threshold { get; set; } = 0.99;

    /// <summary>
    /// Gets or sets the per-channel pixel difference tolerance (0-255).
    /// Pixels with differences within this tolerance are considered matching.
    /// Default is 5.
    /// </summary>
    public int PixelTolerance { get; set; } = 5;

    /// <summary>
    /// Compares two images and returns a comparison result.
    /// </summary>
    /// <param name="expectedPath">Path to the expected image.</param>
    /// <param name="actualPath">Path to the actual image.</param>
    /// <returns>An ImageComparisonResult containing the comparison details.</returns>
    public ImageComparisonResult Compare(string expectedPath, string actualPath)
    {
        using var expected = Image.Load<Rgba32>(expectedPath);
        using var actual = Image.Load<Rgba32>(actualPath);

        return Compare(expected, actual);
    }

    /// <summary>
    /// Compares two images and returns a comparison result.
    /// </summary>
    /// <param name="expected">The expected image.</param>
    /// <param name="actual">The actual image.</param>
    /// <returns>An ImageComparisonResult containing the comparison details.</returns>
    public ImageComparisonResult Compare(Image<Rgba32> expected, Image<Rgba32> actual)
    {
        // Check dimensions
        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            return new ImageComparisonResult
            {
                Match = false,
                Similarity = 0.0,
                DifferentPixels = expected.Width * expected.Height,
                TotalPixels = expected.Width * expected.Height,
                Reason = $"Dimensions differ: expected {expected.Width}x{expected.Height}, actual {actual.Width}x{actual.Height}"
            };
        }

        int differentPixels = 0;
        int totalPixels = expected.Width * expected.Height;

        // Pixel-by-pixel comparison
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                var expectedPixel = expected[x, y];
                var actualPixel = actual[x, y];

                if (!PixelsMatch(expectedPixel, actualPixel))
                {
                    differentPixels++;
                }
            }
        }

        double similarity = totalPixels > 0 ? 1.0 - (double)differentPixels / totalPixels : 1.0;

        return new ImageComparisonResult
        {
            Match = similarity >= Threshold,
            Similarity = similarity,
            DifferentPixels = differentPixels,
            TotalPixels = totalPixels
        };
    }

    /// <summary>
    /// Compares two images and generates a difference image highlighting the differences.
    /// </summary>
    /// <param name="expectedPath">Path to the expected image.</param>
    /// <param name="actualPath">Path to the actual image.</param>
    /// <param name="diffOutputPath">Path where the difference image will be saved.</param>
    /// <returns>An ImageComparisonResult containing the comparison details and diff image path.</returns>
    public ImageComparisonResult CompareWithDiff(string expectedPath, string actualPath, string diffOutputPath)
    {
        using var expected = Image.Load<Rgba32>(expectedPath);
        using var actual = Image.Load<Rgba32>(actualPath);

        var result = Compare(expected, actual);

        if (!result.Match && expected.Width == actual.Width && expected.Height == actual.Height)
        {
            GenerateDiffImage(expected, actual, diffOutputPath);
            result.DiffImagePath = diffOutputPath;
        }

        return result;
    }

    /// <summary>
    /// Generates a difference image highlighting pixels that differ between two images.
    /// Matching pixels are shown in grayscale, differing pixels are highlighted in red.
    /// </summary>
    /// <param name="expected">The expected image.</param>
    /// <param name="actual">The actual image.</param>
    /// <param name="outputPath">Path where the difference image will be saved.</param>
    public void GenerateDiffImage(Image<Rgba32> expected, Image<Rgba32> actual, string outputPath)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            throw new ArgumentException("Images must have the same dimensions to generate a diff image.");
        }

        using var diffImage = new Image<Rgba32>(expected.Width, expected.Height);

        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                var expectedPixel = expected[x, y];
                var actualPixel = actual[x, y];

                if (PixelsMatch(expectedPixel, actualPixel))
                {
                    // Show matching pixels in grayscale
                    byte gray = (byte)((expectedPixel.R + expectedPixel.G + expectedPixel.B) / 3);
                    diffImage[x, y] = new Rgba32(gray, gray, gray, 255);
                }
                else
                {
                    // Highlight differing pixels in red
                    diffImage[x, y] = new Rgba32(255, 0, 0, 255);
                }
            }
        }

        diffImage.Save(outputPath);
    }

    /// <summary>
    /// Determines if two pixels match within the configured tolerance.
    /// </summary>
    /// <param name="a">First pixel.</param>
    /// <param name="b">Second pixel.</param>
    /// <returns>True if pixels match within tolerance, false otherwise.</returns>
    private bool PixelsMatch(Rgba32 a, Rgba32 b)
    {
        return Math.Abs(a.R - b.R) <= PixelTolerance &&
               Math.Abs(a.G - b.G) <= PixelTolerance &&
               Math.Abs(a.B - b.B) <= PixelTolerance &&
               Math.Abs(a.A - b.A) <= PixelTolerance;
    }
}
