namespace PigeonPea.Shared.Perception.Auditory;

using PigeonPea.Shared.Perception.Models;

public interface IAuditoryPerception
{
    AuditoryPerceptionData UpdateAuditoryPerception(
        object agentId,
        (int X, int Y) position,
        float currentTime,
        AuditoryPerceptionData? previous = null);
}
