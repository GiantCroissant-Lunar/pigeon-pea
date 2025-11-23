namespace PigeonPea.Perception.Visual;

using PigeonPea.Perception.Models;

public interface IVisualPerception
{
    VisualPerceptionData UpdateVisualPerception(
        object agentId,
        (int X, int Y) position,
        float currentTime,
        VisualPerceptionData? previous = null);
}
