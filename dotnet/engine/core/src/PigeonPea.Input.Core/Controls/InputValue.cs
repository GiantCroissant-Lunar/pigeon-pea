namespace PigeonPea.Shared.Input.Controls;

/// <summary>
/// Union type for input values (float, Vector2, bool).
/// </summary>
public readonly struct InputValue : IEquatable<InputValue>
{
    private readonly object? _value;
    public InputValueType Type { get; }

    public InputValue(bool value)
    {
        _value = value;
        Type = InputValueType.Button;
    }

    public InputValue(float value)
    {
        _value = value;
        Type = InputValueType.Axis;
    }

    public InputValue(Vector2 value)
    {
        _value = value;
        Type = InputValueType.Vector2;
    }

    /// <summary>
    /// Gets the value as a boolean (button press).
    /// </summary>
    public bool AsButton() => Type == InputValueType.Button ? (bool)_value! : throw new InvalidCastException($"Cannot cast {Type} to bool");

    /// <summary>
    /// Gets the value as a float (axis).
    /// </summary>
    public float AsAxis() => Type == InputValueType.Axis ? (float)_value! : throw new InvalidCastException($"Cannot cast {Type} to float");

    /// <summary>
    /// Gets the value as a Vector2.
    /// </summary>
    public Vector2 AsVector2() => Type == InputValueType.Vector2 ? (Vector2)_value! : throw new InvalidCastException($"Cannot cast {Type} to Vector2");

    /// <summary>
    /// Gets the value as the specified type.
    /// </summary>
    public T Get<T>()
    {
        return Type switch
        {
            InputValueType.Button when typeof(T) == typeof(bool) => (T)_value!,
            InputValueType.Axis when typeof(T) == typeof(float) => (T)_value!,
            InputValueType.Vector2 when typeof(T) == typeof(Vector2) => (T)_value!,
            _ => throw new InvalidCastException($"Cannot cast {Type} to {typeof(T)}")
        };
    }

    /// <summary>
    /// Creates an InputValue from any supported type.
    /// </summary>
    public static InputValue From<T>(T value)
    {
        return value switch
        {
            bool b => new InputValue(b),
            float f => new InputValue(f),
            Vector2 v2 => new InputValue(v2),
            _ => throw new ArgumentException($"Unsupported type: {typeof(T)}")
        };
    }

    public override string ToString() => _value?.ToString() ?? "null";

    public override bool Equals(object? obj) => obj is InputValue other && Equals(other);
    public bool Equals(InputValue other) => Type == other.Type && Equals(_value, other._value);
    public override int GetHashCode() => HashCode.Combine(Type, _value);

    public static bool operator ==(InputValue left, InputValue right) => left.Equals(right);
    public static bool operator !=(InputValue left, InputValue right) => !left.Equals(right);

    // Implicit conversions from supported types
    public static implicit operator InputValue(bool value) => new(value);
    public static implicit operator InputValue(float value) => new(value);
    public static implicit operator InputValue(Vector2 value) => new(value);
}
