using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using PigeonPea.Windows.Rendering;
using SadRogue.Primitives;
using System;

namespace PigeonPea.Windows;

public partial class MainWindow : Window
{
    private readonly GameWorld _gameWorld;
    private readonly SkiaSharpRenderer _renderer;
    private readonly ParticleSystem _particleSystem;
    private readonly AnimationSystem _animationSystem;
    private readonly DispatcherTimer _gameTimer;
    private DateTime _lastUpdate = DateTime.UtcNow;
    private int _frameCount;
    private DateTime _lastFpsUpdate = DateTime.UtcNow;

    public MainWindow(SpriteAtlasManager? spriteAtlasManager = null)
    {
        InitializeComponent();

        _gameWorld = new GameWorld(80, 50);

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
            FpsText.Text = $"FPS: {_frameCount}";
            _frameCount = 0;
            _lastFpsUpdate = now;
        }

        // Update HUD
        UpdateHud();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        Point? direction = e.Key switch
        {
            Key.Up or Key.W => new Point(0, -1),
            Key.Down or Key.S => new Point(0, 1),
            Key.Left or Key.A => new Point(-1, 0),
            Key.Right or Key.D => new Point(1, 0),
            _ => null
        };

        if (direction.HasValue)
        {
            _gameWorld.TryMovePlayer(direction.Value);
            e.Handled = true;
        }
    }

    private void UpdateHud()
    {
        // Arch v2: check Health component IsAlive instead of Entity.IsAlive
        ref readonly var health = ref _gameWorld.PlayerEntity.Get<Health>();
        if (health.IsAlive)
        {
            ref readonly var pos = ref _gameWorld.PlayerEntity.Get<Position>();
            PositionText.Text = $"Pos: ({pos.Point.X}, {pos.Point.Y})";
            HealthText.Text = $"HP: {health.Current}/{health.Maximum}";
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Dispose renderer on window close
        _renderer?.Dispose();
    }
}
