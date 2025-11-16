namespace PigeonPea.Shared.Rendering.Text;

public static class AsciiConverter
{
    public static string[] Convert(byte[] rgba, int width, int height)
    {
        // Placeholder: simple luminance to ASCII ramp conversion
        const string ramp = " .:-=+*#%@";
        var rows = new string[height];
        for (int y = 0; y < height; y++)
        {
            var chars = new char[width];
            for (int x = 0; x < width; x++)
            {
                int idx = (y * width + x) * 4;
                if (idx + 2 >= rgba.Length) { chars[x] = ' '; continue; }
                int lum = (int)(rgba[idx] * 0.299 + rgba[idx + 1] * 0.587 + rgba[idx + 2] * 0.114);
                int si = lum * (ramp.Length - 1) / 255;
                chars[x] = ramp[si];
            }
            rows[y] = new string(chars);
        }
        return rows;
    }
}
