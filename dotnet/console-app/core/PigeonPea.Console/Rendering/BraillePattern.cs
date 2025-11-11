namespace PigeonPea.Console.Rendering;

/// <summary>
/// Helper class for working with Unicode Braille patterns.
/// Braille patterns provide 2x4 dot resolution per character cell.
/// Unicode range: U+2800 to U+28FF (256 patterns).
/// </summary>
/// <remarks>
/// Dot layout in a Braille character:
/// <code>
///   0 3
///   1 4
///   2 5
///   6 7
/// </code>
/// Each dot position maps to a bit in the byte value:
/// Bit 0 = Dot 1, Bit 1 = Dot 2, Bit 2 = Dot 3, Bit 3 = Dot 4,
/// Bit 4 = Dot 5, Bit 5 = Dot 6, Bit 6 = Dot 7, Bit 7 = Dot 8
/// </remarks>
public static class BraillePattern
{
    /// <summary>
    /// Base Unicode code point for Braille patterns.
    /// </summary>
    private const int BrailleBase = 0x2800;

    /// <summary>
    /// Number of horizontal dots in a Braille character.
    /// </summary>
    public const int DotsX = 2;

    /// <summary>
    /// Number of vertical dots in a Braille character.
    /// </summary>
    public const int DotsY = 4;

    /// <summary>
    /// Bit masks for individual Braille dots.
    /// </summary>
    private static readonly byte[] DotMasks = new byte[]
    {
        0b00000001, // Dot 1 (top-left)
        0b00000010, // Dot 2 (middle-left)
        0b00000100, // Dot 3 (bottom-left)
        0b00001000, // Dot 4 (top-right)
        0b00010000, // Dot 5 (middle-right)
        0b00100000, // Dot 6 (bottom-right)
        0b01000000, // Dot 7 (bottom-bottom-left)
        0b10000000, // Dot 8 (bottom-bottom-right)
    };

    /// <summary>
    /// Converts a byte pattern to a Braille Unicode character.
    /// </summary>
    /// <param name="pattern">Byte with bits representing which dots are active.</param>
    /// <returns>The corresponding Braille Unicode character.</returns>
    public static char ToChar(byte pattern)
    {
        return (char)(BrailleBase + pattern);
    }

    /// <summary>
    /// Converts a Braille character back to its byte pattern.
    /// </summary>
    /// <param name="brailleChar">A Braille Unicode character.</param>
    /// <returns>The byte pattern, or 0 if not a valid Braille character.</returns>
    public static byte FromChar(char brailleChar)
    {
        int codePoint = brailleChar;
        if (codePoint >= BrailleBase && codePoint < BrailleBase + 256)
        {
            return (byte)(codePoint - BrailleBase);
        }
        return 0;
    }

    /// <summary>
    /// Creates a Braille pattern from individual dot states.
    /// </summary>
    /// <param name="dots">Array of 8 boolean values indicating which dots are active.</param>
    /// <returns>The Braille character.</returns>
    public static char FromDots(params bool[] dots)
    {
        byte pattern = 0;
        for (int i = 0; i < Math.Min(dots.Length, 8); i++)
        {
            if (dots[i])
            {
                pattern |= DotMasks[i];
            }
        }
        return ToChar(pattern);
    }

    /// <summary>
    /// Sets a specific dot in a pattern.
    /// </summary>
    /// <param name="pattern">The current pattern byte.</param>
    /// <param name="x">The X position of the dot (0 or 1).</param>
    /// <param name="y">The Y position of the dot (0 to 3).</param>
    /// <param name="value">Whether the dot should be on or off.</param>
    /// <returns>The updated pattern.</returns>
    public static byte SetDot(byte pattern, int x, int y, bool value)
    {
        if (x < 0 || x >= DotsX || y < 0 || y >= DotsY)
        {
            return pattern;
        }

        int dotIndex = GetDotIndex(x, y);
        if (value)
        {
            pattern |= DotMasks[dotIndex];
        }
        else
        {
            pattern &= (byte)~DotMasks[dotIndex];
        }
        return pattern;
    }

    /// <summary>
    /// Gets the state of a specific dot in a pattern.
    /// </summary>
    /// <param name="pattern">The pattern byte.</param>
    /// <param name="x">The X position of the dot (0 or 1).</param>
    /// <param name="y">The Y position of the dot (0 to 3).</param>
    /// <returns>True if the dot is on, false otherwise.</returns>
    public static bool GetDot(byte pattern, int x, int y)
    {
        if (x < 0 || x >= DotsX || y < 0 || y >= DotsY)
        {
            return false;
        }

        int dotIndex = GetDotIndex(x, y);
        return (pattern & DotMasks[dotIndex]) != 0;
    }

    /// <summary>
    /// Converts dot X,Y coordinates to dot index (0-7).
    /// </summary>
    private static int GetDotIndex(int x, int y)
    {
        // Mapping:
        // (0,0) -> 0, (1,0) -> 3
        // (0,1) -> 1, (1,1) -> 4
        // (0,2) -> 2, (1,2) -> 5
        // (0,3) -> 6, (1,3) -> 7
        return x == 0 ? (y == 3 ? 6 : y) : (y == 3 ? 7 : y + 3);
    }

    /// <summary>
    /// Creates an empty Braille pattern (all dots off).
    /// </summary>
    public static char Empty => ToChar(0);

    /// <summary>
    /// Creates a full Braille pattern (all dots on).
    /// </summary>
    public static char Full => ToChar(0xFF);
}
