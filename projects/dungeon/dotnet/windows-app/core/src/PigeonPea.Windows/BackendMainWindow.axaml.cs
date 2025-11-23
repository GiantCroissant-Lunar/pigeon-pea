using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Plugins.Rendering.Windows.SkiaSharp;
using PigeonPea.Rendering.Contracts;
using PigeonPea.Scene.Contracts;
using PigeonPea.Game.Contracts.Services;
using SkiaSharp;

namespace PigeonPea.Windows;

/// <summary>
/// Main window using the multi-backend rendering architecture.
/// Uses SkiaSharpBackend for GPU-accelerated rendering.
/// </summary>
public partial class BackendMainWindow : Window, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BackendMainWindow> _logger;
    private readonly BackendGameLoop _gameLoop;
    private readonly SkiaSharpBackend _backend;
    private readonly DispatcherTimer _gameTimer;
    private DateTime _lastUpdate = DateTime.UtcNow;
    private int _frameCount;
    private DateTime _lastFpsUpdate = DateTime.UtcNow;
    private bool _disposed;

    // Rendering resources
    private SKBitmap? _bitmap;
    private WriteableBitmap? _writeableBitmap;
    private const int CanvasWidth = 1280;
    private const int CanvasHeight = 720;
    private const int GameWidth = 80;  // Grid width in tiles
    private const int GameHeight = 45; // Grid height in tiles

    // Parameterless constructor for Avalonia XAML loader
    public BackendMainWindow() : this(null!)
    {
    }

    public BackendMainWindow(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        InitializeComponent();

        _logger = services.GetRequiredService<ILogger<BackendMainWindow>>();

        // Initialize SkiaSharp backend
        var backendLogger = services.GetRequiredService<ILogger<SkiaSharpBackend>>();
        _backend = new SkiaSharpBackend(backendLogger);

        // Create game loop
        var sceneManager = services.GetRequiredService<ISceneManager>();
        var gameplayLoop = services.GetRequiredService<IGameplayLoop>();
        _gameLoop = new BackendGameLoop(
            services,
            _backend,
            sceneManager,
            gameplayLoop,
            GameWidth,
            GameHeight);

        // Initialize rendering resources
        InitializeRenderingResources();

        // Initialize game asynchronously
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await _gameLoop.InitializeAsync("GoRogue");
                StartGameLoop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize game");
                // Show error to user
                await MessageBox.Show(this, $"Failed to initialize game: {ex.Message}");
                Close();
            }
        });

        // Setup game timer (will be started after initialization)
        _gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _gameTimer.Tick += OnGameTick;

        // Handle keyboard input
        KeyDown += OnKeyDown;

        // Focus for keyboard input
        Loaded += (s, e) => Focus();

        _logger.LogInformation("BackendMainWindow initialized");
    }

    private void InitializeRenderingResources()
    {
        // Create bitmap for rendering
        _bitmap = new SKBitmap(CanvasWidth, CanvasHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        _writeableBitmap = new WriteableBitmap(
            new PixelSize(CanvasWidth, CanvasHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        // Set as canvas source
        var canvas = this.FindControl<Image>("GameCanvas");
        if (canvas != null)
        {
            canvas.Source = _writeableBitmap;
        }

        _logger.LogInformation("Rendering resources initialized: {Width}x{Height}", CanvasWidth, CanvasHeight);
    }

    private void StartGameLoop()
    {
        _gameTimer.Start();
        _logger.LogInformation("Game loop started");
    }

    private void OnGameTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var deltaTime = (float)(now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        try
        {
            // Update game logic
            _gameLoop.Update(deltaTime);

            // Render to bitmap
            _gameLoop.Render();

            // Copy to Avalonia image (this needs integration with backend)
            UpdateCanvas();

            // Update FPS counter
            _frameCount++;
            if ((now - _lastFpsUpdate).TotalSeconds >= 1.0)
            {
                UpdateFps(_frameCount);
                _frameCount = 0;
                _lastFpsUpdate = now;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in game tick");
        }
    }

    private unsafe void UpdateCanvas()
    {
        if (_bitmap == null || _writeableBitmap == null)
        {
            return;
        }

        try
        {
            // Get the rendered surface from the backend
            var surface = _backend.GetSurface();
            if (surface == null)
            {
                return;
            }

            // Create a snapshot of the surface
            using var image = surface.Snapshot();
            using var peekPixels = image.PeekPixels();

            if (peekPixels != null)
            {
                // Copy pixels to our bitmap
                var pixelSpan = peekPixels.GetPixelSpan();
                fixed (byte* srcPtr = pixelSpan)
                {
                    var src = (IntPtr)srcPtr;
                    using var framebuffer = _writeableBitmap.Lock();
                    var dst = framebuffer.Address;
                    var size = CanvasWidth * CanvasHeight * 4; // 4 bytes per pixel (BGRA)
                    Buffer.MemoryCopy(srcPtr, dst.ToPointer(), size, size);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating canvas");
        }
    }

    private void UpdateFps(int fps)
    {
        var fpsText = this.FindControl<TextBlock>("FpsText");
        if (fpsText != null)
        {
            fpsText.Text = fps.ToString();
        }
    }

    private void UpdatePosition(int x, int y)
    {
        var posText = this.FindControl<TextBlock>("PositionText");
        if (posText != null)
        {
            posText.Text = $"({x}, {y})";
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        // Movement keys will be handled by the game loop / input system
        // For now, just mark as handled for WASD and arrow keys
        if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right or
            Key.W or Key.A or Key.S or Key.D)
        {
            e.Handled = true;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gameTimer?.Stop();
        _gameLoop?.Stop();
        _gameLoop?.Shutdown();
        _bitmap?.Dispose();
        _writeableBitmap?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("BackendMainWindow disposed");
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Dispose();
    }
}
