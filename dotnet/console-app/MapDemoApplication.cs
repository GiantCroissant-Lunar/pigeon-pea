using Terminal.Gui;
using PigeonPea.Shared.Rendering;
using PigeonPea.Map.Rendering;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;

namespace PigeonPea.Console;

/// <summary>
/// Simple application to render a FantasyMapGenerator map in the console
/// with zoom, pan, and dungeon overlay toggle.
/// </summary>
public class MapDemoApplication : Toplevel
{
    private readonly IRenderer _renderer;
    private readonly View _view;
    private MapData _map;
    private bool _showDungeon;
    private double _zoom = 1.0; // 1.0 => 1 world unit per tile
    private double _offsetX = 0;
    private double _offsetY = 0;
    private MapGenerationSettings _settings = new();

    public MapDemoApplication(IRenderer renderer)
    {
        _renderer = renderer;

        _view = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        Add(_view);

        // Init renderer
        IRenderTarget target;
        if (_renderer is TerminalGuiRenderer)
        {
            target = new TerminalGuiRenderTarget(_view);
        }
        else
        {
            target = new ConsoleRenderTarget(System.Console.WindowWidth, System.Console.WindowHeight);
        }
        _renderer.Initialize(target);

        // Generate a demo map
        _map = GenerateMap();
        // Start near map center to avoid starting on corner ocean
        _offsetX = Math.Max(0, _settings.Width / 2.0 - 40);
        _offsetY = Math.Max(0, _settings.Height / 2.0 - 12);

        // Render loop (~30 FPS)
        Application.AddTimeout(TimeSpan.FromMilliseconds(33), () =>
        {
            Draw();
            return true;
        });

        KeyDown += OnKeyDown;
    }

    private MapData GenerateMap()
    {
        _settings = new MapGenerationSettings
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

        IMapGenerator gen = new FantasyMapGeneratorAdapter();
        var map = gen.Generate(_settings);
        try
        {
            int land = map.Cells.Count(c => c.Height > 20);
            double landPct = map.Cells.Count > 0 ? land * 100.0 / map.Cells.Count : 0;
            System.Console.WriteLine($"[Diag] Land %: {landPct:F1}");
        }
        catch { }
        return map;
    }

    private void Draw()
    {
        if (_renderer is TerminalGuiRenderer tgui)
        {
            tgui.SetDriver(Driver);
        }

        _renderer.BeginFrame();
        _renderer.Clear(SadRogue.Primitives.Color.Black);

        var vp = _view.Viewport;
        var viewport = new Viewport((int)_offsetX, (int)_offsetY, vp.Width, vp.Height);

        _renderer.DrawText(0, 0, "Map demo view pending migration to Map.Rendering", SadRogue.Primitives.Color.White, SadRogue.Primitives.Color.Black);

        _renderer.EndFrame();
    }

    private void OnKeyDown(object? sender, Key e)
    {
        switch (e.KeyCode)
        {
            case KeyCode.Z:
                _zoom = Math.Max(0.25, _zoom * 0.8);
                break;
            case KeyCode.X:
                _zoom = Math.Min(8.0, _zoom * 1.25);
                break;
            case KeyCode.O:
                _showDungeon = !_showDungeon;
                break;
            case KeyCode.W:
            case KeyCode.CursorUp:
                _offsetY = Math.Max(0, _offsetY - 10 * _zoom);
                break;
            case KeyCode.S:
            case KeyCode.CursorDown:
                _offsetY = Math.Min(_settings.Height - 1, _offsetY + 10 * _zoom);
                break;
            case KeyCode.A:
            case KeyCode.CursorLeft:
                _offsetX = Math.Max(0, _offsetX - 10 * _zoom);
                break;
            case KeyCode.D:
            case KeyCode.CursorRight:
                _offsetX = Math.Min(_settings.Width - 1, _offsetX + 10 * _zoom);
                break;
        }
    }
}
