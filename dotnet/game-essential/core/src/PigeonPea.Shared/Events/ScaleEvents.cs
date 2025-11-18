using PigeonPea.Shared.Scale;

namespace PigeonPea.Shared.Events;

/// <summary>
/// Event published when the active scale mode changes (e.g. World -> DungeonFine).
/// This is intended for HUDs, renderers, and chunking systems that need to
/// adapt to the current logical scale.
/// </summary>
public readonly struct ScaleModeChangedEvent
{
    public ScaleMode OldMode { get; init; }
    public ScaleMode NewMode { get; init; }

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(ScaleModeChangedEvent left, ScaleModeChangedEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScaleModeChangedEvent left, ScaleModeChangedEvent right)
    {
        return !(left == right);
    }
}
