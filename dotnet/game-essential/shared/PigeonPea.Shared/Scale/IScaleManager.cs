using System;
using System.Collections.Generic;

namespace PigeonPea.Shared.Scale;

/// <summary>
/// Service for managing discrete scale/zoom modes
/// </summary>
public interface IScaleManager
{
    ScaleConfig ActiveScale { get; }
    double CurrentZoom { get; }

    IReadOnlyList<ScaleConfig> GetAvailableScales();
    ScaleConfig? GetScale(string scaleId);
    void SetScale(string scaleId);

    void SetZoom(double zoom);
    double ClampZoom(double zoom);
    ScaleConfig? GetScaleForZoom(double zoom);

    IReadOnlyList<ScaleTransition> GetAvailableTransitions();
    ScaleTransition? TryTransition(TransitionTrigger trigger, double? currentZoom = null);

    event EventHandler<ScaleChangedEventArgs>? ScaleChanged;
    event EventHandler<ZoomChangedEventArgs>? ZoomChanged;
}

public sealed record ScaleChangedEventArgs(ScaleConfig PreviousScale, ScaleConfig NewScale);
public sealed record ZoomChangedEventArgs(double PreviousZoom, double NewZoom);
