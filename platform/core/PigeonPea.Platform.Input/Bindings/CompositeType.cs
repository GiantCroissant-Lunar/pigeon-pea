namespace PigeonPea.Platform.Input.Bindings;

/// <summary>
/// Type of composite binding.
/// </summary>
public enum CompositeType
{
    None,

    /// <summary>Combines 4 buttons into 2D vector (WASD)</summary>
    TwoDVector,

    /// <summary>Combines 2 buttons into 1D axis (-, +)</summary>
    OneDAxis,

    /// <summary>Button with modifier (Ctrl+C)</summary>
    ButtonWithOneModifier
}
