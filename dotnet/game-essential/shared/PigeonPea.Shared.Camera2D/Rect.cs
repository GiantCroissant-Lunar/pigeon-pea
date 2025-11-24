namespace PigeonPea.Shared.Camera2D.Math
{
    public struct Rect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public float Left => X;
        public float Right => X + Width;
        public float Top => Y;
        public float Bottom => Y + Height;

        public Rect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Vector2 Center => new(X + Width / 2, Y + Height / 2);

        public bool Contains(Vector2 point)
        {
            return point.X >= X && point.X <= X + Width &&
                   point.Y >= Y && point.Y <= Y + Height;
        }

        public bool Overlaps(Rect other)
        {
            return X < other.X + other.Width && X + Width > other.X &&
                   Y < other.Y + other.Height && Y + Height > other.Y;
        }

        public Vector2 Clamp(Vector2 point)
        {
            return new Vector2(
                MathHelper.Clamp(point.X, Left, Right),
                MathHelper.Clamp(point.Y, Top, Bottom)
            );
        }

        public override string ToString()
        {
            return $"{{X:{X} Y:{Y} W:{Width} H:{Height}}}";
        }
    }
}
