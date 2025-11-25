namespace PigeonPea.Platform.Input.Controls;

/// <summary>
/// Type of input value stored in InputValue.
/// </summary>
public enum InputValueType
{
    /// <summary>Boolean value (button press/release)</summary>
    Button,

    /// <summary>Single float value (axis)</summary>
    Axis,

    /// <summary>2D vector value</summary>
    Vector2
}
