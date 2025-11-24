namespace PigeonPea.Game.Perception.Integration;

using PigeonPea.Shared.Perception.Enums;

public sealed class SoundEmitter
{
    private readonly ISoundEventBus _bus;

    public SoundEmitter(ISoundEventBus bus)
    {
        _bus = bus;
    }

    public void Emit(
        SoundType type,
        (int X, int Y) position,
        float timestamp,
        float volume = 1.0f,
        object? sourceId = null)
    {
        _bus.Emit(new GlobalSoundEvent
        {
            Type = type,
            Position = position,
            Volume = volume,
            Timestamp = timestamp,
            SourceId = sourceId
        });
    }

    public void EmitFootstep((int X, int Y) position, float timestamp, object? sourceId = null, float volume = 1.0f)
    {
        Emit(SoundType.Footsteps, position, timestamp, volume, sourceId);
    }

    public void EmitCombat((int X, int Y) position, float timestamp, object? sourceId = null, float volume = 1.0f)
    {
        Emit(SoundType.Combat, position, timestamp, volume, sourceId);
    }
}
