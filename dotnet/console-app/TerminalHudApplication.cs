using PigeonPea.Console.Rendering;
using PigeonPea.Map.Control.ViewModels;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Map.Rendering;
using PigeonPea.Shared.Rendering;
using Terminal.Gui;

namespace PigeonPea.Console;

internal static class TerminalHudApplication
{
    private enum RenderMode { Ascii, Braille, ITerm2 }
    private static RenderMode _renderMode = RenderMode.Ascii;
    private static MapData _map = null!;
    private static double _zoom = 1.0;
    private static int _cameraX = 0;
    private static int _cameraY = 0;
    private static ITerm2GraphicsRenderer? _pixelRenderer;
    private static IRenderTarget? _pixelTarget;
    private static BrailleMapPanelView? _brailleView;
    private static MapRenderViewModel? _viewModel;
    private static System.Collections.Generic.List<Burg>? _capitalBurgs;

    public static void Run()
    {
        Application.Init();
        try
        {
            var top = new Toplevel() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };

            int logRows = 10;
            var mapFrame = new FrameView { Title = "Map", X = 0, Y = 2, Width = Dim.Fill(), Height = Dim.Fill(logRows + 1) };
            var logFrame = new FrameView { Title = "Log", X = 0, Y = Pos.Bottom(mapFrame), Width = Dim.Fill(), Height = logRows };
            var logView = new TextView
            {
                ReadOnly = true,
                WordWrap = true,
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            logFrame.Add(logView);
            top.Add(mapFrame, logFrame);

            // Map setup (match ConsoleMapDemoRunner.DefaultSettings / original FMG archipelago demo)
            var settings = new MapGenerationSettings
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
            var hooks = new MapGenerationHooks
            {
                AfterPoliticalMapGenerated = map =>
                {
                    try
                    {
                        var capitals = new System.Collections.Generic.List<Burg>();
                        if (map.Burgs != null)
                        {
                            foreach (var b in map.Burgs)
                            {
                                if (b != null && b.IsCapital)
                                {
                                    capitals.Add(b);
                                }
                            }
                        }

                        _capitalBurgs = capitals;
                        AppendLog(logView, $"[HUD] Capitals detected: {capitals.Count}");
                    }
                    catch
                    {
                        // Swallow overlay diagnostics errors to avoid breaking map generation
                    }
                }
            };

            var adapter = new FantasyMapGeneratorAdapter();
            _map = adapter.Generate(settings, hooks);
            _cameraX = Math.Max(0, settings.Width / 2 - 40);
            _cameraY = Math.Max(0, settings.Height / 2 - 12);

            // Initialize ViewModel for color scheme management
            _viewModel = new MapRenderViewModel();

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

            // Braille view (initially hidden)
            _brailleView = new BrailleMapPanelView(_map)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = Dim.Fill(1),
                CameraX = _cameraX,
                CameraY = _cameraY,
                Zoom = _zoom,
                Visible = false
            };
            mapFrame.Add(_brailleView);

            // Overlays removed for Phase 3 migration; to be reintroduced via Map.Rendering overlays in a follow-up

            // Settings panel for color scheme selector
            var settingsPanel = new FrameView
            {
                Title = "Settings",
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 1
            };

            var colorSchemeLabel = new Label()
            {
                Text = "Color Scheme:",
                X = 1,
                Y = 0,
                Width = 15
            };

            var colorSchemeCombo = new ComboBox()
            {
                X = Pos.Right(colorSchemeLabel) + 1,
                Y = 0,
                Width = 20,
                Height = 1
            };

            // Populate with available color schemes
            var colorSchemeList = _viewModel!.AvailableColorSchemes.Select(s => s.ToString()).ToList();
            colorSchemeCombo.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(colorSchemeList));
            colorSchemeCombo.SelectedItem = colorSchemeList.IndexOf(_viewModel.ColorScheme.ToString());

            // Handle color scheme changes
            colorSchemeCombo.SelectedItemChanged += (sender, args) =>
            {
                if (args.Item >= 0 && _viewModel != null)
                {
                    var selectedText = colorSchemeList[args.Item];
                    if (Enum.TryParse<PigeonPea.Map.Rendering.ColorScheme>(selectedText, out var selectedScheme))
                    {
                        _viewModel.ColorScheme = selectedScheme;

                        // Update tile source color scheme
                        if (_brailleView != null)
                        {
                            _brailleView.TileSource.ColorScheme = selectedScheme;
                        }

                        // Trigger re-render
                        InvalidateActiveView();
                    }
                }
            };

