namespace PigeonPea.Game.Perception.Integration;

using PigeonPea.Shared.Perception.Enums;

public sealed class GlobalSoundEvent
{
    public SoundType Type { get; init; }
    public (int X, int Y) Position { get; init; }
    public float Volume { get; init; } = 1.0f;
    public float Timestamp { get; init; }
    public object? SourceId { get; init; }
}
