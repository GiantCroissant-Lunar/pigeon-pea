using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Geometry;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Map.Rendering;
using PigeonPea.Plugin.Map.FMG;
using PigeonPea.Shared.Rendering;
using SkiaSharp;
using RenderViewport = PigeonPea.Rendering.Contracts.Viewport;
using RenderContext = PigeonPea.Rendering.Contracts.RenderContext;
using RenderCommandList = PigeonPea.Rendering.Contracts.RenderCommandList;
using RenderOptions = PigeonPea.Rendering.Contracts.RenderOptions;

namespace PigeonPea.Windows.MapHud;

public partial class MainWindow : Window
{
    private MapHudConfig _config = new();
    private IMapData? _map;
    private Viewport _viewport;
    private double _zoom;
    private int _pixelsPerCell;
    private int _mapWidth;
    private int _mapHeight;
    private bool _isRendering;
    private bool _showSettlements;
    private bool _showRivers;
    private bool _showBorders;
    private bool _showMarkers;
    private bool _showVoronoi;

    public MainWindow()
    {
        InitializeComponent();

        Opened += async (_, _) => await LoadMapAsync();
        KeyDown += async (_, e) => await HandleKeyAsync(e);
    }

    private async Task LoadMapAsync()
    {
        try
        {
            // Load basic viewport/render configuration from MapHud.json so
            // cols/rows/zoom etc. can be tweaked without recompiling.
            _config = LoadConfig();

            // Simple demo bounds and viewport
            var bounds = new BoundingBox(0, 0, _config.MapWidth, _config.MapHeight);

            // Match FmgMapProvider.CreateSettingsFromBounds so our camera math
            // lines up with the generated FMG map dimensions.
            _mapWidth = Math.Max(1024, _config.MapWidth);
            _mapHeight = Math.Max(1024, _config.MapHeight);

            int cols = _config.Cols;
            int rows = _config.Rows;
            _zoom = _config.Zoom;
            _pixelsPerCell = _config.PixelsPerCell;
            _showSettlements = _config.ShowSettlements;
            _showRivers = _config.ShowRivers;
            _showBorders = _config.ShowBorders;
            _showMarkers = _config.ShowMarkers;
            _showVoronoi = _config.ShowVoronoi;

            // Center viewport over the map so we see land instead of only oceans.
            double viewWorldWidth = cols * _zoom;
            double viewWorldHeight = rows * _zoom;
            int originX = (int)Math.Max(0, (_mapWidth - viewWorldWidth) / 2.0);
            int originY = (int)Math.Max(0, (_mapHeight - viewWorldHeight) / 2.0);

            _viewport = new Viewport(originX, originY, cols, rows);

            // Use the existing FMG-backed map provider
            IMapGenerator generator = new FantasyMapGeneratorAdapter();
            var provider = new FmgMapProvider(generator);

            _map = await provider.GetMapAsync(bounds);

            await RenderAsync();
        }
        catch (Exception ex)
        {
            // Fallback: show a simple test pattern so we know the pipeline works
            Console.Error.WriteLine(ex);
            Title = $"Error: {ex.GetType().Name}";

            if (MapImage != null)
            {
                var testRaster = CreateTestRaster(256, 192);
                MapImage.Source = CreateBitmapFromRaster(testRaster);
            }
        }
    }

    private async Task HandleKeyAsync(KeyEventArgs e)
    {
        if (_map == null) return;

        const double zoomFactor = 0.8; // <1 zooms in, >1 zooms out
        bool changed = false;

        switch (e.Key)
        {
            case Key.Left:
            case Key.A:
                Pan(-1, 0);
                changed = true;
                break;
            case Key.Right:
            case Key.D:
                Pan(1, 0);
                changed = true;
                break;
            case Key.Up:
            case Key.W:
                Pan(0, -1);
                changed = true;
                break;
            case Key.Down:
            case Key.S:
                Pan(0, 1);
                changed = true;
                break;
            case Key.Add:
            case Key.OemPlus:
            case Key.Z:
                ZoomByFactor(zoomFactor);
                changed = true;
                break;
            case Key.Subtract:
            case Key.OemMinus:
            case Key.X:
                ZoomByFactor(1.0 / zoomFactor);
                changed = true;
                break;
            case Key.R:
                _showRivers = !_showRivers;
                changed = true;
                break;
            case Key.B:
                _showBorders = !_showBorders;
                changed = true;
                break;
            case Key.C:
                _showSettlements = !_showSettlements;
                changed = true;
                break;
            case Key.M:
                _showMarkers = !_showMarkers;
                changed = true;
                break;
            case Key.V:
                _showVoronoi = !_showVoronoi;
                changed = true;
                break;
        }

        if (changed)
        {
            await RenderAsync();
        }
    }

