namespace PigeonPea.Game.Contracts.Stats.Models;

public sealed class StatDefinition
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public float MinValue { get; init; }

    public float MaxValue { get; init; }

    public float DefaultValue { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? Formula { get; init; }
}
