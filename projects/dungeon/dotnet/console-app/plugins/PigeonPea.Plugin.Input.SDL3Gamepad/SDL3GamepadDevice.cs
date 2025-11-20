using System;
using System.Collections.Generic;
using PigeonPea.Input.Core.Bindings;
using PigeonPea.Input.Core.Controls;
using SDL3; // Assuming SDL3-CS exposes this namespace

namespace PigeonPea.Plugin.Input.SDL3Gamepad;

/// <summary>
/// SDL3 gamepad device - Tier 4 provider.
/// </summary>
public sealed class SDL3GamepadDevice : IInputDevice, IDisposable
{
    public string DeviceId => "SDL3-Gamepad";
    public string DeviceType => "Gamepad";

    private IntPtr _gamepad;
    private readonly Dictionary<string, float> _axisStates = new();
    private readonly Dictionary<string, bool> _buttonStates = new();

    public SDL3GamepadDevice()
    {
        // Initialize SDL3 gamepad subsystem
        SDL.SDL_Init(SDL.SDL_INIT_GAMEPAD);

        var numGamepads = SDL.SDL_NumJoysticks();
        if (numGamepads > 0)
        {
            _gamepad = SDL.SDL_GameControllerOpen(0);
        }
    }

    public void Update()
    {
        if (_gamepad == IntPtr.Zero)
        {
            return;
        }

        SDL.SDL_GameControllerUpdate();

        // Update axes (normalized to -1..1)
        _axisStates["leftStickX"] = SDL.SDL_GameControllerGetAxis(_gamepad, SDL.SDL_CONTROLLER_AXIS_LEFTX) / 32768f;
        _axisStates["leftStickY"] = SDL.SDL_GameControllerGetAxis(_gamepad, SDL.SDL_CONTROLLER_AXIS_LEFTY) / 32768f;
        _axisStates["rightStickX"] = SDL.SDL_GameControllerGetAxis(_gamepad, SDL.SDL_CONTROLLER_AXIS_RIGHTX) / 32768f;
        _axisStates["rightStickY"] = SDL.SDL_GameControllerGetAxis(_gamepad, SDL.SDL_CONTROLLER_AXIS_RIGHTY) / 32768f;

        // Triggers as 0..1
        _axisStates["lt"] = SDL.SDL_GameControllerGetAxis(_gamepad, SDL.SDL_CONTROLLER_AXIS_TRIGGERLEFT) / 32768f;
        _axisStates["rt"] = SDL.SDL_GameControllerGetAxis(_gamepad, SDL.SDL_CONTROLLER_AXIS_TRIGGERRIGHT) / 32768f;

        // Update buttons
        _buttonStates["a"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_A) > 0;
        _buttonStates["b"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_B) > 0;
        _buttonStates["x"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_X) > 0;
        _buttonStates["y"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_Y) > 0;
        _buttonStates["lb"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_LEFTSHOULDER) > 0;
        _buttonStates["rb"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER) > 0;
        _buttonStates["start"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_START) > 0;
        _buttonStates["back"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_BACK) > 0;

        _buttonStates["dpad_up"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_DPAD_UP) > 0;
        _buttonStates["dpad_down"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_DPAD_DOWN) > 0;
        _buttonStates["dpad_left"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_DPAD_LEFT) > 0;
        _buttonStates["dpad_right"] = SDL.SDL_GameControllerGetButton(_gamepad, SDL.SDL_CONTROLLER_BUTTON_DPAD_RIGHT) > 0;
    }

    public bool IsControlActive(InputControlPath path)
    {
        if (path.DeviceType != "Gamepad") return false;

        var controlName = path.ControlName.ToLowerInvariant();

        // Buttons
        if (_buttonStates.TryGetValue(controlName, out var buttonValue))
        {
            return buttonValue;
        }

        // Axes: consider active if magnitude > deadzone
        if (_axisStates.TryGetValue(controlName, out var axisValue))
        {
            return Math.Abs(axisValue) > 0.2f;
        }

        return false;
    }

    public InputValue ReadControlValue(InputControlPath path)
    {
        if (path.DeviceType != "Gamepad")
        {
            return new InputValue(false);
        }

        var controlName = path.ControlName.ToLowerInvariant();

        // Button
        if (_buttonStates.TryGetValue(controlName, out var buttonValue))
        {
            return new InputValue(buttonValue);
        }

        // Axis
        if (_axisStates.TryGetValue(controlName, out var axisValue))
        {
            return new InputValue(axisValue);
        }

        return new InputValue(0f);
    }

    public void Dispose()
    {
        if (_gamepad != IntPtr.Zero)
        {
            SDL.SDL_GameControllerClose(_gamepad);
            _gamepad = IntPtr.Zero;
        }

        SDL.SDL_Quit();
    }
}
