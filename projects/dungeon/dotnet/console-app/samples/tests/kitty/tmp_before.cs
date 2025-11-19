using Terminal.Gui;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;

namespace PigeonPea.Console;

internal static class TerminalHudApplication
{
    private static MapData _map = null!;
    private static double _zoom = 1.0;
    private static int _cameraX = 0;
    private static int _cameraY = 0;
    private static bool _regenRequested = false;

    public static void Run()
    {
        Application.Init();
        try
        {
            var top = new Toplevel() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };

            // Menu
            var menu = new MenuBar();
            menu.Menus = new MenuBarItem[]
            {
                new MenuBarItem("_File", new MenuItem[]
                {
                    new MenuItem("_Regenerate", string.Empty, () => _regenRequested = true),
                    new MenuItem("_Quit", string.Empty, () => Application.RequestStop())
                }),
                new MenuBarItem("_View", new MenuItem[]
                {
                    new MenuItem("Zoom _In", string.Empty, () => { _zoom = Math.Min(8.0, _zoom * 1.25); }),
                    new MenuItem("Zoom _Out", string.Empty, () => { _zoom = Math.Max(0.25, _zoom * 0.8); })
                }),
                new MenuBarItem("_Help", new MenuItem[]
                {
                    new MenuItem("_About", string.Empty, () => MessageBox.Query("About", "Pigeon Pea HUD", "OK"))
                })
            };
            top.Add(menu);

            // Layout: Map left, Log right
            int sidebar = 28;
            var mapFrame = new FrameView { Title = "Map", X = 0, Y = 1, Width = Dim.Fill(sidebar), Height = Dim.Fill() };
            var logFrame = new FrameView { Title = "Log", X = Pos.Right(mapFrame), Y = 1, Width = sidebar, Height = Dim.Fill() };
            var logView = new TextView { ReadOnly = true, WordWrap = true };
            logFrame.Add(logView);
            top.Add(mapFrame, logFrame);

            // Map setup
            var settings = new MapGenerationSettings { Width = 800, Height = 600, NumPoints = 8000, HeightmapMode = HeightmapMode.Template, Seed = 123456, GridMode = GridMode.Jittered, ReseedAtPhaseStart = true };
            var generator = new MapGenerator();
            _map = generator.Generate(settings);
            _cameraX = Math.Max(0, settings.Width / 2 - 40);
            _cameraY = Math.Max(0, settings.Height / 2 - 12);

            // Create a view inside the frame to render the map with Terminal.Gui driver
            var mapView = new MapPanelView(_map)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = Dim.Fill(1),
                CameraX = _cameraX,
                CameraY = _cameraY,
                Zoom = _zoom,
            };
            mapFrame.Add(mapView);

            // Periodic refresh and regeneration handling
            Application.AddTimeout(TimeSpan.FromMilliseconds(50), () =>
            {
                try
                {
                    if (_regenRequested)
                    {
                        _map = generator.Generate(settings);
                        mapView.Map = _map;
                        _regenRequested = false;
                        AppendLog(logView, "Regenerated map");
                    }

                    // Keep view synced
                    mapView.CameraX = _cameraX;
                    mapView.CameraY = _cameraY;
                    mapView.Zoom = _zoom;
                    mapView.SetNeedsDraw();
                }
                catch { }
                return true;
            });

            // Pan keys
            top.KeyDown += (sender, e) =>
            {
                bool changed = false;
                switch (e.KeyCode)
                {
                    case KeyCode.CursorUp: _cameraY = Math.Max(0, _cameraY - (int)Math.Ceiling(5 * _zoom)); changed = true; break;
                    case KeyCode.CursorDown: _cameraY = Math.Min(Math.Max(1, _map.Height - 1), _cameraY + (int)Math.Ceiling(5 * _zoom)); changed = true; break;
                    case KeyCode.CursorLeft: _cameraX = Math.Max(0, _cameraX - (int)Math.Ceiling(10 * _zoom)); changed = true; break;
                    case KeyCode.CursorRight: _cameraX = Math.Min(Math.Max(1, _map.Width - 1), _cameraX + (int)Math.Ceiling(10 * _zoom)); changed = true; break;
                }
                if (changed) mapView.SetNeedsDraw();
            };

            AppendLog(logView, "HUD started (Terminal.Gui v2)");
            Application.Run(top);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    private static void AppendLog(TextView log, string text)
    {
        log.Text = (log.Text?.ToString() ?? string.Empty) + text + Environment.NewLine;
    }
}
