namespace PigeonPea.Windows.Rendering;

/// <summary>
/// Manages all active animations and their playback state.
/// Tracks elapsed time for each animation instance and provides current frame information.
/// </summary>
public class AnimationSystem
{
    private readonly Dictionary<int, Animation> _animations = new();
    private readonly Dictionary<int, AnimationInstance> _activeInstances = new();
    private int _nextInstanceId = 1;

    /// <summary>
    /// Represents an active instance of an animation.
    /// </summary>
    private class AnimationInstance
    {
        public required int AnimationId { get; init; }
        public float ElapsedTime { get; set; }
        public bool IsComplete { get; set; }
    }

    /// <summary>
    /// Gets the number of registered animations.
    /// </summary>
    public int AnimationCount => _animations.Count;

    /// <summary>
    /// Gets the number of active animation instances.
    /// </summary>
    public int ActiveInstanceCount => _activeInstances.Count;

    /// <summary>
    /// Registers an animation with the system.
    /// </summary>
    /// <param name="animation">The animation to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when animation is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an animation with the same ID is already registered.</exception>
    public void RegisterAnimation(Animation animation)
    {
        if (animation == null)
            throw new ArgumentNullException(nameof(animation));

        if (_animations.ContainsKey(animation.Id))
            throw new InvalidOperationException($"Animation with ID {animation.Id} is already registered.");

        _animations[animation.Id] = animation;
    }

    /// <summary>
    /// Checks if an animation is registered.
    /// </summary>
    /// <param name="animationId">The animation ID to check.</param>
    /// <returns>True if the animation is registered, false otherwise.</returns>
    public bool HasAnimation(int animationId)
    {
        return _animations.ContainsKey(animationId);
    }

    /// <summary>
    /// Starts playing an animation.
    /// </summary>
    /// <param name="animationId">The ID of the animation to play.</param>
    /// <returns>The instance ID for the started animation, or -1 if the animation is not registered.</returns>
    public int PlayAnimation(int animationId)
    {
        if (!_animations.ContainsKey(animationId))
            return -1;

        var instanceId = _nextInstanceId++;
        _activeInstances[instanceId] = new AnimationInstance
        {
            AnimationId = animationId,
            ElapsedTime = 0,
            IsComplete = false
        };

        return instanceId;
    }

    /// <summary>
    /// Stops an animation instance.
    /// </summary>
    /// <param name="instanceId">The instance ID to stop.</param>
    /// <returns>True if the instance was found and stopped, false otherwise.</returns>
    public bool StopAnimation(int instanceId)
    {
        return _activeInstances.Remove(instanceId);
    }

    /// <summary>
    /// Updates all active animations by advancing their elapsed time.
    /// Removes completed one-shot animations automatically.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    public void Update(float deltaTime)
    {
        // Validate deltaTime
        if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            return;

        var completedInstances = new List<int>();

        foreach (var kvp in _activeInstances)
        {
            var instanceId = kvp.Key;
            var instance = kvp.Value;

            if (!_animations.TryGetValue(instance.AnimationId, out var animation))
            {
                completedInstances.Add(instanceId);
                continue;
            }

            // Update elapsed time
            instance.ElapsedTime += deltaTime;

            // Check if non-looping animation is complete
            if (!animation.IsLooping && animation.IsComplete(instance.ElapsedTime))
            {
                instance.IsComplete = true;
                completedInstances.Add(instanceId);
            }
        }

        // Remove completed instances
        foreach (var instanceId in completedInstances)
        {
            _activeInstances.Remove(instanceId);
        }
    }

    /// <summary>
    /// Gets the current frame (sprite ID) for an animation instance.
    /// </summary>
    /// <param name="instanceId">The instance ID to get the frame for.</param>
    /// <returns>The sprite ID for the current frame, or null if the instance is not found or animation is complete.</returns>
    public int? GetCurrentFrame(int instanceId)
    {
        if (!_activeInstances.TryGetValue(instanceId, out var instance))
            return null;

        if (!_animations.TryGetValue(instance.AnimationId, out var animation))
            return null;

        return animation.GetCurrentFrame(instance.ElapsedTime);
    }

    /// <summary>
    /// Checks if an animation instance is currently active (playing).
    /// </summary>
    /// <param name="instanceId">The instance ID to check.</param>
    /// <returns>True if the instance is active, false otherwise.</returns>
    public bool IsInstanceActive(int instanceId)
    {
        return _activeInstances.ContainsKey(instanceId);
    }

    /// <summary>
    /// Gets the elapsed time for an animation instance.
    /// </summary>
    /// <param name="instanceId">The instance ID to get the elapsed time for.</param>
    /// <returns>The elapsed time in seconds, or null if the instance is not found.</returns>
    public float? GetElapsedTime(int instanceId)
    {
        if (!_activeInstances.TryGetValue(instanceId, out var instance))
            return null;

        return instance.ElapsedTime;
    }

    /// <summary>
    /// Clears all active animation instances.
    /// </summary>
    public void ClearInstances()
    {
        _activeInstances.Clear();
    }

    /// <summary>
    /// Clears all registered animations and active instances.
    /// </summary>
    public void Clear()
    {
        _animations.Clear();
        _activeInstances.Clear();
        _nextInstanceId = 1;
    }
}