            settingsPanel.Add(colorSchemeLabel, colorSchemeCombo);
            top.Add(settingsPanel);

            // Status bar (v2) using Shortcuts
            var status = new StatusBar();
            var zoomItem = new Shortcut { Title = "Zoom: 1.00", Key = Key.Empty };
            var modeItem = new Shortcut { Title = "Mode: ASCII", Key = Key.Empty };
            var schemeItem = new Shortcut { Title = $"Scheme: {_viewModel!.ColorScheme}", Key = Key.Empty };
            var brailleColorItem = new Shortcut { Title = "Braille: Mono", Key = Key.Empty };
            var overlayItem = new Shortcut { Title = "Overlays: C=On D=On", Key = Key.Empty };
            status.Add(zoomItem, modeItem, schemeItem, brailleColorItem, overlayItem);
            top.Add(status);

            bool showCapitals = true;
            bool showDungeons = true;

            void UpdateStatus()
            {
                zoomItem.Title = $"Zoom: {_zoom:F2}";
                modeItem.Title = $"Mode: {(_renderMode switch { RenderMode.Ascii => "ASCII", RenderMode.Braille => "Braille", RenderMode.ITerm2 => "iTerm2", _ => "?" })}";
                if (_viewModel != null)
                {
                    schemeItem.Title = $"Scheme: {_viewModel.ColorScheme}";
                }

                // Reflect Braille mono/color mode in the status bar
                if (_brailleView != null)
                {
                    brailleColorItem.Title = $"Braille: {(_brailleView.MonoMode ? "Mono" : "Color")}";
                }
                else
                {
                    brailleColorItem.Title = "Braille: -";
                }

                overlayItem.Title = $"Overlays: C={(showCapitals ? "On" : "Off")} D={(showDungeons ? "On" : "Off")}";
            }

            void UpdateViews()
            {
                // Sync cameras
                mapView.CameraX = _cameraX; mapView.CameraY = _cameraY; mapView.Zoom = _zoom;
                if (_brailleView != null) { _brailleView.CameraX = _cameraX; _brailleView.CameraY = _cameraY; _brailleView.Zoom = _zoom; }
                UpdateStatus();

                if (_renderMode == RenderMode.Ascii)
                {
                    if (!mapView.Visible) mapView.Visible = true;
                    if (_brailleView != null && _brailleView.Visible) _brailleView.Visible = false;
                    mapView.SetNeedsDraw();
                }
                else if (_renderMode == RenderMode.Braille)
                {
                    if (mapView.Visible) mapView.Visible = false;
                    if (_brailleView != null && !_brailleView.Visible) _brailleView.Visible = true;
                    _brailleView?.SetNeedsDraw();
                }
                else // iTerm2
                {
                    if (mapView.Visible) mapView.Visible = false;
                    if (_brailleView != null && _brailleView.Visible) _brailleView.Visible = false;
                    // Pixel view is handled by TerminalGuiRenderer's pixel path in MapDataRenderer; force a redraw
                    mapView.SetNeedsDraw();
                }
            }

            // Helper to invalidate only the active view (for animation ticks)
            void InvalidateActiveView()
            {
                if (_renderMode == RenderMode.Ascii)
                {
                    mapView.SetNeedsDraw();
                }
                else if (_renderMode == RenderMode.Braille)
                {
                    _brailleView?.SetNeedsDraw();
                }
                else // iTerm2
                {
                    // Pixel path sits behind mapView via renderer capabilities; trigger a redraw
                    mapView.SetNeedsDraw();
                }
            }

            void ApplyOverlayState()
            {
                if (_brailleView != null)
                {
                    _brailleView.TileSource.ShowCapitals = showCapitals;
                    _brailleView.TileSource.ShowDungeons = showDungeons;
                }
            }

            // Create menu after views and helpers are ready
            // Animation state (local to HUD)
            bool animEnabled = true;
            var animInterval = TimeSpan.FromMilliseconds(33); // ~30 FPS
            var animSW = System.Diagnostics.Stopwatch.StartNew();

