using System;
using PigeonPea.Shared.Camera2D.Extensions;
using PigeonPea.Shared.Camera2D.Math;

namespace PigeonPea.Shared.Camera2D.Core;

public class DeadzoneExtension : ICameraExtension
{
    public string Name => "Deadzone";
    public bool Enabled { get; set; } = true;

    public float DeadzoneWidth { get; set; }
    public float DeadzoneHeight { get; set; }
    public float SoftEdge { get; set; }

    private Camera2DController? _camera;

    public void Initialize(Camera2DController camera)
    {
        _camera = camera;
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

        if (DeadzoneWidth <= 0f || DeadzoneHeight <= 0f)
        {
            return;
        }

        var target = _camera.CalculateTargetPosition();
        var position = _camera.Transform.Position;

        var dx = target.X - position.X;
        var dy = target.Y - position.Y;

        var halfWidth = DeadzoneWidth * 0.5f;
        var halfHeight = DeadzoneHeight * 0.5f;

        float shiftX = 0f;
        float shiftY = 0f;

        if (dx > halfWidth)
        {
            shiftX = dx - halfWidth;
        }
        else if (dx < -halfWidth)
        {
            shiftX = dx + halfWidth;
        }

        if (dy > halfHeight)
        {
            shiftY = dy - halfHeight;
        }
        else if (dy < -halfHeight)
        {
            shiftY = dy + halfHeight;
        }

        if (SoftEdge > 0f)
        {
            var distanceX = MathF.Abs(dx) - halfWidth;
            var distanceY = MathF.Abs(dy) - halfHeight;

            var factorX = distanceX > 0f ? MathHelper.Clamp01(distanceX / SoftEdge) : 0f;
            var factorY = distanceY > 0f ? MathHelper.Clamp01(distanceY / SoftEdge) : 0f;

            shiftX *= factorX;
            shiftY *= factorY;
        }

        _camera.Transform.Position = new Vector2(position.X + shiftX, position.Y + shiftY);
    }

    public void PostUpdate(float deltaTime)
    {
    }
}
