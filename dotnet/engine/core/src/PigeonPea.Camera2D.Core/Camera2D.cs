using System.Collections.Generic;
using System.Linq;
using PigeonPea.Camera2D.Extensions;
using PigeonPea.Camera2D.Math;
using PigeonPea.Camera2D.Triggers;

namespace PigeonPea.Camera2D.Core;

public sealed class Camera2D
{
    public CameraTransform Transform { get; } = new();

    public float ViewportWidth { get; set; }
    public float ViewportHeight { get; set; }

    public CameraUpdateMode UpdateMode { get; set; } = CameraUpdateMode.Update;

    private readonly List<CameraTarget> _targets = new();
    private readonly List<ICameraExtension> _extensions = new();
    private readonly List<ICameraTrigger> _triggers = new();

    public IReadOnlyList<CameraTarget> Targets => _targets.Where(t => t.Enabled).ToList();
    public IReadOnlyList<ICameraTrigger> Triggers => _triggers;

    public Camera2D()
    {
    }

    public void AddTarget(Vector2 position, float weight = 1.0f, Vector2? offset = null)
    {
        _targets.Add(new CameraTarget
        {
            Position = position,
            Weight = weight,
            Offset = offset ?? Vector2.Zero
        });
    }

    public void ClearTargets()
    {
        _targets.Clear();
    }

    public void AddExtension(ICameraExtension extension)
    {
        _extensions.Add(extension);
        extension.Initialize(this);
    }

    public void RemoveExtension(ICameraExtension extension)
    {
        _extensions.Remove(extension);
    }

    public void AddTrigger(ICameraTrigger trigger)
    {
        _triggers.Add(trigger);
        trigger.Initialize(this);
    }

    public void RemoveTrigger(ICameraTrigger trigger)
    {
        _triggers.Remove(trigger);
    }

    public T? GetExtension<T>() where T : ICameraExtension
    {
        return _extensions.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// The calculated target position based on the weighted average of all targets.
    /// Extensions like FollowExtension use this to determine where to move the camera.
    /// </summary>
    public Vector2 CalculateTargetPosition()
    {
        var activeTargets = Targets.ToList();
        if (activeTargets.Count == 0)
        {
            return Transform.Position;
        }

        float totalWeight = activeTargets.Sum(t => t.Weight);
        if (totalWeight <= 0)
        {
            return Transform.Position;
        }

        Vector2 weightedSum = Vector2.Zero;
        foreach (var target in activeTargets)
        {
            weightedSum = weightedSum + target.EffectivePosition * target.Weight;
        }

        return weightedSum / totalWeight;
    }

    /// <summary>
    /// Instantly moves the camera to the specified position.
    /// Also resets the TargetPosition to this value to prevent immediate snapping back if using smoothing.
    /// </summary>
    public void LookAt(Vector2 position)
    {
        Transform.Position = position;
    }

    public void Update(float deltaTime)
    {
        foreach (var trigger in _triggers.Where(t => t.Enabled))
        {
            trigger.Update(deltaTime);
        }

        foreach (var ext in _extensions.Where(e => e.Enabled))
        {
            ext.PreUpdate(deltaTime);
        }

        foreach (var ext in _extensions.Where(e => e.Enabled))
        {
            ext.Update(deltaTime);
        }

        foreach (var ext in _extensions.Where(e => e.Enabled))
        {
            ext.PostUpdate(deltaTime);
        }
    }
}
