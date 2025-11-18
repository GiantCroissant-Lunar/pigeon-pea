using PigeonPea.Shared.Input.Bindings;

namespace PigeonPea.Shared.Input.Controls;

/// <summary>
/// Platform-agnostic input device interface.
/// Implemented per-platform (Console, Avalonia, SDL, etc.).
/// </summary>
public interface IInputDevice
{
    string DeviceId { get; }
    string DeviceType { get; } // "Keyboard", "Mouse", "Gamepad"

    /// <summary>
    /// Checks if a control is currently active/pressed.
    /// </summary>
    bool IsControlActive(InputControlPath path);

    /// <summary>
    /// Reads the current value of a control.
    /// </summary>
    InputValue ReadControlValue(InputControlPath path);

    /// <summary>
    /// Updates device state (called each frame).
    /// </summary>
    void Update();
}
