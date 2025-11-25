using System;
using System.Collections.Generic;
using System.Numerics;
using PigeonPea.Input.Core.Bindings;
using PigeonPea.Input.Core.Controls;
using SDL3; // Assuming SDL3-CS exposes this namespace
using Plate.SCG.General.DisposePattern.Attributes;

namespace PigeonPea.Plugin.Input.SDL3Mouse;

/// <summary>
/// SDL3 mouse device - Tier 4 provider.
/// </summary>
[DisposePattern]
public sealed partial class SDL3MouseDevice : IInputDevice
{
    public string DeviceId => "SDL3-Mouse";
    public string DeviceType => "Mouse";

    private readonly Dictionary<string, bool> _buttonStates = new(StringComparer.OrdinalIgnoreCase);
    private int _x;
    private int _y;

    public SDL3MouseDevice()
    {
        // Initialize SDL3 mouse / events subsystem
        SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
    }

    public void Update()
    {
        // Pump events and update mouse state
        SDL.SDL_PumpEvents();

        uint buttons = SDL.SDL_GetMouseState(out _x, out _y);

        _buttonStates["leftButton"] = (buttons & SDL.SDL_BUTTON_LMASK) != 0;
        _buttonStates["rightButton"] = (buttons & SDL.SDL_BUTTON_RMASK) != 0;
        _buttonStates["middleButton"] = (buttons & SDL.SDL_BUTTON_MMASK) != 0;
    }

    public bool IsControlActive(InputControlPath path)
    {
        if (!string.Equals(path.DeviceType, "Mouse", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var controlName = path.ControlName.ToLowerInvariant();

        if (controlName == "position")
        {
            // Position is exposed via ReadControlValue as a Vector2, not as an "active" button.
            return false;
        }

        return _buttonStates.TryGetValue(controlName, out var pressed) && pressed;
    }

    public InputValue ReadControlValue(InputControlPath path)
    {
        if (!string.Equals(path.DeviceType, "Mouse", StringComparison.OrdinalIgnoreCase))
        {
            return new InputValue(false);
        }

        var controlName = path.ControlName.ToLowerInvariant();

        if (controlName == "position")
        {
            // Expose raw screen coordinates as a Vector2 for future content-level handlers.
            return new InputValue(new Vector2(_x, _y));
        }

        if (_buttonStates.TryGetValue(controlName, out var pressed))
        {
            return new InputValue(pressed);
        }

        return new InputValue(false);
    }

    partial void DisposeUnmanagedResources()
    {
        SDL.SDL_Quit();
    }
}
