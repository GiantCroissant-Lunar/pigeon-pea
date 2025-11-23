using System;

namespace NexusCamera2D.Math
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public float X;
        public float Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Zero => new Vector2(0, 0);
        public static Vector2 One => new Vector2(1, 1);

        public float Length()
        {
            return MathF.Sqrt(X * X + Y * Y);
        }

        public float LengthSquared()
        {
            return X * X + Y * Y;
        }

        public float Magnitude => Length();

        public Vector2 Normalized
        {
            get
            {
                var length = Length();
                if (length == 0)
                {
                    return Zero;
                }

                return new Vector2(X / length, Y / length);
            }
        }

        public static float Distance(Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            t = MathHelper.Clamp01(t);
            return new Vector2(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t
            );
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.X * d, a.Y * d);
        public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.X * d, a.Y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.X / d, a.Y / d);

        public bool Equals(Vector2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is Vector2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"{{X:{X} Y:{Y}}}";
        }
    }
}
