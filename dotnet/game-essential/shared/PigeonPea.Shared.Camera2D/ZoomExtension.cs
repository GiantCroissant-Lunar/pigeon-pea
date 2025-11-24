using PigeonPea.Shared.Camera2D.Extensions;
using PigeonPea.Shared.Camera2D.Math;

namespace PigeonPea.Shared.Camera2D.Core
{
    public class ZoomExtension : ICameraExtension
    {
        public string Name => "Zoom";
        public bool Enabled { get; set; } = true;

        public float ZoomSpeed { get; set; } = 2.0f;
        public float MinZoom { get; set; } = 0.1f;
        public float MaxZoom { get; set; } = 10.0f;

        private Camera2DController? _camera;
        private float _targetZoom = 1.0f;

        public void Initialize(Camera2DController camera)
        {
            _camera = camera;
            _targetZoom = camera.Transform.Zoom;
        }

        public void SetTargetZoom(float zoom)
        {
            _targetZoom = MathHelper.Clamp(zoom, MinZoom, MaxZoom);
        }

        public void ZoomBy(float delta)
        {
            SetTargetZoom(_targetZoom + delta);
        }

        public void PreUpdate(float deltaTime)
        {
            // No pre-update logic
        }

        public void Update(float deltaTime)
        {
            if (_camera == null || !Enabled) return;

            _targetZoom = MathHelper.Clamp(_targetZoom, MinZoom, MaxZoom);

            float currentZoom = _camera.Transform.Zoom;
            float t = MathHelper.Clamp01(ZoomSpeed * deltaTime);

            _camera.Transform.Zoom = MathHelper.Lerp(currentZoom, _targetZoom, t);
        }

        public void PostUpdate(float deltaTime)
        {
            // No post-update logic
        }
    }
}
