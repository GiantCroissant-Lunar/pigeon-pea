namespace PigeonPea.Platform.Contracts.Time;

/// <summary>
/// Represents an absolute point in time in the game world.
/// </summary>
public readonly struct WorldTick : IEquatable<WorldTick>, IComparable<WorldTick>
{
    public long Value { get; }

    public WorldTick(long value)
    {
        Value = value;
    }

    public static WorldTick Zero => new(0);

    public static WorldTick operator +(WorldTick a, long ticks) => new(a.Value + ticks);
    public static WorldTick operator -(WorldTick a, long ticks) => new(a.Value - ticks);
    public static long operator -(WorldTick a, WorldTick b) => a.Value - b.Value;

    public static bool operator ==(WorldTick left, WorldTick right) => left.Equals(right);
    public static bool operator !=(WorldTick left, WorldTick right) => !left.Equals(right);
    public static bool operator <(WorldTick left, WorldTick right) => left.Value < right.Value;
    public static bool operator <=(WorldTick left, WorldTick right) => left.Value <= right.Value;
    public static bool operator >(WorldTick left, WorldTick right) => left.Value > right.Value;
    public static bool operator >=(WorldTick left, WorldTick right) => left.Value >= right.Value;

    public bool Equals(WorldTick other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is WorldTick other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(WorldTick other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();

    public static WorldTick Add(WorldTick left, WorldTick right)
    {
        throw new NotImplementedException();
    }

    public static WorldTick Subtract(WorldTick left, WorldTick right)
    {
        throw new NotImplementedException();
    }
}
