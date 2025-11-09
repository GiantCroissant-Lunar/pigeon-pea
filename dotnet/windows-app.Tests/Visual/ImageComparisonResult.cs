namespace PigeonPea.Windows.Tests.Visual;

/// <summary>
/// Represents the result of an image comparison operation.
/// </summary>
public class ImageComparisonResult
{
    /// <summary>
    /// Gets or sets whether the images match based on the configured threshold.
    /// </summary>
    public bool Match { get; init; }

    /// <summary>
    /// Gets or sets the similarity percentage between 0.0 and 1.0.
    /// </summary>
    public double Similarity { get; init; }

    /// <summary>
    /// Gets or sets the number of pixels that differ between the images.
    /// </summary>
    public int DifferentPixels { get; init; }

    /// <summary>
    /// Gets or sets the total number of pixels compared.
    /// </summary>
    public int TotalPixels { get; init; }

    /// <summary>
    /// Gets or sets the reason for a failed comparison (e.g., dimension mismatch).
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets or sets the path to the generated difference image, if available.
    /// </summary>
    public string? DiffImagePath { get; set; }
}
