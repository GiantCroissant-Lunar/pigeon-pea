using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Utility methods for color interpolation and distance-based fading.
/// </summary>
public static class ColorGradient
{
    public static Color Lerp(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t)
        );
    }

    public static Color[] CreateGradient(Color start, Color end, int steps)
    {
        if (steps < 2)
            throw new ArgumentException("Gradient must have at least 2 steps.", nameof(steps));

        var gradient = new Color[steps];
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)(steps - 1);
            gradient[i] = Lerp(start, end, t);
        }
        return gradient;
    }

    public static Color ApplyDistanceFade(Color baseColor, float distance, float maxDistance)
    {
        if (maxDistance <= 0f)
            return distance > 0f ? Color.Black : baseColor;

        float t = Math.Clamp(distance / maxDistance, 0f, 1f);
        return Lerp(baseColor, Color.Black, t);
    }
}
