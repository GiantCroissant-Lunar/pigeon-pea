using PigeonPea.Camera2D.Math;

namespace NexusCamera2D.Core
{
    public class CameraTransform
    {
        public Vector2 Position { get; set; }
        public float Rotation { get; set; }
        public float Zoom { get; set; } = 1.0f;

        public CameraTransform()
        {
            Position = Vector2.Zero;
            Rotation = 0f;
            Zoom = 1f;
        }
    }
}
