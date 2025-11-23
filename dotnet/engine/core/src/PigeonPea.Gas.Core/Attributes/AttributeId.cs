namespace PigeonPea.Gas.Attributes;

/// <summary>
/// Strongly-typed attribute identifier.
/// Common examples: "Health", "Mana", "Attack", "Defense", "Speed"
/// </summary>
public readonly struct AttributeId : IEquatable<AttributeId>
{
    public string Value { get; }

    public AttributeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AttributeId cannot be null or empty", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => obj is AttributeId other && Equals(other);
    public bool Equals(AttributeId other) => Value == other.Value;

    public static bool operator ==(AttributeId left, AttributeId right) => left.Equals(right);
    public static bool operator !=(AttributeId left, AttributeId right) => !left.Equals(right);

    public static implicit operator string(AttributeId id) => id.Value;
    public static implicit operator AttributeId(string value) => new(value);
}
