using System;
using PigeonPea.Shared.Camera2D.Extensions;
using PigeonPea.Shared.Camera2D.Math;

namespace PigeonPea.Shared.Camera2D.Core
{
    public class ShakeExtension : ICameraExtension
    {
        public string Name => "Shake";
        public bool Enabled { get; set; } = true;

        private float _trauma;
        private float _seed;
        private readonly Random _random = new Random();
        private Camera2DController? _camera;

        public float MaxOffset { get; set; } = 10f;
        public float MaxAngle { get; set; } = 5f;
        public float TraumaDecay { get; set; } = 1.5f;
        public float TraumaPower { get; set; } = 2f; // Trauma^2 = Shake

        public float Trauma
        {
            get => _trauma;
            set => _trauma = MathHelper.Clamp(value, 0f, 1f);
        }

        public void AddTrauma(float amount)
        {
            Trauma += amount;
        }

        public void Initialize(Camera2DController camera)
        {
            _camera = camera;
        }

        public void PreUpdate(float deltaTime)
        {
            // No pre-update logic
        }

        public void Update(float deltaTime)
        {
            if (!Enabled) return;

            if (_trauma > 0)
            {
                _trauma -= TraumaDecay * deltaTime;
                _trauma = MathHelper.Clamp(_trauma, 0f, 1f);
                _seed += deltaTime * 10f; // Move through noise
            }
        }

        public void PostUpdate(float deltaTime)
        {
            if (_camera == null || !Enabled) return;
            if (_trauma <= 0) return;

            float shake = MathF.Pow(_trauma, TraumaPower);

            // Simple noise approximation using Random for now
            // In a real engine, Perlin noise would be smoother
            float offsetX = (float)(_random.NextDouble() * 2 - 1) * MaxOffset * shake;
            float offsetY = (float)(_random.NextDouble() * 2 - 1) * MaxOffset * shake;
            float angle = (float)(_random.NextDouble() * 2 - 1) * MaxAngle * shake;

            _camera.Transform.Position += new Vector2(offsetX, offsetY);
            _camera.Transform.Rotation += angle;
        }
    }
}
