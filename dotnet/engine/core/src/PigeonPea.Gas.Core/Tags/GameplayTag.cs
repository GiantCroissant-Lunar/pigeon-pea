using System;

namespace PigeonPea.Gas.Tags;

/// <summary>
/// Hierarchical tag using dotted notation (e.g., "State.Movement.Stunned").
/// Parent tags: "State" is parent of "State.Movement"
/// Child tags: "State.Movement.Stunned" is child of "State.Movement"
/// </summary>
public readonly struct GameplayTag : IEquatable<GameplayTag>
{
    public string Value { get; }

    /// <summary>
    /// Gets the tag segments (e.g., ["State", "Movement", "Stunned"])
    /// </summary>
    public string[] Segments => Value.Split('.');

    /// <summary>
    /// Gets the parent tag (e.g., "State.Movement" for "State.Movement.Stunned")
    /// Returns null if this is a root tag.
    /// </summary>
    public GameplayTag? Parent
    {
        get
        {
            var lastDot = Value.LastIndexOf('.');
            if (lastDot == -1) return null;
            return new GameplayTag(Value[..lastDot]);
        }
    }

    public int Depth => Segments.Length;

    public GameplayTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("GameplayTag cannot be null or empty", nameof(value));
        if (value.Contains(".."))
            throw new ArgumentException("GameplayTag cannot contain consecutive dots", nameof(value));
        if (value.StartsWith('.') || value.EndsWith('.'))
            throw new ArgumentException("GameplayTag cannot start or end with a dot", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Checks if this tag is an ancestor of another tag.
    /// Example: "State.Movement" is ancestor of "State.Movement.Stunned"
    /// </summary>
    public bool IsAncestorOf(GameplayTag other)
    {
        return other.Value.StartsWith(Value + ".", StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks if this tag is a descendant of another tag.
    /// Example: "State.Movement.Stunned" is descendant of "State.Movement"
    /// </summary>
    public bool IsDescendantOf(GameplayTag other)
    {
        return other.IsAncestorOf(this);
    }

    public override string ToString() => Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => obj is GameplayTag other && Equals(other);
    public bool Equals(GameplayTag other) => Value == other.Value;

    public static bool operator ==(GameplayTag left, GameplayTag right) => left.Equals(right);
    public static bool operator !=(GameplayTag left, GameplayTag right) => !left.Equals(right);

    public static implicit operator string(GameplayTag tag) => tag.Value;
    public static implicit operator GameplayTag(string value) => new(value);
}
