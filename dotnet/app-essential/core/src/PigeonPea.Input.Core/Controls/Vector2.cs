namespace PigeonPea.Input.Core.Controls;

/// <summary>
/// Simple 2D vector struct (to avoid external dependencies).
/// </summary>
public struct Vector2 : IEquatable<Vector2>
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);
    public static Vector2 Up => new(0, 1);
    public static Vector2 Down => new(0, -1);
    public static Vector2 Left => new(-1, 0);
    public static Vector2 Right => new(1, 0);

    public float LengthSquared => X * X + Y * Y;
    public float Length => MathF.Sqrt(LengthSquared);

    public override string ToString() => $"({X:F2}, {Y:F2})";

    public override bool Equals(object? obj) => obj is Vector2 other && Equals(other);
    public bool Equals(Vector2 other) => X == other.X && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);
    public static bool operator !=(Vector2 left, Vector2 right) => !left.Equals(right);

    public static Vector2 operator +(Vector2 left, Vector2 right) => new(left.X + right.X, left.Y + right.Y);
    public static Vector2 operator -(Vector2 left, Vector2 right) => new(left.X - right.X, left.Y - right.Y);
    public static Vector2 operator *(Vector2 vector, float scalar) => new(vector.X * scalar, vector.Y * scalar);
    public static Vector2 operator *(float scalar, Vector2 vector) => vector * scalar;
    public static Vector2 operator /(Vector2 vector, float scalar) => new(vector.X / scalar, vector.Y / scalar);

    /// <summary>
    /// Normalizes the vector to unit length.
    /// </summary>
    public Vector2 Normalized()
    {
        var length = Length;
        return length > 0 ? this / length : Zero;
    }

    /// <summary>
    /// Returns the dot product of two vectors.
    /// </summary>
    public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;

    /// <summary>
    /// Returns the distance between two vectors.
    /// </summary>
    public static float Distance(Vector2 a, Vector2 b) => (a - b).Length;
}
