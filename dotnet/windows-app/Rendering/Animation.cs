namespace PigeonPea.Windows.Rendering;

/// <summary>
/// Represents an animation with multiple frames and timing configuration.
/// Animations can be looping (e.g., water, torches) or one-shot (e.g., explosions).
/// </summary>
public class Animation
{
    /// <summary>
    /// Gets the unique identifier for this animation.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the list of sprite IDs representing animation frames.
    /// </summary>
    public required int[] Frames { get; init; }

    /// <summary>
    /// Gets the duration of each frame in seconds.
    /// </summary>
    public required float FrameDuration { get; init; }

    /// <summary>
    /// Gets a value indicating whether this animation loops.
    /// </summary>
    public bool IsLooping { get; init; } = true;

    /// <summary>
    /// Gets the total duration of the animation in seconds.
    /// </summary>
    public float TotalDuration => Frames.Length * FrameDuration;

    /// <summary>
    /// Gets the current frame index based on elapsed time.
    /// </summary>
    /// <param name="elapsedTime">The elapsed time since the animation started in seconds.</param>
    /// <returns>The frame index, or -1 if the animation has completed (for non-looping animations).</returns>
    public int GetFrameIndex(float elapsedTime)
    {
        if (Frames.Length == 0)
            return -1;

        if (FrameDuration <= 0)
            return 0;

        var totalTime = elapsedTime;
        var totalFrames = Frames.Length;

        if (IsLooping)
        {
            // For looping animations, wrap around using modulo
            var frameIndex = (int)(totalTime / FrameDuration) % totalFrames;
            return frameIndex;
        }
        else
        {
            // For one-shot animations, clamp to last frame or return -1 when complete
            var frameIndex = (int)(totalTime / FrameDuration);
            if (frameIndex >= totalFrames)
                return -1; // Animation complete
            return frameIndex;
        }
    }

    /// <summary>
    /// Gets the sprite ID for the current frame based on elapsed time.
    /// </summary>
    /// <param name="elapsedTime">The elapsed time since the animation started in seconds.</param>
    /// <returns>The sprite ID for the current frame, or null if the animation has completed.</returns>
    public int? GetCurrentFrame(float elapsedTime)
    {
        var frameIndex = GetFrameIndex(elapsedTime);
        if (frameIndex < 0 || frameIndex >= Frames.Length)
            return null;

        return Frames[frameIndex];
    }

    /// <summary>
    /// Gets a value indicating whether the animation has completed.
    /// Always returns false for looping animations.
    /// </summary>
    /// <param name="elapsedTime">The elapsed time since the animation started in seconds.</param>
    /// <returns>True if the animation has completed, false otherwise.</returns>
    public bool IsComplete(float elapsedTime)
    {
        if (IsLooping)
            return false;

        return GetFrameIndex(elapsedTime) < 0;
    }
}
