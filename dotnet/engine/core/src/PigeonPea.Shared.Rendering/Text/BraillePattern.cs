namespace PigeonPea.Shared.Rendering.Text;

public static class BraillePattern
{
    private const int BrailleBase = 0x2800;
    public const int DotsX = 2;
    public const int DotsY = 4;

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

    public static char ToChar(byte pattern) => (char)(BrailleBase + pattern);

    public static byte FromChar(char brailleChar)
    {
        int codePoint = brailleChar;
        if (codePoint >= BrailleBase && codePoint < BrailleBase + 256)
            return (byte)(codePoint - BrailleBase);
        return 0;
    }

    public static char FromDots(params bool[] dots)
    {
        byte pattern = 0;
        for (int i = 0; i < Math.Min(dots.Length, 8); i++)
            if (dots[i]) pattern |= DotMasks[i];
        return ToChar(pattern);
    }

    public static byte SetDot(byte pattern, int x, int y, bool value)
    {
        if (x < 0 || x >= DotsX || y < 0 || y >= DotsY) return pattern;
        int dotIndex = GetDotIndex(x, y);
        if (value) pattern |= DotMasks[dotIndex];
        else pattern &= (byte)~DotMasks[dotIndex];
        return pattern;
    }

    public static bool GetDot(byte pattern, int x, int y)
    {
        if (x < 0 || x >= DotsX || y < 0 || y >= DotsY) return false;
        int dotIndex = GetDotIndex(x, y);
        return (pattern & DotMasks[dotIndex]) != 0;
    }

    private static int GetDotIndex(int x, int y)
        => x == 0 ? (y == 3 ? 6 : y) : (y == 3 ? 7 : y + 3);

    public static char Empty => ToChar(0);
    public static char Full => ToChar(0xFF);
}
