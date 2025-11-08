using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PigeonPea.Shared;
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
        Direction? direction = e.Key switch
        {
            Key.Up or Key.W => Direction.Up,
            Key.Down or Key.S => Direction.Down,
            Key.Left or Key.A => Direction.Left,
            Key.Right or Key.D => Direction.Right,
            _ => null
        };

        if (direction.HasValue && _gameWorld.Player != null)
        {
            _gameWorld.Player.Move(direction.Value);
            e.Handled = true;
        }
    }

    private void UpdateHud()
    {
        if (_gameWorld.Player != null)
        {
            var pos = _gameWorld.Player.GetPosition();
            PositionText.Text = $"Pos: ({pos.X}, {pos.Y})";

            // TODO: Update health display from player entity
        }
    }
}
