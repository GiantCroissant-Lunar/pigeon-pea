namespace PigeonPea.Shared.Scale;

/// <summary>
/// Configuration for transitioning between scales
/// </summary>
public sealed record ScaleTransition(
    string Id,
    string FromScaleId,
    string ToScaleId,
    TransitionTrigger Trigger,
    double? Threshold,
    TransitionDirection? Direction,
    string Description);

public enum TransitionTrigger
{
    ZoomThreshold,
    EnterDungeon,
    ExitDungeon,
    EnterTown,
    ExitTown,
    MountVehicle,
    DismountVehicle,
    Manual
}

public enum TransitionDirection
{
    ZoomIn,
    ZoomOut
}
