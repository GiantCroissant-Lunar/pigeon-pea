using PigeonPea.Platform.Input.Controls;

namespace PigeonPea.Platform.Input.Events;

/// <summary>
/// Context passed to action callbacks.
/// Contains input value and metadata.
/// </summary>
public sealed class InputContext
{
    public InputValue Value { get; }
    public InputActionPhase Phase { get; }
    public double Time { get; }
    public string ActionName { get; }

    public InputContext(string actionName, InputValue value, InputActionPhase phase, double time)
    {
        ActionName = actionName;
        Value = value;
        Phase = phase;
        Time = time;
    }

    /// <summary>
    /// Reads the value as the specified type.
    /// </summary>
    public T ReadValue<T>() => Value.Get<T>();

    /// <summary>
    /// Reads the value as a boolean (button press).
    /// </summary>
    public bool ReadValueAsButton() => Value.AsButton();

    /// <summary>
    /// Reads the value as a float (axis).
    /// </summary>
    public float ReadValueAsAxis() => Value.AsAxis();

    /// <summary>
    /// Reads the value as a Vector2.
    /// </summary>
    public Vector2 ReadValueAsVector2() => Value.AsVector2();

    public override string ToString() => $"{ActionName} [{Phase}]: {Value}";
}
