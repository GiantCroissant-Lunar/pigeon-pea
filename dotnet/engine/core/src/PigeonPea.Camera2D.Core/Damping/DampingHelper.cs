using System;
using PigeonPea.Camera2D.Math;

namespace PigeonPea.Camera2D.Damping;

public static class DampingHelper
{
    public static Vector2 LinearDamp(Vector2 current, Vector2 target, float smoothness, float deltaTime)
    {
        var t = MathHelper.Clamp01(smoothness * deltaTime);
        return Vector2.Lerp(current, target, t);
    }

    public static Vector2 ExponentialDamp(Vector2 current, Vector2 target, float smoothness, float deltaTime)
    {
        var decay = 1f - MathF.Exp(-smoothness * deltaTime);
        return Vector2.Lerp(current, target, decay);
    }

    public static Vector2 SpringDamp(Vector2 current, Vector2 target, ref Vector2 velocity, float smoothness, float deltaTime)
    {
        if (smoothness <= 0f)
        {
            return target;
        }

        var smoothTime = 1f / smoothness;
        return MathHelper.SmoothDamp(current, target, ref velocity, smoothTime, deltaTime);
    }
}
