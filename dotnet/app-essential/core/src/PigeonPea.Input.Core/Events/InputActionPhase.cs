namespace PigeonPea.Input.Core.Events;

/// <summary>
/// Lifecycle phase of an input action.
/// </summary>
public enum InputActionPhase
{
    /// <summary>Action is disabled</summary>
    Disabled,

    /// <summary>Waiting for input</summary>
    Waiting,

    /// <summary>Input started (button pressed)</summary>
    Started,

    /// <summary>Input performed (threshold met)</summary>
    Performed,

    /// <summary>Input canceled (button released)</summary>
    Canceled
}
