namespace PigeonPea.Game.Perception.Integration;

using System.Collections.Generic;
using PigeonPea.Perception.Enums;

public interface ISoundEventBus
{
    void Emit(GlobalSoundEvent sound);

    IReadOnlyList<GlobalSoundEvent> GetRecent(float sinceTime);
}
