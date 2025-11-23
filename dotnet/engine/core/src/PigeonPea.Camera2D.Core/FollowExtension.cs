using PigeonPea.Camera2D.Damping;
using PigeonPea.Camera2D.Extensions;
using PigeonPea.Camera2D.Math;

namespace NexusCamera2D.Core
{
    public class FollowExtension : ICameraExtension
    {
        public string Name => "Follow";
        public bool Enabled { get; set; } = true;

        public DampingType DampingType { get; set; } = DampingType.Exponential;
        public float Smoothness { get; set; } = 5f;
        public float LookAhead { get; set; }

        private Vector2 _velocity;
        private Camera2D? _camera;
        private Vector2 _previousTargetPosition;

        public void Initialize(Camera2D camera)
        {
            _camera = camera;
            _previousTargetPosition = camera.CalculateTargetPosition();
        }

        public void PreUpdate(float deltaTime)
        {
        }

        public void Update(float deltaTime)
        {
            if (_camera == null || !Enabled)
            {
                return;
            }

            if (_camera.Targets.Count == 0)
            {
                return;
            }

            var target = _camera.CalculateTargetPosition();

            if (LookAhead > 0f)
            {
                var direction = (target - _previousTargetPosition).Normalized;
                target = target + direction * LookAhead;
            }

            var current = _camera.Transform.Position;
            Vector2 result;

            switch (DampingType)
            {
                case DampingType.Linear:
                    result = DampingHelper.LinearDamp(current, target, Smoothness, deltaTime);
                    break;
                case DampingType.Exponential:
                    result = DampingHelper.ExponentialDamp(current, target, Smoothness, deltaTime);
                    break;
                case DampingType.Spring:
                    result = DampingHelper.SpringDamp(current, target, ref _velocity, Smoothness, deltaTime);
                    break;
                default:
                    result = target;
                    break;
            }

            _camera.Transform.Position = result;
            _previousTargetPosition = target;
        }

        public void PostUpdate(float deltaTime)
        {
        }
    }
}
