using System;
using System.Collections.Generic;
using PigeonPea.Input.Core.Bindings;
using PigeonPea.Input.Core.Controls;

namespace PigeonPea.Plugin.Input.UniInputSystem;

public sealed class ConsoleKeyboardDevice : IInputDevice
{
    public string DeviceId => "Console-Keyboard";
    public string DeviceType => "Keyboard";

    private readonly Dictionary<string, bool> _keyStates = new();
    private ConsoleKeyInfo? _lastKey;

    public void Update()
    {
        if (Console.KeyAvailable)
        {
            _lastKey = Console.ReadKey(intercept: true);
            UpdateKeyState(_lastKey.Value.Key, true);
        }
        else
        {
            if (_lastKey.HasValue)
            {
                UpdateKeyState(_lastKey.Value.Key, false);
                _lastKey = null;
            }
        }
    }

    public bool IsControlActive(InputControlPath path)
    {
        if (path.DeviceType != "Keyboard") return false;

        var keyName = path.ControlName.ToLowerInvariant();
        return _keyStates.TryGetValue(keyName, out var value) && value;
    }

    public InputValue ReadControlValue(InputControlPath path)
    {
        var isActive = IsControlActive(path);
        return new InputValue(isActive);
    }

    private void UpdateKeyState(ConsoleKey key, bool isPressed)
    {
        var keyName = MapConsoleKey(key);
        _keyStates[keyName] = isPressed;
    }

    private static string MapConsoleKey(ConsoleKey key)
    {
        return key switch
        {
            ConsoleKey.W => "w",
            ConsoleKey.A => "a",
            ConsoleKey.S => "s",
            ConsoleKey.D => "d",
            ConsoleKey.Spacebar => "space",
            ConsoleKey.Enter => "enter",
            ConsoleKey.Escape => "escape",
            ConsoleKey.UpArrow => "uparrow",
            ConsoleKey.DownArrow => "downarrow",
            ConsoleKey.LeftArrow => "leftarrow",
            ConsoleKey.RightArrow => "rightarrow",
            ConsoleKey.I => "i",
            ConsoleKey.E => "e",
            _ => key.ToString().ToLowerInvariant()
        };
    }
}
