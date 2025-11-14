using Microsoft.Extensions.Configuration;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Map.Rendering;
using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;
using TilesNS = PigeonPea.Shared.Rendering.Tiles;

namespace PigeonPea.Console;

internal static class ConsoleMapDemoRunner
{
    public static void RunInteractive()
    {
        var settings = LoadSettingsFromConfig() ?? DefaultSettings();
        IMapGenerator generator = new FantasyMapGeneratorAdapter();
        var map = generator.Generate(settings);

        // Renderers
        var ascii = new Rendering.AsciiRenderer(true);
        var braille = new Rendering.BrailleRenderer();
        var iterm2 = new Rendering.ITerm2GraphicsRenderer();
        var kitty = new Rendering.KittyGraphicsRenderer();
        var sixel = new Rendering.SixelRenderer();
        var caps = Rendering.TerminalCapabilities.Detect();
        bool useITerm2 = caps.SupportsiTerm2Graphics;
        bool useKitty = false;
        bool useSixel = false;
        bool useBraille = false;

        double zoom = 1.0;
        int width = System.Console.WindowWidth > 0 ? System.Console.WindowWidth : 100;
        int height = System.Console.WindowHeight > 1 ? System.Console.WindowHeight - 1 : 30;
        int cameraX = System.Math.Max(0, settings.Width / 2 - width / 2);
        int cameraY = System.Math.Max(0, settings.Height / 2 - height / 2);

        ConsoleRenderTarget? target = null;
        void Init()
        {
            target = new ConsoleRenderTarget(width, height);
            ascii.Initialize(target);
            braille.Initialize(target);
            iterm2.Initialize(target);
            kitty.Initialize(target);
            sixel.Initialize(target);
        }
        Init();

        System.Console.WriteLine("Controls: Z/X zoom, arrows/WASD pan, I/K/V/B/T switch renderer, R regen, Q quit");
        bool running = true;
        while (running)
        {
            int newWidth = System.Console.WindowWidth > 0 ? System.Console.WindowWidth : width;
            int newHeight = System.Console.WindowHeight > 1 ? System.Console.WindowHeight - 1 : height;
            if (newWidth != width || newHeight != height)
            { width = newWidth; height = newHeight; Init(); }

            IRenderer active = useITerm2 ? iterm2 : (useKitty ? (IRenderer)kitty : (useSixel ? sixel : (useBraille ? braille : ascii)));
            active.BeginFrame();
            var vp = new PigeonPea.Shared.Rendering.Viewport(cameraX, cameraY, System.Math.Max(1, width), System.Math.Max(1, height));
            // Reserve a right sidebar (Terminal.Gui-like) of 28 columns
            int sidebar = System.Math.Min(28, System.Math.Max(10, width / 4));
            int mapCols = System.Math.Max(1, width - sidebar);
            int mapRows = System.Math.Max(1, height);

            // If iTerm2 is active, render via tile assembler for crisp placement
            if (useITerm2 && active is Rendering.ITerm2GraphicsRenderer it2)
            {
                int maxPpc = 24;
                int ppc = System.Math.Max(4, System.Math.Min(maxPpc, (int)System.Math.Round(16 / System.Math.Max(zoom, 0.5))))
                    ;
                var src = new PigeonPea.Map.Rendering.Tiles.MapTileSource();
                var frame = TilesNS.TileAssembler.Assemble(src, map, new PigeonPea.Shared.Rendering.Viewport(cameraX, cameraY, mapCols, mapRows), mapCols, mapRows, ppc, zoom, true, true);
                it2.ReplaceAndDisplayImage(5555, frame.Rgba, frame.WidthPx, frame.HeightPx, 0, 0, mapCols, mapRows);
            }
            else
            {
                // ASCII/Braille branch: use existing ASCII renderer. For Braille/pixel modes, Map.Rendering handles conversion.
                if (active is PigeonPea.Console.Rendering.BrailleRenderer)
                {
                    var brailleGrid = BrailleMapRenderer.RenderToBraille(map, vp, zoom, ppc: 4, biomeColors: true, rivers: true);
                    // Convert char[,] to a single string for DrawText placeholder (first row only to avoid huge output)
                    int rows = brailleGrid.GetLength(0);
                    int cols = brailleGrid.GetLength(1);
                    var sb = new System.Text.StringBuilder(cols);
                    for (int c = 0; c < cols; c++) sb.Append(brailleGrid[0, c]);
                    var brailleLine = sb.ToString();
                    active.DrawText(0, 0, brailleLine, SadRogue.Primitives.Color.White, SadRogue.Primitives.Color.Black);
                }
                else
                {
                    // Minimal placeholder for ASCII/Sixel/Kitty while migration completes
                    active.DrawText(0, 0, "Rendering via Map.Rendering pending migration", SadRogue.Primitives.Color.White, SadRogue.Primitives.Color.Black);
                }
            }

            // Sidebar content
            int sx = System.Math.Max(0, width - sidebar);
            int sy = 0;
            void Line(string text, SadRogue.Primitives.Color fg)
            {
                active.DrawText(sx, sy, text.PadRight(sidebar - 1), fg, Color.Black);
                sy++;
            }
            Line($"Renderer: {(useITerm2 ? "iTerm2" : useKitty ? "Kitty" : useSixel ? "Sixel" : useBraille ? "Braille" : "ASCII")}", Color.LightGray);
            Line($"Zoom   : {zoom:0.00}", Color.White);
            Line($"Camera : {cameraX},{cameraY}", Color.White);
            sy++;
            Line("Controls:", Color.CadetBlue);
            Line(" Z/X  Zoom +/-", Color.White);
            Line(" Arrows/WASD Pan", Color.White);
            Line(" I/K/V/T Mode", Color.White);
            Line(" R    Regen", Color.White);
            Line(" Q    Quit", Color.White);
            sy++;
            Line($"Map: {settings.Width}x{settings.Height}", Color.LightGray);
            Line($"Sites: {settings.NumPoints}", Color.LightGray);
            active.EndFrame();

            var key = System.Console.ReadKey(true);
            switch (key.Key)
            {
                case System.ConsoleKey.Q: running = false; break;
                case System.ConsoleKey.Z: zoom = System.Math.Max(0.25, zoom * 0.8); break;
                case System.ConsoleKey.X: zoom = System.Math.Min(8.0, zoom * 1.25); break;
                case System.ConsoleKey.W:
                case System.ConsoleKey.UpArrow: cameraY = System.Math.Max(0, cameraY - (int)System.Math.Ceiling(5 * zoom)); break;
                case System.ConsoleKey.S:
                case System.ConsoleKey.DownArrow: cameraY = System.Math.Min(settings.Height - 1, cameraY + (int)System.Math.Ceiling(5 * zoom)); break;
                case System.ConsoleKey.A:
                case System.ConsoleKey.LeftArrow: cameraX = System.Math.Max(0, cameraX - (int)System.Math.Ceiling(10 * zoom)); break;
                case System.ConsoleKey.D:
                case System.ConsoleKey.RightArrow: cameraX = System.Math.Min(settings.Width - 1, cameraX + (int)System.Math.Ceiling(10 * zoom)); break;
                case System.ConsoleKey.I: useITerm2 = true; useKitty = useSixel = useBraille = false; System.Console.Clear(); break;
                case System.ConsoleKey.K: useKitty = true; useITerm2 = useSixel = useBraille = false; System.Console.Clear(); break;
                case System.ConsoleKey.V: useSixel = true; useITerm2 = useKitty = useBraille = false; System.Console.Clear(); break;
                case System.ConsoleKey.B: useBraille = true; useITerm2 = useKitty = useSixel = false; System.Console.Clear(); break;
                case System.ConsoleKey.T: useBraille = useKitty = useSixel = false; useITerm2 = false; System.Console.Clear(); break;
                case System.ConsoleKey.R:
                    map = generator.Generate(settings);
                    System.Console.WriteLine("[Diag] Regenerated map");
                    break;
            }
        }
    }

    private static MapGenerationSettings DefaultSettings() => new MapGenerationSettings
    {
        Width = 800,
        Height = 600,
        Seed = 123456,
        NumPoints = 8000,
        RNGMode = RNGMode.Alea,
        SeedString = "demo-seed",
        ReseedAtPhaseStart = true,
        GridMode = GridMode.Jittered,
        HeightmapMode = HeightmapMode.Template,
        UseAdvancedNoise = false,
        HeightmapTemplate = "archipelago"
    };

    private static MapGenerationSettings? LoadSettingsFromConfig()
    {
        try
        {
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("mapgen.json", optional: true, reloadOnChange: false)
                .AddJsonFile(System.IO.Path.Combine("dotnet", "mapgen.json"), optional: true, reloadOnChange: false);
            var config = builder.Build();
            var s = DefaultSettings();
            config.Bind("map", s);
            return s;
        }
        catch { return null; }
    }
}
