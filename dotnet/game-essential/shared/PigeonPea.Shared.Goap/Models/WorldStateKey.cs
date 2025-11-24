using System.Collections.Immutable;

namespace PigeonPea.Shared.Goap.WorldState;

/// <summary>
/// Strongly-typed world state key.
/// Common examples: "HasWeapon", "PlayerVisible", "Health", "Ammo"
/// </summary>
public readonly struct WorldStateKey : IEquatable<WorldStateKey>
{
    public string Value { get; }

    public WorldStateKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("WorldStateKey cannot be null or empty", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => obj is WorldStateKey other && Equals(other);
    public bool Equals(WorldStateKey other) => Value == other.Value;

    public static bool operator ==(WorldStateKey left, WorldStateKey right) => left.Equals(right);
    public static bool operator !=(WorldStateKey left, WorldStateKey right) => !left.Equals(right);

    public static implicit operator string(WorldStateKey key) => key.Value;
    public static implicit operator WorldStateKey(string value) => new(value);
}
