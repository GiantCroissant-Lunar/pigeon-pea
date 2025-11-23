using System;

namespace NexusCamera2D.Math
{
    public static class MathHelper
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float Clamp01(float value) => Clamp(value, 0f, 1f);

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            return new Vector2(
                Lerp(a.X, b.X, t),
                Lerp(a.Y, b.Y, t)
            );
        }

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (MathF.Abs(target - current) <= maxDelta)
                return target;

            return current + MathF.Sign(target - current) * maxDelta;
        }

        // Based on Game Programming Gems 4 Chapter 1.10
        public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float deltaTime, float maxSpeed = float.PositiveInfinity)
        {
            smoothTime = MathF.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float originalTo = target;
            float maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            target = current - change;
            float temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;

            if (originalTo - current > 0.0f == output > originalTo)
            {
                output = originalTo;
                velocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 velocity, float smoothTime, float deltaTime, float maxSpeed = float.PositiveInfinity)
        {
            float vx = velocity.X;
            float vy = velocity.Y;

            float newX = SmoothDamp(current.X, target.X, ref vx, smoothTime, deltaTime, maxSpeed);
            float newY = SmoothDamp(current.Y, target.Y, ref vy, smoothTime, deltaTime, maxSpeed);

            velocity = new Vector2(vx, vy);
            return new Vector2(newX, newY);
        }
    }
}
