using System;
using PigeonPea.Shared.Camera2D.Extensions;
using PigeonPea.Shared.Camera2D.Math;

namespace PigeonPea.Shared.Camera2D.Core
{
    public class PixelPerfectExtension : ICameraExtension
    {
        public string Name => "PixelPerfect";
        public bool Enabled { get; set; } = true;

        public int PixelsPerUnit { get; set; } = 100;

        private Camera2DController? _camera;

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
            // No update logic
        }

        public void PostUpdate(float deltaTime)
        {
            if (_camera == null || !Enabled) return;

            Vector2 pos = _camera.Transform.Position;

            if (PixelsPerUnit > 0)
            {
                pos.X = MathF.Round(pos.X * PixelsPerUnit) / PixelsPerUnit;
                pos.Y = MathF.Round(pos.Y * PixelsPerUnit) / PixelsPerUnit;
            }
            else
            {
                pos.X = MathF.Round(pos.X);
                pos.Y = MathF.Round(pos.Y);
            }

            _camera.Transform.Position = pos;
        }
    }
}
