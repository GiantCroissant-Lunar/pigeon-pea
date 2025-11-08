using Terminal.Gui;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
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
    private readonly System.Timers.Timer _gameTimer;
    private DateTime _lastUpdate = DateTime.UtcNow;
    private int _frameCount;
    private DateTime _lastFpsUpdate = DateTime.UtcNow;

    public GameApplication(TerminalCapabilities terminalCaps)
    {
        _terminalCaps = terminalCaps;
        _gameWorld = new GameWorld(80, 40);

        // Main game view (80x40 grid)
        _gameView = new GameView(_gameWorld, terminalCaps)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 3
        };
        Add(_gameView);

        // Status bar at bottom
        var statusBar = new FrameView("Status")
        {
            X = 0,
            Y = Pos.Bottom(_gameView),
            Width = Dim.Fill(),
            Height = 3
        };

        _healthLabel = new Label("HP: 100/100") { X = 1, Y = 0 };
        _positionLabel = new Label("Pos: (0, 0)") { X = 20, Y = 0 };
        _fpsLabel = new Label("FPS: 0") { X = 40, Y = 0 };

        statusBar.Add(_healthLabel, _positionLabel, _fpsLabel);
        Add(statusBar);

        // Handle keyboard input
        KeyPress += OnKeyPress;

        // Game loop timer
        _gameTimer = new System.Timers.Timer(16); // ~60 FPS
        _gameTimer.Elapsed += OnGameTick;
        _gameTimer.Start();
    }

    private void OnGameTick(object? sender, System.Timers.ElapsedEventArgs e)
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
            Application.MainLoop.Invoke(() => _fpsLabel.Text = $"FPS: {_frameCount}");
            _frameCount = 0;
            _lastFpsUpdate = now;
        }

        // Update HUD and redraw
        Application.MainLoop.Invoke(() =>
        {
            UpdateHud();
            _gameView.SetNeedsDisplay();
        });
    }

    private void OnKeyPress(KeyEventEventArgs e)
    {
        Direction? direction = e.KeyEvent.Key switch
        {
            Key.CursorUp or Key.w => Direction.Up,
            Key.CursorDown or Key.s => Direction.Down,
            Key.CursorLeft or Key.a => Direction.Left,
            Key.CursorRight or Key.d => Direction.Right,
            Key.q => null, // Will quit via Terminal.Gui default
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
            _positionLabel.Text = $"Pos: ({pos.X}, {pos.Y})";
            // TODO: Update health from entity
        }
    }
}
