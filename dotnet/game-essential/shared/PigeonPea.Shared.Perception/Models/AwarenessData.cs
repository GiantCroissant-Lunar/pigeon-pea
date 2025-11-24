using PigeonPea.Shared.Perception.Enums;

namespace PigeonPea.Shared.Perception.Models;

/// <summary>
/// Aggregates awareness state information for an AI agent.
/// Represents the agent's current alert level, threat assessment, and emotional state.
/// </summary>
public sealed class AwarenessData
{
    /// <summary>
    /// Current alert level of the agent.
    /// </summary>
    public AlertLevel AlertLevel { get; set; } = AlertLevel.Unaware;

    /// <summary>
    /// Assessed threat level based on perceived dangers.
    /// </summary>
    public ThreatLevel ThreatLevel { get; set; } = ThreatLevel.None;

    /// <summary>
    /// Current emotional state of the agent.
    /// </summary>
    public EmotionalState EmotionalState { get; set; } = EmotionalState.Neutral;

    /// <summary>
    /// Time when the agent last saw/detected a threat.
    /// Used to determine when to return to calm/unaware state.
    /// </summary>
    public float? LastThreatTime { get; set; }

    /// <summary>
    /// Time since the agent last saw/detected a threat.
    /// </summary>
    /// <param name="currentTime">Current game time.</param>
    /// <returns>Time elapsed since last threat, or null if never threatened.</returns>
    public float? GetTimeSinceLastThreat(float currentTime)
    {
        return LastThreatTime.HasValue ? currentTime - LastThreatTime.Value : null;
    }

    /// <summary>
    /// Primary target the agent is currently focused on (if any).
    /// </summary>
    public object? PrimaryTarget { get; set; }

    /// <summary>
    /// Position of the primary target (if known).
    /// </summary>
    public (int X, int Y)? PrimaryTargetPosition { get; set; }

    /// <summary>
    /// Position where the agent last detected suspicious activity.
    /// Used when investigating or searching.
    /// </summary>
    public (int X, int Y)? SuspiciousPosition { get; set; }

    /// <summary>
    /// Timestamp of the last alert state change.
    /// </summary>
    public float LastAlertChangeTime { get; set; }

    /// <summary>
    /// Checks if the agent is currently alert to threats.
    /// </summary>
    /// <returns>True if alert level is Alert.</returns>
    public bool IsAlert()
    {
        return AlertLevel == AlertLevel.Alert;
    }

    /// <summary>
    /// Checks if the agent is suspicious of something.
    /// </summary>
    /// <returns>True if alert level is Suspicious.</returns>
    public bool IsSuspicious()
    {
        return AlertLevel == AlertLevel.Suspicious;
    }

    /// <summary>
    /// Checks if the agent is unaware of threats.
    /// </summary>
    /// <returns>True if alert level is Unaware.</returns>
    public bool IsUnaware()
    {
        return AlertLevel == AlertLevel.Unaware;
    }

    /// <summary>
    /// Checks if the agent is in a calm state (recovering from alert).
    /// </summary>
    /// <returns>True if alert level is Calm.</returns>
    public bool IsCalm()
    {
        return AlertLevel == AlertLevel.Calm;
    }

    /// <summary>
    /// Checks if the agent has a high or critical threat level.
    /// </summary>
    /// <returns>True if threat level is High or Critical.</returns>
    public bool IsInDanger()
    {
        return ThreatLevel >= ThreatLevel.High;
    }

    /// <summary>
    /// Sets the agent to alert state with a specific threat level.
    /// </summary>
    /// <param name="threatLevel">Threat level to set.</param>
    /// <param name="currentTime">Current game time.</param>
    public void SetAlert(ThreatLevel threatLevel, float currentTime)
    {
        AlertLevel = AlertLevel.Alert;
        ThreatLevel = threatLevel;
        LastThreatTime = currentTime;
        LastAlertChangeTime = currentTime;
    }

    /// <summary>
    /// Sets the agent to suspicious state.
    /// </summary>
    /// <param name="suspiciousPosition">Position of suspicious activity.</param>
    /// <param name="currentTime">Current game time.</param>
    public void SetSuspicious((int X, int Y) suspiciousPosition, float currentTime)
    {
        AlertLevel = AlertLevel.Suspicious;
        SuspiciousPosition = suspiciousPosition;
        LastAlertChangeTime = currentTime;
    }

    /// <summary>
    /// Sets the agent to calm state (recovering from alert).
    /// </summary>
    /// <param name="currentTime">Current game time.</param>
    public void SetCalm(float currentTime)
    {
        AlertLevel = AlertLevel.Calm;
        ThreatLevel = ThreatLevel.Low;
        LastAlertChangeTime = currentTime;
    }

    /// <summary>
    /// Sets the agent to unaware state (completely relaxed).
    /// </summary>
    /// <param name="currentTime">Current game time.</param>
    public void SetUnaware(float currentTime)
    {
        AlertLevel = AlertLevel.Unaware;
        ThreatLevel = ThreatLevel.None;
        PrimaryTarget = null;
        PrimaryTargetPosition = null;
        SuspiciousPosition = null;
        LastAlertChangeTime = currentTime;
    }
}
