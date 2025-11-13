using System;
using PigeonPea.Console.Rendering;

namespace PigeonPea.Console;

/// <summary>
/// Interactive test for iTerm2 graphics without Terminal.Gui.
/// This works by writing escape sequences directly to the console.
/// </summary>
public static class ITerm2InteractiveTest
{
    public static void Run()
    {
        var rng = new Random();
        const int imgSize = 16;
        const int imageId = 42424;

        int cols = 80, rows = 25;
        try
        {
            cols = System.Console.WindowWidth > 0 ? System.Console.WindowWidth : 80;
            rows = System.Console.WindowHeight > 0 ? System.Console.WindowHeight : 25;
        }
        catch { }

        var target = new ConsoleRenderTarget(cols, rows);
        var renderer = new ITerm2GraphicsRenderer();
        renderer.Initialize(target);

        System.Console.Clear();
        System.Console.WriteLine("=== iTerm2 Graphics Interactive Test ===");
        System.Console.WriteLine();
        System.Console.WriteLine("Controls:");
        System.Console.WriteLine("  SPACE - Draw random colored square");
        System.Console.WriteLine("  A     - Toggle auto mode (draws continuously)");
        System.Console.WriteLine("  C     - Clear screen");
        System.Console.WriteLine("  Q     - Quit");
        System.Console.WriteLine();
        System.Console.WriteLine($"Grid: {cols}x{rows}  Image: {imgSize}x{imgSize}");
        System.Console.WriteLine();

        bool autoMode = false;
        bool running = true;

        while (running)
        {
            if (autoMode || System.Console.KeyAvailable)
            {
                ConsoleKeyInfo key = autoMode && !System.Console.KeyAvailable
                    ? new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false)
                    : System.Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.Spacebar:
                        DrawRandomImage(renderer, rng, imgSize, imageId, cols, rows);
                        break;

                    case ConsoleKey.A:
                        autoMode = !autoMode;
                        System.Console.SetCursorPosition(0, 7);
                        System.Console.Write($"Auto mode: {(autoMode ? "ON " : "OFF")}");
                        break;

                    case ConsoleKey.C:
                        System.Console.Clear();
                        System.Console.WriteLine("Screen cleared. Press SPACE to draw.");
                        break;

                    case ConsoleKey.Q:
                        running = false;
                        break;
                }

                if (autoMode)
                {
                    System.Threading.Thread.Sleep(100); // 10 FPS
                }
            }
            else
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        renderer.Dispose();
        System.Console.Clear();
        System.Console.WriteLine("Test complete!");
    }

    private static void DrawRandomImage(ITerm2GraphicsRenderer renderer, Random rng,
        int imgSize, int imageId, int cols, int rows)
    {
        // Random position (leave some margin)
        int x = rng.Next(0, Math.Max(1, cols - 5));
        int y = rng.Next(8, Math.Max(9, rows - 3)); // Start below the instructions

        // Generate random gradient
        var rgba = new byte[imgSize * imgSize * 4];
        byte r = (byte)rng.Next(100, 255);
        byte g = (byte)rng.Next(100, 255);
        byte b = (byte)rng.Next(100, 255);

        for (int py = 0; py < imgSize; py++)
            for (int px = 0; px < imgSize; px++)
            {
                int idx = (py * imgSize + px) * 4;
                rgba[idx] = (byte)(r * px / imgSize);
                rgba[idx + 1] = (byte)(g * py / imgSize);
                rgba[idx + 2] = b;
                rgba[idx + 3] = 255;
            }

        renderer.BeginFrame();
        renderer.ReplaceAndDisplayImage(imageId, rgba, imgSize, imgSize, x, y, 1, 1);
        renderer.EndFrame();
    }
}
