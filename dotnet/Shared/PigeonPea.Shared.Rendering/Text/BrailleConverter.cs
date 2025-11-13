namespace PigeonPea.Shared.Rendering.Text;

public static class BrailleConverter
{
    /// <summary>
    /// Converts an RGBA pixel buffer to Braille characters (2x4 dots per cell).
    /// </summary>
    public static char[,] Convert(byte[] rgba, int width, int height, int brightnessThreshold = 128)
    {
        int cellWidth = (width + 1) / BraillePattern.DotsX;
        int cellHeight = (height + 3) / BraillePattern.DotsY;
        var result = new char[cellWidth, cellHeight];

        for (int cy = 0; cy < cellHeight; cy++)
        {
            for (int cx = 0; cx < cellWidth; cx++)
            {
                byte pattern = 0;
                for (int dy = 0; dy < BraillePattern.DotsY; dy++)
                {
                    for (int dx = 0; dx < BraillePattern.DotsX; dx++)
                    {
                        int px = cx * BraillePattern.DotsX + dx;
                        int py = cy * BraillePattern.DotsY + dy;
                        if (IsPixelOn(rgba, px, py, width, height, brightnessThreshold))
                            pattern = BraillePattern.SetDot(pattern, dx, dy, true);
                    }
                }
                result[cx, cy] = BraillePattern.ToChar(pattern);
            }
        }
        return result;
    }

    private static bool IsPixelOn(byte[] rgba, int x, int y, int width, int height, int threshold)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        int idx = (y * width + x) * 4;
        if (idx + 2 >= rgba.Length) return false;
        int brightness = (int)(rgba[idx] * 0.299 + rgba[idx + 1] * 0.587 + rgba[idx + 2] * 0.114);
        return brightness > threshold;
    }
}
