using Terminal.Gui;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using PigeonPea.Shared.Rendering;
using PigeonPea.SharedApp.Rendering;
using PigeonPea.Console.Rendering;
using PigeonPea.SharedApp.Rendering.Tiles;

namespace PigeonPea.Console;

internal static class TerminalHudApplication
{
    private enum RenderMode { Ascii, ITerm2 }
    private static RenderMode _renderMode = RenderMode.Ascii;
    private static MapData _map = null!;
    private static double _zoom = 1.0;
    private static int _cameraX = 0;
    private static int _cameraY = 0;
    private static bool _regenRequested = false;
    private static ITerm2GraphicsRenderer? _pixelRenderer;
    private static IRenderTarget? _pixelTarget;

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
                    // Zoom: smaller value => zoomed in; larger => zoomed out
                    new MenuItem("Zoom _In", string.Empty, () => { _zoom = Math.Max(0.1, _zoom * 0.8); }),
                    new MenuItem("Zoom _Out", string.Empty, () => { _zoom = Math.Min(16.0, _zoom * 1.25); }),
                    null,
                    new MenuItem("Renderer: _ASCII", string.Empty, () => { _renderMode = RenderMode.Ascii; }),
                    new MenuItem("Renderer: _iTerm2", string.Empty, () => { _renderMode = RenderMode.ITerm2; EnsurePixelRenderer(); })
                }),
                new MenuBarItem("_Help", new MenuItem[]
                {
                    new MenuItem("_About", string.Empty, () => MessageBox.Query("About", "Pigeon Pea HUD", "OK"))
                })
            };
            top.Add(menu);

            // Layout: Map top, Log bottom (wider log view as requested)
            int logRows = 10;
            var mapFrame = new FrameView { Title = "Map", X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(logRows) };
            var logFrame = new FrameView { Title = "Log", X = 0, Y = Pos.Bottom(mapFrame), Width = Dim.Fill(), Height = logRows };
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

            // Status bar (v2) using Shortcuts
            var status = new StatusBar();
            var zoomItem = new Shortcut { Title = "Zoom: 1.00", Key = Key.Empty };
            var modeItem = new Shortcut { Title = "Mode: ASCII", Key = Key.Empty };
            status.Add(zoomItem, modeItem);
            top.Add(status);

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
                        AppendMapSummary(logView, _map);
                    }

                    // Keep view synced
                    mapView.CameraX = _cameraX;
                    mapView.CameraY = _cameraY;
                    mapView.Zoom = _zoom;
                    // Update status bar
                    zoomItem.Title = $"Zoom: {_zoom:F2}";
                    modeItem.Title = $"Mode: {(_renderMode == RenderMode.Ascii ? "ASCII" : "iTerm2")}";

                    if (_renderMode == RenderMode.Ascii)
                    {
                        if (!mapView.Visible) mapView.Visible = true;
                        mapView.SetNeedsDraw();
                    }
                    else // ITerm2
                    {
                        if (mapView.Visible) mapView.Visible = false;
                        EnsurePixelRenderer();
                        if (_pixelTarget == null)
                        {
                            _pixelTarget = new ConsoleRenderTarget(System.Console.WindowWidth, System.Console.WindowHeight);
                            _pixelRenderer!.Initialize(_pixelTarget);
                        }
                        var f = mapFrame.Frame;
                        int innerX = Math.Max(0, f.X + 1);
                        int innerY = Math.Max(0, f.Y + 1);
                        int innerW = Math.Max(1, f.Width - 2);
                        int innerH = Math.Max(1, f.Height - 2);
                        var vp = new Viewport(_cameraX, _cameraY, innerW, innerH);
                        // Choose pixels-per-cell based on zoom
                        int maxPpc = 24;
                        int ppc = Math.Max(4, Math.Min(maxPpc, (int)Math.Round(16 / Math.Max(_zoom, 0.5))));
                        var tileSource = new SkiaTileSource();
                        var frame = TileAssembler.Assemble(tileSource, _map, vp, innerW, innerH, ppc, _zoom, biomeColors: true, rivers: true);
                        _pixelRenderer!.BeginFrame();
                        _pixelRenderer.ReplaceAndDisplayImage(7777, frame.Rgba, frame.WidthPx, frame.HeightPx, innerX, innerY, innerW, innerH);
                        _pixelRenderer.EndFrame();
                    }
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
            AppendMapSummary(logView, _map);
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

    private static void AppendMapSummary(TextView log, MapData map)
    {
        try
        {
            int cellCount = map.Cells?.Count ?? 0;
            int rivers = map.Rivers?.Count ?? 0;
            int riverCells = 0;
            if (map.Rivers != null)
            {
                var seen = new System.Collections.Generic.HashSet<int>();
                foreach (var r in map.Rivers)
                {
                    if (r?.Cells == null) continue;
                    foreach (var cid in r.Cells) seen.Add(cid);
                }
                riverCells = seen.Count;
            }
            int biomes = map.Biomes?.Count ?? 0;
            var biomeCounts = new System.Collections.Generic.Dictionary<int, int>();
            if (map.Cells != null)
            {
                foreach (var c in map.Cells)
                {
                    int b = c.Biome;
                    if (!biomeCounts.ContainsKey(b)) biomeCounts[b] = 0;
                    biomeCounts[b]++;
                }
            }
            AppendLog(log, $"[HUD] Cells={cellCount}, Rivers={rivers}, RiverCells={riverCells}, Biomes={biomes}");
            if (map.Biomes != null && map.Biomes.Count > 0)
            {
                int shown = 0;
                foreach (var kv in biomeCounts)
                {
                    if (kv.Key < 0 || kv.Key >= map.Biomes.Count) continue;
                    var name = map.Biomes[kv.Key].Name;
                    AppendLog(log, $"  {name}: {kv.Value}");
                    if (++shown >= 8) { AppendLog(log, "  ..."); break; }
                }
            }
        }
        catch { }
    }

    private static void EnsurePixelRenderer()
    {
        if (_pixelRenderer == null)
        {
            _pixelRenderer = new ITerm2GraphicsRenderer();
            _pixelTarget = new ConsoleRenderTarget(System.Console.WindowWidth, System.Console.WindowHeight);
            _pixelRenderer.Initialize(_pixelTarget);
        }
    }
}
