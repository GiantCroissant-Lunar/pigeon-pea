namespace PigeonPea.Game.Perception.Integration;

using System.Collections.Generic;

public sealed class SoundEventBus : ISoundEventBus
{
    private readonly List<GlobalSoundEvent> _events = new();

    public void Emit(GlobalSoundEvent sound)
    {
        _events.Add(sound);
    }

    public IReadOnlyList<GlobalSoundEvent> GetRecent(float sinceTime)
    {
        var results = new List<GlobalSoundEvent>();

        for (var i = 0; i < _events.Count; i++)
        {
            var sound = _events[i];
            if (sound.Timestamp >= sinceTime)
            {
                results.Add(sound);
            }
        }

        return results;
    }
}
