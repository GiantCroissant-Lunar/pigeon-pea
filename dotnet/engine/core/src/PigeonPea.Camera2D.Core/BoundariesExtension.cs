using PigeonPea.Camera2D.Extensions;
using PigeonPea.Camera2D.Math;

namespace NexusCamera2D.Core
{
    public class BoundariesExtension : ICameraExtension
    {
        public string Name => "Boundaries";
        public bool Enabled { get; set; } = true;

        public Rect Limits { get; set; }

        private Camera2D? _camera;

        public BoundariesExtension(Rect limits)
        {
            Limits = limits;
        }

        public void Initialize(Camera2D camera)
        {
            _camera = camera;
        }

        public void PreUpdate(float deltaTime)
        {
            // No pre-update logic
        }

        public void Update(float deltaTime)
        {
            // No update logic, we clamp after movement
        }

        public void PostUpdate(float deltaTime)
        {
            if (_camera == null || !Enabled) return;

            Vector2 pos = _camera.Transform.Position;

            // Simple clamping of the center position
            // A more advanced version might account for viewport size/zoom
            pos.X = MathHelper.Clamp(pos.X, Limits.Left, Limits.Right);
            pos.Y = MathHelper.Clamp(pos.Y, Limits.Top, Limits.Bottom);

            _camera.Transform.Position = pos;
        }
    }
}
