using PigeonPea.Camera2D.Math;

namespace NexusCamera2D.Core
{
    public class CameraTarget
    {
        public Vector2 Position { get; set; }
        public float Weight { get; set; } = 1.0f;
        public Vector2 Offset { get; set; }
        public bool Enabled { get; set; } = true;

        public Vector2 EffectivePosition => Position + Offset;
    }
}