    private void Pan(int dxSign, int dySign)
    {
        // Move by a fraction of the current view size in world units
        double stepWorldX = _viewport.Width * _zoom * 0.2;
        double stepWorldY = _viewport.Height * _zoom * 0.2;

        double newX = _viewport.X + dxSign * stepWorldX;
        double newY = _viewport.Y + dySign * stepWorldY;

        ClampViewport(ref newX, ref newY);
        _viewport = new Viewport((int)Math.Round(newX), (int)Math.Round(newY), _viewport.Width, _viewport.Height);
    }

    private void ZoomByFactor(double factor)
    {
        double newZoom = _zoom * factor;
        newZoom = Math.Clamp(newZoom, 0.25, 8.0);

        // Keep the same center while changing zoom
        double centerX = _viewport.X + _viewport.Width * _zoom / 2.0;
        double centerY = _viewport.Y + _viewport.Height * _zoom / 2.0;

        _zoom = newZoom;

        double viewWorldWidth = _viewport.Width * _zoom;
        double viewWorldHeight = _viewport.Height * _zoom;
        double newX = centerX - viewWorldWidth / 2.0;
        double newY = centerY - viewWorldHeight / 2.0;

        ClampViewport(ref newX, ref newY);
        _viewport = new Viewport((int)Math.Round(newX), (int)Math.Round(newY), _viewport.Width, _viewport.Height);
    }

    private void ClampViewport(ref double x, ref double y)
    {
        double viewWorldWidth = _viewport.Width * _zoom;
        double viewWorldHeight = _viewport.Height * _zoom;

        double maxX = Math.Max(0, _mapWidth - viewWorldWidth);
        double maxY = Math.Max(0, _mapHeight - viewWorldHeight);

        x = Math.Clamp(x, 0, maxX);
        y = Math.Clamp(y, 0, maxY);
    }

