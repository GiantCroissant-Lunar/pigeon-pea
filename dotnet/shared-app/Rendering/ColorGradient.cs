using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Utility class for color gradient operations, color interpolation, and fog of war effects.
/// </summary>
public static class ColorGradient
{
    /// <summary>
    /// Linearly interpolates between two colors.
    /// </summary>
    /// <param name="a">The starting color.</param>
    /// <param name="b">The ending color.</param>
    /// <param name="t">The interpolation factor (0.0 = a, 1.0 = b).</param>
    /// <returns>The interpolated color.</returns>
    public static Color Lerp(Color a, Color b, float t)
    {
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t)
        );
    }

    /// <summary>
    /// Creates an array of colors representing a gradient between two colors.
    /// </summary>
    /// <param name="start">The starting color.</param>
    /// <param name="end">The ending color.</param>
    /// <param name="steps">The number of color steps in the gradient (must be at least 2).</param>
    /// <returns>An array of colors representing the gradient.</returns>
    /// <exception cref="ArgumentException">Thrown when steps is less than 2.</exception>
    public static Color[] CreateGradient(Color start, Color end, int steps)
    {
        if (steps < 2)
        {
            throw new ArgumentException("Gradient must have at least 2 steps.", nameof(steps));
        }

        var gradient = new Color[steps];
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)(steps - 1);
            gradient[i] = Lerp(start, end, t);
        }

        return gradient;
    }

    /// <summary>
    /// Applies a distance-based fade effect to a color for fog of war.
    /// </summary>
    /// <param name="baseColor">The base color to fade.</param>
    /// <param name="distance">The distance from the viewer.</param>
    /// <param name="maxDistance">The maximum distance at which the color is fully faded to black.</param>
    /// <returns>The faded color based on distance.</returns>
    public static Color ApplyDistanceFade(Color baseColor, float distance, float maxDistance)
    {
        float t = Math.Clamp(distance / maxDistance, 0f, 1f);
        return Lerp(baseColor, Color.Black, t);
    }
}
