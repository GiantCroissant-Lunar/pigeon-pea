namespace PigeonPea.Game.Perception.Sensors;

using System;
using PigeonPea.Perception.Auditory;
using PigeonPea.Perception.Enums;
using PigeonPea.Perception.Models;
using PigeonPea.Perception.Visual;
using PigeonPea.Game.Perception.Integration;

public sealed class HearingSensor : IAuditoryPerception
{
    private readonly ISoundEventBus _bus;

    public HearingSensor()
        : this(new SoundEventBus())
    {
    }

    public HearingSensor(ISoundEventBus bus)
    {
        _bus = bus;
    }

    public AuditoryPerceptionData UpdateAuditoryPerception(
        object agentId,
        (int X, int Y) position,
        float currentTime,
        AuditoryPerceptionData? previous = null)
    {
        var auditory = previous ?? new AuditoryPerceptionData();
        auditory.HeardSounds.Clear();

        var windowSeconds = 1.0f;
        var sinceTime = currentTime - windowSeconds;

        var sounds = _bus.GetRecent(sinceTime);

        foreach (var sound in sounds)
        {
            var dx = sound.Position.X - position.X;
            var dy = sound.Position.Y - position.Y;

            var distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance > auditory.HearingRange)
            {
                continue;
            }

            if (sound.Volume < auditory.HearingThreshold)
            {
                continue;
            }

            var direction = VisibilityCheck.GetDirection(position, sound.Position);

            var perceived = new SoundEvent
            {
                Type = sound.Type,
                Position = sound.Position,
                Volume = sound.Volume,
                Distance = distance,
                Direction = direction,
                Timestamp = sound.Timestamp,
                SourceId = sound.SourceId,
            };

            auditory.HeardSounds.Add(perceived);
        }

        return auditory;
    }
}