            // Toggle item will not self-reference; fixed title
            var animItem = new MenuItem("Toggle Animations", string.Empty, () =>
            {
                animEnabled = !animEnabled;
                if (animEnabled)
                {
                    animSW.Restart();
                    Application.AddTimeout(animInterval, () =>
                    {
                        var dt = animSW.Elapsed; animSW.Restart();
                        InvalidateActiveView();
                        return animEnabled;
                    });
                }
            });

            var menu = new MenuBar();
            menu.Menus = new MenuBarItem[]
            {
                new MenuBarItem("_File", new MenuItem[]
                {
                    new MenuItem("_Regenerate", string.Empty, () => { UpdateViews(); }),
                    new MenuItem("_Quit", string.Empty, () => Application.RequestStop())
                }),
                new MenuBarItem("_View", new MenuItem[]
                {
                    new MenuItem("Zoom _In", string.Empty, () => { _zoom = Math.Max(0.1, _zoom * 0.8); UpdateViews(); }),
                    new MenuItem("Zoom _Out", string.Empty, () => { _zoom = Math.Min(16.0, _zoom * 1.25); UpdateViews(); }),
                    null,
                    new MenuItem("Renderer: _ASCII", string.Empty, () => { _renderMode = RenderMode.Ascii; UpdateViews(); }),
                    new MenuItem("Renderer: _Braille", string.Empty, () => { _renderMode = RenderMode.Braille; UpdateViews(); }),
                    new MenuItem("Renderer: _iTerm2", string.Empty, () => { _renderMode = RenderMode.ITerm2; EnsurePixelRenderer(); UpdateViews(); }),
                    null,
                    new MenuItem("Braille: _Toggle Color", string.Empty, () =>
                    {
                        if (_brailleView != null)
                        {
                            _brailleView.MonoMode = !_brailleView.MonoMode;
                            UpdateStatus();
                            InvalidateActiveView();
                        }
                    }),
                    null,
                    animItem
                }),
                new MenuBarItem("_Help", new MenuItem[]
                {
                    new MenuItem("_About", string.Empty, () => MessageBox.Query("About", "Pigeon Pea HUD", "OK"))
                })
            };
            top.Add(menu);

            // Start animation loop if enabled
            Application.AddTimeout(animInterval, () =>
            {
                var dt = animSW.Elapsed; animSW.Restart();
                InvalidateActiveView();
                return animEnabled;
            });

            Application.KeyDown += (object? sender, Key k) =>
            {
                // Camera controls
                bool cameraChanged = false;
                if (k == Key.CursorUp) { _cameraY = Math.Max(0, _cameraY - (int)Math.Ceiling(5 * _zoom)); cameraChanged = true; }
                else if (k == Key.CursorDown) { _cameraY = _cameraY + (int)Math.Ceiling(5 * _zoom); cameraChanged = true; }
                else if (k == Key.CursorLeft) { _cameraX = Math.Max(0, _cameraX - (int)Math.Ceiling(10 * _zoom)); cameraChanged = true; }
                else if (k == Key.CursorRight) { _cameraX = _cameraX + (int)Math.Ceiling(10 * _zoom); cameraChanged = true; }

                // Overlay toggles: C = capitals, D = dungeons
                else if (k == (Key)'c' || k == (Key)'C')
                {
                    showCapitals = !showCapitals;
                    ApplyOverlayState();
                    UpdateStatus();
                    InvalidateActiveView();
                    return;
                }
                else if (k == (Key)'d' || k == (Key)'D')
                {
                    showDungeons = !showDungeons;
                    ApplyOverlayState();
                    UpdateStatus();
                    InvalidateActiveView();
                    return;
                }

                if (!cameraChanged) return;
                UpdateViews();
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
            if (cellCount > 0)
            {
                // Height statistics to verify FMG port behavior
                var heights = map.Cells.Select(c => c.Height).ToArray();
                double minH = heights.Min();
                double maxH = heights.Max();
                double avgH = heights.Average();
                int land = heights.Count(h => h >= 20);
                int ocean = cellCount - land;
                AppendLog(log, $"[HUD] Height min={minH:F1}, max={maxH:F1}, avg={avgH:F1}, land={land} ({land * 100.0 / cellCount:F1}%), ocean={ocean} ({ocean * 100.0 / cellCount:F1}%)");
            }
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
            if (_capitalBurgs != null)
            {
                AppendLog(log, $"[HUD] Capitals={_capitalBurgs.Count}");
            }
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
