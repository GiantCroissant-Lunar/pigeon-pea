using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

class TestKittyBinary
{
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
            try
            {
                var handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (GetConsoleMode(handle, out uint mode))
                {
                    mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
                    SetConsoleMode(handle, mode);
                }
            }
            catch { }
        }
    }

    static void Main()
    {
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

        // Build the command as bytes to avoid any string encoding issues
        string cmdStr = $"\x1b_Ga=T,f=32,s={width},v={height};{b64}\x1b\\";
        byte[] cmdBytes = Encoding.UTF8.GetBytes(cmdStr);

        // Write directly to stdout's base stream
        using (Stream stdout = Console.OpenStandardOutput())
        {
            stdout.Write(cmdBytes, 0, cmdBytes.Length);
            stdout.Flush();
        }

        // Use stderr for messages so they don't interfere with the image
        Console.Error.WriteLine("\nIf you see a red square above, Kitty graphics works!");
        Console.Error.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}
