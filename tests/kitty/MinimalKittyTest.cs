using System;

class MinimalKittyTest
{
    static void Main()
    {
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

        // Encode to base64
        string b64 = Convert.ToBase64String(rgba);

        // Send Kitty graphics command: transmit and display (a=T)
        // Using ST terminator (ESC backslash)
        Console.Write($"\x1b_Ga=T,f=32,s={width},v={height};{b64}\x1b\\");
        Console.Out.Flush();

        Console.WriteLine("\n\nIf you see a red square above, Kitty graphics works!");
        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}

