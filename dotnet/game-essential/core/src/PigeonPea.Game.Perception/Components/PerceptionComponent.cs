using PigeonPea.Perception.Models;

namespace PigeonPea.Game.Perception.Components;

/// <summary>
/// ECS component that stores perception data for an AI agent.
/// This component is attached to entities that can perceive the world (e.g., monsters, NPCs).
/// </summary>
public struct PerceptionComponent
{
    /// <summary>
    /// The aggregated perception data for this entity.
    /// Contains visual, auditory, knowledge, and awareness information.
    /// </summary>
    public PerceptionData Data { get; set; }

    /// <summary>
    /// Initializes a new perception component with default perception data.
    /// </summary>
    public PerceptionComponent()
    {
        Data = new PerceptionData();
    }

    /// <summary>
    /// Initializes a new perception component with specified perception data.
    /// </summary>
    /// <param name="data">Initial perception data.</param>
    public PerceptionComponent(PerceptionData data)
    {
        Data = data;
    }
}
