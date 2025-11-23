namespace PigeonPea.Perception.Auditory;

using PigeonPea.Perception.Models;

public interface IAuditoryPerception
{
    AuditoryPerceptionData UpdateAuditoryPerception(
        object agentId,
        (int X, int Y) position,
        float currentTime,
        AuditoryPerceptionData? previous = null);
}
