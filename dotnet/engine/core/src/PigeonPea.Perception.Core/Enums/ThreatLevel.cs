namespace PigeonPea.Perception.Enums;

/// <summary>
/// Represents the assessed threat level based on perceived dangers.
/// </summary>
public enum ThreatLevel
{
    /// <summary>
    /// No threats detected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Minor threat detected (e.g., weak enemy).
    /// </summary>
    Low = 1,

    /// <summary>
    /// Moderate threat detected (e.g., equal-strength enemy).
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Serious threat detected (e.g., stronger enemy or multiple enemies).
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical threat detected (e.g., overwhelming force).
    /// </summary>
    Critical = 4
}
