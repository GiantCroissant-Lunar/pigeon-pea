using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

class TestKittyChunked
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

    static void WriteChunk(Stream stdout, string chunk)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(chunk);
        stdout.Write(bytes, 0, bytes.Length);
        stdout.Flush();
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
        Console.Error.WriteLine($"Base64 length: {b64.Length}");

        // Chunk the base64 data (4096 bytes per chunk like imgcat might do)
        const int chunkSize = 4096;
        int offset = 0;
        int chunkNum = 0;

        using (Stream stdout = Console.OpenStandardOutput())
        {
            while (offset < b64.Length)
            {
                int len = Math.Min(chunkSize, b64.Length - offset);
                string dataChunk = b64.Substring(offset, len);
                offset += len;
                bool isLast = offset >= b64.Length;
                int m = isLast ? 0 : 1; // m=1 means more chunks coming

                string cmd;
                if (chunkNum == 0)
                {
                    // First chunk: include metadata
                    cmd = $"\x1b_Ga=T,f=32,s={width},v={height},m={m};{dataChunk}\x1b\\";
                }
                else
                {
                    // Continuation chunk
                    cmd = $"\x1b_Gm={m};{dataChunk}\x1b\\";
                }

                WriteChunk(stdout, cmd);
                Console.Error.WriteLine($"Sent chunk {chunkNum} ({dataChunk.Length} bytes, m={m})");
                chunkNum++;
            }
        }

        Console.Error.WriteLine("\nIf you see a red square above, Kitty graphics works!");
        Console.Error.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}

