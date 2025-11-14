using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

class TestKittyDirect
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

    const int STD_OUTPUT_HANDLE = -11;
    const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

    static void Main()
    {
        // Get the console output handle
        IntPtr hOut = GetStdHandle(STD_OUTPUT_HANDLE);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Try to enable VT processing
            if (GetConsoleMode(hOut, out uint mode))
            {
                Console.Error.WriteLine($"Original console mode: 0x{mode:X}");
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
                SetConsoleMode(hOut, mode);
                GetConsoleMode(hOut, out uint newMode);
                Console.Error.WriteLine($"After setting VT flags: 0x{newMode:X}");
            }
        }

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

        // Build command - use a=T (transmit and display at cursor)
        string cmd = $"\x1b_Ga=T,f=32,s={width},v={height};{b64}\x1b\\";
        byte[] cmdBytes = Encoding.UTF8.GetBytes(cmd);

        Console.Error.WriteLine($"\nWriting {cmdBytes.Length} bytes directly to console handle...");
        Console.Error.WriteLine($"First 30 bytes (hex): {BitConverter.ToString(cmdBytes, 0, Math.Min(30, cmdBytes.Length))}");

        // Write directly to console handle using WriteFile
        bool success = WriteFile(hOut, cmdBytes, (uint)cmdBytes.Length, out uint written, IntPtr.Zero);

        Console.Error.WriteLine($"WriteFile returned: {success}, wrote {written} bytes");

        if (!success)
        {
            Console.Error.WriteLine($"Error: {Marshal.GetLastWin32Error()}");
        }

        // Try also with standard Console.Write for comparison
        Console.Error.WriteLine("\nNow trying with Console.Write...");
        Console.Write(cmd);
        Console.Out.Flush();

        // And with OpenStandardOutput
        Console.Error.WriteLine("\nAnd with OpenStandardOutput...");
        using (Stream stdout = Console.OpenStandardOutput())
        {
            stdout.Write(cmdBytes, 0, cmdBytes.Length);
            stdout.Flush();
        }

        Console.Error.WriteLine("\n\nDid you see a red square with ANY of the three methods?");
        Console.Error.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}
