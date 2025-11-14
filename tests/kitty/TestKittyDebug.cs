using System;
using System.Runtime.InteropServices;

class TestKittyDebug
{
    // Windows console API for enabling VT sequences
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    const int STD_OUTPUT_HANDLE = -11;
    const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

    static void EnableVTMode()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            GetConsoleMode(handle, out uint mode);
            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
            SetConsoleMode(handle, mode);
        }
    }

    static void Main()
    {
        Console.WriteLine("Enabling VT mode...");
        EnableVTMode();

        // Create a simple 32x32 red square
        int width = 32;
        int height = 32;
        byte[] rgba = new byte[width * height * 4];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = 255;     // R
            rgba[i + 1] = 0;   // G
            rgba[i + 2] = 0;   // B
            rgba[i + 3] = 255; // A
        }

        string b64 = Convert.ToBase64String(rgba);

        Console.WriteLine($"Image data: {rgba.Length} bytes -> {b64.Length} base64 chars");
        Console.WriteLine($"First 50 chars of base64: {b64.Substring(0, Math.Min(50, b64.Length))}");

        // Try transmit and display all in one (like imgcat probably does)
        string cmd = $"\x1b_Ga=T,f=32,s={width},v={height};{b64}\x1b\\";
        Console.WriteLine($"\nSending Kitty command ({cmd.Length} chars)...");
        Console.WriteLine("Sequence starts with: ESC _ G a = T , f = 3 2 ...");

        Console.Write(cmd);
        Console.Out.Flush();

        Console.WriteLine("\n\nIf you see a red square above, Kitty graphics works!");
        Console.WriteLine("If not, there's an issue with how .NET sends escape sequences.");
        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }
}
