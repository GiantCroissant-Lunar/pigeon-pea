using Terminal.Gui;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using PigeonPea.Console.Rendering;
using SadRogue.Primitives;
using System;
using System.Threading;

namespace PigeonPea.Console;

/// <summary>
/// Main Terminal.Gui application for the console version.
/// </summary>
public class GameApplication : Toplevel
{
    private readonly GameWorld _gameWorld;
    private readonly TerminalCapabilities _terminalCaps;
    private readonly GameView _gameView;
    private readonly Label _healthLabel;
    private readonly Label _positionLabel;
    private readonly Label _fpsLabel;
    // Scheduled UI timer (via Terminal.Gui timeout)
    private DateTime _lastUpdate = DateTime.UtcNow;
    private int _frameCount;
    private DateTime _lastFpsUpdate = DateTime.UtcNow;

    /// <summary>
    /// Gets the game world instance for external access (e.g., DevTools).
    /// </summary>
    public GameWorld GameWorld => _gameWorld;

    public GameApplication(TerminalCapabilities terminalCaps, PigeonPea.Shared.Rendering.IRenderer renderer)
    {
        _terminalCaps = terminalCaps;
        _gameWorld = new GameWorld(80, 40);

        // Main game view (80x40 grid)
        _gameView = new GameView(_gameWorld, renderer)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 3
        };
        Add(_gameView);

        // Status bar at bottom
        var statusBar = new FrameView
        {
            Title = "Status",
            X = 0,
            Y = Pos.Bottom(_gameView),
            Width = Dim.Fill(),
            Height = 3
        };

        _healthLabel = new Label { Text = "HP: 100/100", X = 1, Y = 0 };
        _positionLabel = new Label { Text = "Pos: (0, 0)", X = 20, Y = 0 };
        _fpsLabel = new Label { Text = "FPS: 0", X = 40, Y = 0 };

        statusBar.Add(_healthLabel, _positionLabel, _fpsLabel);
        Add(statusBar);

        // Handle keyboard input
        KeyDown += OnKeyDown;

        // Schedule game loop at ~60 FPS using Terminal.Gui timeout
        Application.AddTimeout(TimeSpan.FromMilliseconds(16), () =>
        {
            OnGameTick();
            return true; // keep repeating
        });
    }

    private void OnGameTick()
    {
        var now = DateTime.UtcNow;
        var deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        // Update game logic
        _gameWorld.Update(deltaTime);

        // Update FPS
        _frameCount++;
        if ((now - _lastFpsUpdate).TotalSeconds >= 1.0)
        {
            _fpsLabel.Text = $"FPS: {_frameCount}";
            _frameCount = 0;
            _lastFpsUpdate = now;
        }

        // Update HUD and redraw on UI thread
        UpdateHud();
        _gameView.SetNeedsDraw();
    }

    private void OnKeyDown(object? sender, Key e)
    {
        Point? direction = e.KeyCode switch
        {
            KeyCode.CursorUp or KeyCode.W => new Point(0, -1),
            KeyCode.CursorDown or KeyCode.S => new Point(0, 1),
            KeyCode.CursorLeft or KeyCode.A => new Point(-1, 0),
            KeyCode.CursorRight or KeyCode.D => new Point(1, 0),
            KeyCode.Q => null, // Will quit via Terminal.Gui default
            _ => null
        };

        if (direction.HasValue)
        {
            _gameWorld.TryMovePlayer(direction.Value);
        }
    }

    private void UpdateHud()
    {
        // Arch v2: check Health component's IsAlive instead of Entity.IsAlive extension
        ref readonly var health = ref _gameWorld.PlayerEntity.Get<Health>();
        if (health.IsAlive)
        {
            ref readonly var pos = ref _gameWorld.PlayerEntity.Get<Position>();
            _positionLabel.Text = $"Pos: ({pos.Point.X}, {pos.Point.Y})";
            _healthLabel.Text = $"HP: {health.Current}/{health.Maximum}";
        }
    }
}
