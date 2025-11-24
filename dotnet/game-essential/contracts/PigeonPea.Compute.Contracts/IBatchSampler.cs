namespace PigeonPea.Compute.Contracts;

/// <summary>
/// Interface for batch sampling operations (useful for GPU acceleration).
/// </summary>
public interface IBatchSampler
{
    /// <summary>
    /// Samples values from a 2D field at multiple positions in parallel.
    /// </summary>
    /// <param name="field">The 2D field to sample from.</param>
    /// <param name="positions">Positions to sample.</param>
    /// <param name="interpolate">Whether to use interpolation between cells.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Array of sampled values corresponding to input positions.</returns>
    Task<float[]> BatchSampleAsync(
        float[,] field,
        IReadOnlyList<(float x, float y)> positions,
        bool interpolate = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a kernel operation to multiple positions in a field.
    /// </summary>
    /// <param name="field">The 2D field to operate on.</param>
    /// <param name="positions">Positions to apply the kernel.</param>
    /// <param name="kernel">The kernel to apply (relative offsets and weights).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Array of computed values.</returns>
    Task<float[]> BatchKernelAsync(
        float[,] field,
        IReadOnlyList<(int x, int y)> positions,
        IReadOnlyList<((int dx, int dy) offset, float weight)> kernel,
        CancellationToken cancellationToken = default);
}
