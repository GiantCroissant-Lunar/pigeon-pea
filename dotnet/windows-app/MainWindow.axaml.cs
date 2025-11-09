using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using SadRogue.Primitives;
using System;

namespace PigeonPea.Windows;

public partial class MainWindow : Window
{
    private readonly GameWorld _gameWorld;
    private readonly DispatcherTimer _gameTimer;
    private DateTime _lastUpdate = DateTime.UtcNow;
    private int _frameCount;
    private DateTime _lastFpsUpdate = DateTime.UtcNow;

    public MainWindow()
    {
        InitializeComponent();

        _gameWorld = new GameWorld(80, 50);

        // Initialize game canvas
        GameCanvas.Initialize(_gameWorld);

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

        // Render
        GameCanvas.InvalidateVisual();

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
}
