namespace PigeonPea.Shared.Perception.Enums;

/// <summary>
/// Represents the AI agent's current alert state based on perceived information.
/// </summary>
public enum AlertLevel
{
    /// <summary>
    /// Unaware of any threats or interesting stimuli.
    /// </summary>
    Unaware = 0,

    /// <summary>
    /// Something seems off; investigating suspicious activity.
    /// </summary>
    Suspicious = 1,

    /// <summary>
    /// Active threat detected; heightened awareness.
    /// </summary>
    Alert = 2,

    /// <summary>
    /// Previously alerted but threat is gone; returning to normal.
    /// </summary>
    Calm = 3
}
