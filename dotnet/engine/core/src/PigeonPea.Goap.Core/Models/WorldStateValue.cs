namespace PigeonPea.Goap.WorldState;

/// <summary>
/// Union type for world state values.
/// Supports bool, int, float, string.
/// </summary>
public readonly struct WorldStateValue : IEquatable<WorldStateValue>
{
    private readonly object? _value;

    public WorldStateValueType Type { get; }

    public WorldStateValue(bool value)
    {
        _value = value;
        Type = WorldStateValueType.Bool;
    }

    public WorldStateValue(int value)
    {
        _value = value;
        Type = WorldStateValueType.Int;
    }

    public WorldStateValue(float value)
    {
        _value = value;
        Type = WorldStateValueType.Float;
    }

    public WorldStateValue(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        Type = WorldStateValueType.String;
    }

    public bool AsBool() => Type == WorldStateValueType.Bool ? (bool)_value! : throw new InvalidCastException();
    public int AsInt() => Type == WorldStateValueType.Int ? (int)_value! : throw new InvalidCastException();
    public float AsFloat() => Type == WorldStateValueType.Float ? (float)_value! : throw new InvalidCastException();
    public string AsString() => Type == WorldStateValueType.String ? (string)_value! : throw new InvalidCastException();

    public override string ToString() => _value?.ToString() ?? "null";
    public override int GetHashCode() => HashCode.Combine(_value, Type);
    public override bool Equals(object? obj) => obj is WorldStateValue other && Equals(other);

    public bool Equals(WorldStateValue other)
    {
        if (Type != other.Type) return false;
        return Type switch
        {
            WorldStateValueType.Bool => AsBool() == other.AsBool(),
            WorldStateValueType.Int => AsInt() == other.AsInt(),
            WorldStateValueType.Float => Math.Abs(AsFloat() - other.AsFloat()) < 0.0001f,
            WorldStateValueType.String => AsString() == other.AsString(),
            _ => false
        };
    }

    public static bool operator ==(WorldStateValue left, WorldStateValue right) => left.Equals(right);
    public static bool operator !=(WorldStateValue left, WorldStateValue right) => !left.Equals(right);

    public static implicit operator WorldStateValue(bool value) => new(value);
    public static implicit operator WorldStateValue(int value) => new(value);
    public static implicit operator WorldStateValue(float value) => new(value);
    public static implicit operator WorldStateValue(string value) => new(value);
}

public enum WorldStateValueType
{
    Bool,
    Int,
    Float,
    String
}
