namespace PigeonPea.Shared.Input.Actions;

/// <summary>
/// Type of input action.
/// </summary>
public enum InputActionType
{
    /// <summary>Single press/release (e.g., Jump, Fire)</summary>
    Button,

    /// <summary>Continuous value (e.g., Move stick, Mouse delta)</summary>
    Value,

    /// <summary>Continuous pass-through (no processing)</summary>
    PassThrough
}
