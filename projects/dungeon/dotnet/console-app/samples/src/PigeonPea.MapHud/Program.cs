using System;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using Terminal.Gui;

namespace PigeonPea.MapHud;

internal static class Program
{
    private static void Main(string[] args)
    {
        Exception? fatal = null;

        Console.WriteLine("[MapHud] Main starting");

        try
        {
            Application.Init();

            var top = new Toplevel
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            Console.WriteLine("[MapHud] Toplevel created");

            var frame = new FrameView
            {
                Title = "Pigeon Pea Map HUD (Sandbox)",
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // Generate a demo world map using FantasyMapGenerator
            // Use the same settings as MapDemoApplication.GenerateMap (known-good)
            var settings = new MapGenerationSettings
            {
                Width = 800,
                Height = 600,
                Seed = 123456,
                NumPoints = 2000,
                RNGMode = RNGMode.Alea,
                SeedString = "demo-seed",
                ReseedAtPhaseStart = true,
                GridMode = GridMode.Jittered,
                HeightmapMode = HeightmapMode.Template,
                UseAdvancedNoise = false,
                HeightmapTemplate = "continents"
            };

            IMapGenerator generator = new FantasyMapGeneratorAdapter();
            Console.WriteLine("[MapHud] Generating map...");
            var map = generator.Generate(settings);
            Console.WriteLine($"[MapHud] Map generated: cells={map.Cells?.Count ?? 0}");

            // Start near map center to avoid corner ocean
            var mapView = new MapHudMapView(map)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                OffsetX = Math.Max(0, settings.Width / 2.0 - 40),
                OffsetY = Math.Max(0, settings.Height / 2.0 - 12),
                Zoom = 1.0
            };

            frame.Add(mapView);
            top.Add(frame);

            Console.WriteLine("[MapHud] UI constructed; entering Terminal.Gui loop (press Q to quit)");

            // Simple camera + zoom controls (signature matches EventHandler<Key>)
            top.KeyDown += (sender, key) =>
            {
                bool changed = false;
                switch (key.KeyCode)
                {
                    case KeyCode.CursorUp:
                    case KeyCode.W:
                        mapView.OffsetY = Math.Max(0, mapView.OffsetY - 10 * mapView.Zoom);
                        changed = true;
                        break;
                    case KeyCode.CursorDown:
                    case KeyCode.S:
                        mapView.OffsetY = Math.Min(settings.Height - 1, mapView.OffsetY + 10 * mapView.Zoom);
                        changed = true;
                        break;
                    case KeyCode.CursorLeft:
                    case KeyCode.A:
                        mapView.OffsetX = Math.Max(0, mapView.OffsetX - 10 * mapView.Zoom);
                        changed = true;
                        break;
                    case KeyCode.CursorRight:
                    case KeyCode.D:
                        mapView.OffsetX = Math.Min(settings.Width - 1, mapView.OffsetX + 10 * mapView.Zoom);
                        changed = true;
                        break;
                    case KeyCode.Z:
                        mapView.Zoom = Math.Max(0.25, mapView.Zoom * 0.8);
                        changed = true;
                        break;
                    case KeyCode.X:
                        mapView.Zoom = Math.Min(8.0, mapView.Zoom * 1.25);
                        changed = true;
                        break;
                    case KeyCode.Q:
                        Application.RequestStop();
                        break;
                }

                if (changed)
                {
                    mapView.SetNeedsDraw();
                }
            };

            Application.Run(top);
        }
        catch (Exception ex)
        {
            fatal = ex;
        }
        finally
        {
            Application.Shutdown();
        }

        if (fatal is not null)
        {
            Console.Error.WriteLine("Unhandled exception in MapHud:");
            Console.Error.WriteLine(fatal);
            Console.WriteLine("Press any key to exit (exception)...");
            Console.ReadKey(intercept: true);
        }
        else
        {
            Console.WriteLine("[MapHud] Application.Run ended without exception.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(intercept: true);
        }
    }
}