    private async Task RenderAsync()
    {
        if (_map == null || _isRendering)
        {
            return;
        }

        _isRendering = true;
        try
        {
            var map = _map;
            var viewport = _viewport;
            var zoom = _zoom;
            var ppc = _pixelsPerCell;

            int widthPx = Math.Max(1, viewport.Width * ppc);
            int heightPx = Math.Max(1, viewport.Height * ppc);

            // Use the new SkiaSharpBackend
            var renderContext = new RenderContext(widthPx, heightPx);
            
            using var backend = new SkiaSharpBackend(renderContext);
            
            var settings = new WorldMapRenderSettings
            {
                ColorScheme = ColorScheme.Original,
                ShowSettlements = _showSettlements,
                ShowRivers = _showRivers,
                ShowBorders = _showBorders,
                ShowMarkers = _showMarkers,
                PixelsPerCell = ppc,
                ShowVoronoiOutlines = _showVoronoi
            };

            var renderViewport = new RenderViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
            var options = new RenderOptions(renderViewport, zoom, settings, false, false);
            var renderer = new WorldMapDomainRenderer();

            await Task.Run(() =>
            {
                renderer.Render(map, backend.Commands, options, settings);
                backend.Execute(backend.Commands);
            });

            using var image = backend.Snapshot();
            if (image == null) return;

            // Convert SKImage to Avalonia Bitmap
            var bitmap = CreateBitmapFromSkImage(image);

            if (MapImage != null)
            {
                MapImage.Source = bitmap;
            }

            if (HudText != null)
            {
                HudText.Text =
                    $"Zoom: {_zoom:0.00} | View: ({_viewport.X}, {_viewport.Y}) | Map: {_mapWidth}x{_mapHeight} | " +
                    $"Set:{OnOff(_showSettlements)} Riv:{OnOff(_showRivers)} Bord:{OnOff(_showBorders)} Mark:{OnOff(_showMarkers)} Vor:{OnOff(_showVoronoi)}";
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            Title = $"Error: {ex.GetType().Name} - {ex.Message}";
        }
        finally
        {
            _isRendering = false;
        }
    }

    private static Bitmap CreateBitmapFromSkImage(SKImage image)
    {
        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var skBitmap = new SKBitmap(info);
        if (image.ReadPixels(info, skBitmap.GetPixels(), info.RowBytes, 0, 0))
        {
            var size = new PixelSize(image.Width, image.Height);
            var dpi = new Vector(96, 96);
            var writeableBitmap = new WriteableBitmap(size, dpi, PixelFormat.Rgba8888, AlphaFormat.Premul);

            using (var fb = writeableBitmap.Lock())
            {
                unsafe
                {
                    var src = skBitmap.GetPixels();
                    var dst = fb.Address;
                    Buffer.MemoryCopy((void*)src, (void*)dst, skBitmap.ByteCount, skBitmap.ByteCount);
                }
            }
            return writeableBitmap;
        }
        return null!;
    }

    private static Bitmap CreateBitmapFromRaster(SkiaMapRasterizer.Raster raster)
    {
        var size = new PixelSize(raster.WidthPx, raster.HeightPx);
        var dpi = new Vector(96, 96);
        var bitmap = new WriteableBitmap(size, dpi, PixelFormat.Rgba8888, AlphaFormat.Premul);

        using (var fb = bitmap.Lock())
        {
            Marshal.Copy(raster.Rgba, 0, fb.Address, raster.Rgba.Length);
        }

        return bitmap;
    }

    private static MapHudConfig LoadConfig()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "MapHud.json");
            if (!File.Exists(path))
            {
                return new MapHudConfig();
            }

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var cfg = JsonSerializer.Deserialize<MapHudConfig>(json, options);
            return cfg ?? new MapHudConfig();
        }
        catch
        {
            return new MapHudConfig();
        }
    }

    private sealed class MapHudConfig
    {
        public int Cols { get; set; } = 256;
        public int Rows { get; set; } = 192;
        public double Zoom { get; set; } = 4.0;
        public int PixelsPerCell { get; set; } = 2;
        public int MapWidth { get; set; } = 2048;
        public int MapHeight { get; set; } = 2048;
        public bool ShowSettlements { get; set; } = true;
        public bool ShowRivers { get; set; } = true;
        public bool ShowBorders { get; set; } = false;
        public bool ShowMarkers { get; set; } = true;
        public bool ShowVoronoi { get; set; } = false;
    }

    private static SkiaMapRasterizer.Raster CreateTestRaster(int width, int height)
    {
        var rgba = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = (y * width + x) * 4;
                byte r = (byte)(x * 255 / Math.Max(1, width - 1));
                byte g = (byte)(y * 255 / Math.Max(1, height - 1));
                byte b = 128;
                rgba[idx] = r;
                rgba[idx + 1] = g;
                rgba[idx + 2] = b;
                rgba[idx + 3] = 255;
            }
        }

        return new SkiaMapRasterizer.Raster(rgba, width, height);
    }

    private static string OnOff(bool value) => value ? "on" : "off";

    private static void OverlayRiversWithSkia(
        SkiaMapRasterizer.Raster raster,
        IMapData map,
        Viewport viewport,
        double zoom,
        int pixelsPerCell)
    {
        if (raster.Rgba.Length == 0 || raster.WidthPx <= 0 || raster.HeightPx <= 0)
        {
            return;
        }

        var info = new SKImageInfo(raster.WidthPx, raster.HeightPx, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);

        var pixelsPtr = bitmap.GetPixels();
        if (pixelsPtr == IntPtr.Zero)
        {
            return;
        }

        Marshal.Copy(raster.Rgba, 0, pixelsPtr, raster.Rgba.Length);

        using (var canvas = new SKCanvas(bitmap))
        {
            var bounds = new BoundingBox(
                viewport.X,
                viewport.Y,
                viewport.Width * zoom,
                viewport.Height * zoom);

            var features = map.GetFeatures(bounds, new ZoomLevel(10)).ToList();

            foreach (var feature in features)
            {
                if (feature.Kind != FeatureKind.River)
                {
                    continue;
                }

                if (feature.Geometry is not LineString line || line.Points.Count < 2)
                {
                    continue;
                }

                int cellCount = 0;
                if (feature.Metadata != null && feature.Metadata.TryGetValue("cellCount", out var v))
                {
                    try
                    {
                        cellCount = Convert.ToInt32(v);
                    }
                    catch
                    {
                        cellCount = 0;
                    }
                }

                float baseWidth = cellCount switch
                {
                    >= 120 => 4f,
                    >= 60 => 3f,
                    >= 20 => 2.5f,
                    >= 5 => 2.0f,
                    _ => 1.5f
                };

                float zoomScale = zoom switch
                {
                    <= 0.4 => 3.0f,
                    <= 0.8 => 2.2f,
                    <= 1.6 => 1.6f,
                    <= 3.0 => 1.0f,
                    <= 5.0 => 0.6f,
                    _ => 0.0f
                };

                float strokeWidth = baseWidth * zoomScale;
                if (strokeWidth <= 0.1f)
                {
                    continue;
                }

                using var path = new SKPath();
                bool first = true;
                foreach (var p in line.Points)
                {
                    float sx = (float)(((p.X - viewport.X) / zoom) * pixelsPerCell);
                    float sy = (float)(((p.Y - viewport.Y) / zoom) * pixelsPerCell);
                    if (first)
                    {
                        path.MoveTo(sx, sy);
                        first = false;
                    }
                    else
                    {
                        path.LineTo(sx, sy);
                    }
                }

                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round,
                    Color = new SKColor(30, 144, 200),
                    StrokeWidth = strokeWidth
                };

                canvas.DrawPath(path, paint);
            }
        }

        var updatedPtr = bitmap.GetPixels();
        if (updatedPtr != IntPtr.Zero)
        {
            Marshal.Copy(updatedPtr, raster.Rgba, 0, raster.Rgba.Length);
        }
    }
}
