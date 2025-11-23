namespace PigeonPea.Perception.Awareness;

public sealed class ThreatAssessmentConfig
{
    public float VisibleEnemyWeight { get; set; } = 1.0f;
    public float KnownEnemyWeight { get; set; } = 0.5f;
    public float CombatSoundWeight { get; set; } = 1.5f;
    public float FootstepSoundWeight { get; set; } = 0.5f;

    public float MaxEnemyDistance { get; set; } = 10f;
    public float MaxSoundDistance { get; set; } = 15f;

    public float LowThreshold { get; set; } = 0.5f;
    public float MediumThreshold { get; set; } = 1.5f;
    public float HighThreshold { get; set; } = 3.0f;
    public float CriticalThreshold { get; set; } = 5.0f;

    public float CalmDownDelaySeconds { get; set; } = 5f;
}
