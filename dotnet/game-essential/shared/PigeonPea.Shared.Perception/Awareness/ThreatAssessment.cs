namespace PigeonPea.Shared.Perception.Awareness;

using PigeonPea.Shared.Perception.Enums;
using PigeonPea.Shared.Perception.Models;

public static class ThreatAssessment
{
    public static ThreatLevel EvaluateThreat(
        PerceptionData perception,
        ThreatAssessmentConfig config,
        float currentTime)
    {
        float score = 0f;

        var visibleEnemies = perception.Visual.GetEntitiesOfType("Enemy");
        foreach (var enemy in visibleEnemies)
        {
            var distanceFactor = 1f - MathF.Min(enemy.Distance, config.MaxEnemyDistance) / config.MaxEnemyDistance;
            score += config.VisibleEnemyWeight * distanceFactor;
        }

        var hiddenEnemyCount = perception.Knowledge.KnownEnemies.Count - visibleEnemies.Count;
        if (hiddenEnemyCount > 0)
        {
            score += hiddenEnemyCount * config.KnownEnemyWeight;
        }

        if (perception.Auditory.HeardCombat())
        {
            score += config.CombatSoundWeight;
        }

        if (perception.Auditory.HeardFootsteps())
        {
            score += config.FootstepSoundWeight;
        }

        if (score >= config.CriticalThreshold) return ThreatLevel.Critical;
        if (score >= config.HighThreshold) return ThreatLevel.High;
        if (score >= config.MediumThreshold) return ThreatLevel.Medium;
        if (score >= config.LowThreshold) return ThreatLevel.Low;
        return ThreatLevel.None;
    }

    public static void UpdateAwareness(
        PerceptionData perception,
        ThreatAssessmentConfig config,
        float currentTime)
    {
        var newThreat = EvaluateThreat(perception, config, currentTime);
        var awareness = perception.Awareness;

        if (newThreat > ThreatLevel.None)
        {
            awareness.SetAlert(newThreat, currentTime);
            return;
        }

        if (awareness.IsAlert() || awareness.IsSuspicious())
        {
            var sinceLastThreat = awareness.GetTimeSinceLastThreat(currentTime);
            if (sinceLastThreat.HasValue && sinceLastThreat.Value >= config.CalmDownDelaySeconds)
            {
                awareness.SetCalm(currentTime);
            }

            return;
        }

        if (awareness.IsCalm())
        {
            var sinceAlertChange = currentTime - awareness.LastAlertChangeTime;
            if (sinceAlertChange >= config.CalmDownDelaySeconds)
            {
                awareness.SetUnaware(currentTime);
            }
        }
    }
}
