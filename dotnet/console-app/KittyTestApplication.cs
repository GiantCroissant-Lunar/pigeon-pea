using System;
using Terminal.Gui;
using PigeonPea.Console.Rendering;

namespace PigeonPea.Console;

/// <summary>
/// Terminal.Gui application that draws random Kitty images to verify graphics support.
/// </summary>
public class KittyTestApplication : Toplevel
{
    private readonly Label _info;
    private readonly Label _mode;
    private readonly KittyGraphicsRenderer _kitty;
    private readonly ConsoleRenderTarget _target;
    private readonly Random _rng = new Random();
    private bool _auto;
    private DateTime _lastDraw = DateTime.UtcNow;
    private const int ImgSize = 16;
    private const int ImageId = 424242; // reused and replaced each draw

    public KittyTestApplication()
    {
        Title = "Kitty Graphics Test";

        var cols = Math.Max(1, Application.Driver?.Cols ?? 80);
        var rows = Math.Max(1, Application.Driver?.Rows ?? 25);

        var win = new FrameView
        {
            Title = "Kitty Test",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };
        Add(win);

        var status = new FrameView
        {
            Title = "Status",
            X = 0,
            Y = Pos.Bottom(win),
            Width = Dim.Fill(),
            Height = 2
        };

        _info = new Label
        {
            Text = "Space: draw random image  |  A: auto on/off  |  Q: quit",
            X = 1,
            Y = 0,
            Width = Dim.Fill(),
        };
        _mode = new Label
        {
            Text = $"Grid: {cols}x{rows}  Img: {ImgSize}x{ImgSize}  Auto: OFF",
            X = 1,
            Y = 0,
            Width = Dim.Fill(),
        };
        status.Add(_info);
        status.Add(new Label { Text = string.Empty, X = 1, Y = 0 }); // spacer
        status.Add(_mode);
        Add(status);

        // Initialize Kitty renderer with a console render target
        _target = new ConsoleRenderTarget(cols, rows);
        _kitty = new KittyGraphicsRenderer();
        _kitty.Initialize(_target);

        // Ensure the toplevel receives key events
        CanFocus = true;
        SetFocus();
        KeyDown += OnKeyDown;

        // Auto draw timer (~10 Hz when enabled)
        Application.AddTimeout(TimeSpan.FromMilliseconds(100), () =>
        {
            if (_auto && (DateTime.UtcNow - _lastDraw).TotalMilliseconds >= 90)
            {
                DrawRandomImage();
                _lastDraw = DateTime.UtcNow;
            }
            return true;
        });
    }

    private void OnKeyDown(object? sender, Key e)
    {
        switch (e.KeyCode)
        {
            case KeyCode.Space:
                DrawRandomImage();
                break;
            case KeyCode.A:
                _auto = !_auto;
                _mode.Text = ToggleAutoText(_auto);
                break;
            case KeyCode.Q:
                RequestStop();
                break;
        }
    }

    private string ToggleAutoText(bool on)
    {
        var cols = Math.Max(1, Application.Driver?.Cols ?? 80);
        var rows = Math.Max(1, Application.Driver?.Rows ?? 25);
        return $"Grid: {cols}x{rows}  Img: {ImgSize}x{ImgSize}  Auto: {(on ? "ON" : "OFF")}";
    }

    private void DrawRandomImage()
    {
        var cols = Math.Max(1, Application.Driver?.Cols ?? 80);
        var rows = Math.Max(1, (Application.Driver?.Rows ?? 25) - 1); // leave bottom row for status

        // Random cell position within the visible grid
        int x = _rng.Next(0, Math.Max(1, cols - 1));
        int y = _rng.Next(0, Math.Max(1, rows - 2));

        // Generate a simple RGBA pattern (gradient + noise)
        var rgba = new byte[ImgSize * ImgSize * 4];
        for (int py = 0; py < ImgSize; py++)
            for (int px = 0; px < ImgSize; px++)
            {
                int idx = (py * ImgSize + px) * 4;
                byte r = (byte)((px * 255) / (ImgSize - 1));
                byte g = (byte)((py * 255) / (ImgSize - 1));
                byte b = (byte)_rng.Next(60, 220);
                rgba[idx] = r; rgba[idx + 1] = g; rgba[idx + 2] = b; rgba[idx + 3] = 255;
            }

        // Replace or create and display at a random grid cell (1x1 cell area)
        _kitty.ReplaceAndDisplayImage(ImageId, rgba, ImgSize, ImgSize, x, y, 1, 1);
    }
}
