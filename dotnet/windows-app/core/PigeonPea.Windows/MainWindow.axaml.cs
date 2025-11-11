using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.ViewModels;
using PigeonPea.Windows.Rendering;
using System;

namespace PigeonPea.Windows;

public partial class MainWindow : Window, IDisposable
{
    private readonly GameWorld _gameWorld;
    private readonly SkiaSharpRenderer _renderer;
    private readonly ParticleSystem _particleSystem;
    private readonly AnimationSystem _animationSystem;
    private readonly GameViewModel _gameViewModel;
    private readonly DispatcherTimer _gameTimer;
    private DateTime _lastUpdate = DateTime.UtcNow;
    private int _frameCount;
    private DateTime _lastFpsUpdate = DateTime.UtcNow;
    private bool _disposed;

    // Parameterless constructor for Avalonia XAML loader (AVLN3001)
    public MainWindow() : this(null)
    {
    }

    public MainWindow(SpriteAtlasManager? spriteAtlasManager = null)
    {
        InitializeComponent();

        _gameWorld = new GameWorld(80, 50);

        // Get services from App
        var app = (App)Application.Current!;
        var services = app.Services ?? throw new InvalidOperationException("Services not initialized");

        // Initialize GameViewModel and set as DataContext
        _gameViewModel = new GameViewModel(_gameWorld, services);
        DataContext = _gameViewModel;

        // Initialize rendering systems
        _renderer = new SkiaSharpRenderer();
        _particleSystem = new ParticleSystem(1000);
        _animationSystem = new AnimationSystem();

        // Set sprite atlas manager on renderer if provided
        if (spriteAtlasManager != null)
        {
            _renderer.SetSpriteAtlasManager(spriteAtlasManager);
        }

        // Initialize game canvas with renderer and systems
        GameCanvas.Initialize(_gameWorld, _renderer, _particleSystem, _animationSystem);

        // Setup game loop
        _gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _gameTimer.Tick += OnGameTick;
        _gameTimer.Start();

        // Handle keyboard input
        KeyDown += OnKeyDown;

        // Focus for keyboard input
        Loaded += (s, e) => Focus();
    }

    private void OnGameTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        // Update game logic
        _gameWorld.Update(deltaTime);

        // Update rendering systems
        _particleSystem.Update((float)deltaTime);
        _animationSystem.Update((float)deltaTime);

        // Render
        GameCanvas.RenderFrame();

        // Update FPS counter
        _frameCount++;
        if ((now - _lastFpsUpdate).TotalSeconds >= 1.0)
        {
            _gameViewModel.Fps = _frameCount;
            _frameCount = 0;
            _lastFpsUpdate = now;
        }

        // Note: HUD updates now handled automatically through data bindings to GameViewModel
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        SadRogue.Primitives.Point? direction = e.Key switch
        {
            Key.Up or Key.W => new SadRogue.Primitives.Point(0, -1),
            Key.Down or Key.S => new SadRogue.Primitives.Point(0, 1),
            Key.Left or Key.A => new SadRogue.Primitives.Point(-1, 0),
            Key.Right or Key.D => new SadRogue.Primitives.Point(1, 0),
            _ => null
        };

        if (direction.HasValue)
        {
            _gameWorld.TryMovePlayer(direction.Value);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Disposes of resources and stops timers.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gameTimer?.Stop();
        _gameViewModel?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Dispose renderer on window close
        _renderer?.Dispose();
    }
}
