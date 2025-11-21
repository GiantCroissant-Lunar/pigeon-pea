using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PigeonPea.Shared.Scale;

namespace PigeonPea.Plugin.Scale.Manager;

public sealed class ScaleManager : IScaleManager
{
    private readonly ILogger<ScaleManager> _logger;
    private readonly Dictionary<string, ScaleConfig> _scales;
    private readonly List<ScaleTransition> _transitions;
    private ScaleConfig _activeScale;
    private double _currentZoom;

    public ScaleManager(ScaleConfigSet configSet, ILogger<ScaleManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scales = configSet.Scales.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        _transitions = configSet.Transitions.ToList();

        if (!_scales.TryGetValue("world", out var worldScale))
        {
            throw new InvalidOperationException("Configuration must include 'world' scale");
        }

        _activeScale = worldScale;
        _currentZoom = 1.0;

        _logger.LogInformation("ScaleManager initialized with {ScaleCount} scales, {TransitionCount} transitions. Active scale: {ActiveScale}",
            _scales.Count, _transitions.Count, _activeScale.Id);
    }

    public ScaleConfig ActiveScale => _activeScale;
    public double CurrentZoom => _currentZoom;

    public IReadOnlyList<ScaleConfig> GetAvailableScales() => _scales.Values.ToList();

    public ScaleConfig? GetScale(string scaleId)
    {
        return _scales.TryGetValue(scaleId, out var scale) ? scale : null;
    }

    public void SetScale(string scaleId)
    {
        if (!_scales.TryGetValue(scaleId, out var newScale))
        {
            _logger.LogWarning("Scale '{ScaleId}' not found", scaleId);
            return;
        }

        if (string.Equals(_activeScale.Id, newScale.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var previousScale = _activeScale;
        _activeScale = newScale;

        _currentZoom = ClampZoom(_currentZoom);

        _logger.LogInformation("Scale changed: {PreviousScale} → {NewScale} (zoom: {Zoom:F2})",
            previousScale.Id, newScale.Id, _currentZoom);

        ScaleChanged?.Invoke(this, new ScaleChangedEventArgs(previousScale, newScale));
    }

    public void SetZoom(double zoom)
    {
        var clampedZoom = ClampZoom(zoom);
        if (Math.Abs(clampedZoom - _currentZoom) < 0.001)
        {
            return;
        }

        var previousZoom = _currentZoom;
        _currentZoom = clampedZoom;

        _logger.LogDebug("Zoom changed: {PreviousZoom:F2} → {NewZoom:F2} (scale: {Scale})",
            previousZoom, clampedZoom, _activeScale.Id);

        ZoomChanged?.Invoke(this, new ZoomChangedEventArgs(previousZoom, clampedZoom));

        var transition = TryTransition(TransitionTrigger.ZoomThreshold, clampedZoom);
        if (transition != null)
        {
            SetScale(transition.ToScaleId);
        }
    }

    public double ClampZoom(double zoom)
    {
        return Math.Clamp(zoom, _activeScale.MinZoom, _activeScale.MaxZoom);
    }

    public ScaleConfig? GetScaleForZoom(double zoom)
    {
        return _scales.Values
            .Where(s => zoom >= s.MinZoom && zoom <= s.MaxZoom)
            .OrderBy(s => Math.Abs(zoom - 1.0))
            .FirstOrDefault();
    }

    public IReadOnlyList<ScaleTransition> GetAvailableTransitions()
    {
        return _transitions
            .Where(t => string.Equals(t.FromScaleId, _activeScale.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public ScaleTransition? TryTransition(TransitionTrigger trigger, double? currentZoom = null)
    {
        var zoom = currentZoom ?? _currentZoom;

        foreach (var transition in _transitions.Where(t =>
            string.Equals(t.FromScaleId, _activeScale.Id, StringComparison.OrdinalIgnoreCase) &&
            t.Trigger == trigger))
        {
            if (trigger == TransitionTrigger.ZoomThreshold && transition.Threshold.HasValue && transition.Direction.HasValue)
            {
                var direction = transition.Direction.Value;
                if (direction == TransitionDirection.ZoomIn && zoom >= transition.Threshold.Value)
                {
                    _logger.LogInformation("Triggering transition: {TransitionId} (zoom {Zoom:F2} >= threshold {Threshold:F2})",
                        transition.Id, zoom, transition.Threshold.Value);
                    return transition;
                }
                else if (direction == TransitionDirection.ZoomOut && zoom <= transition.Threshold.Value)
                {
                    _logger.LogInformation("Triggering transition: {TransitionId} (zoom {Zoom:F2} <= threshold {Threshold:F2})",
                        transition.Id, zoom, transition.Threshold.Value);
                    return transition;
                }
            }
            else if (trigger != TransitionTrigger.ZoomThreshold)
            {
                _logger.LogInformation("Triggering transition: {TransitionId} (trigger: {Trigger})",
                    transition.Id, trigger);
                return transition;
            }
        }

        return null;
    }

    public event EventHandler<ScaleChangedEventArgs>? ScaleChanged;
    public event EventHandler<ZoomChangedEventArgs>? ZoomChanged;
}
