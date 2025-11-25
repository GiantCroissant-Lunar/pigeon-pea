namespace PigeonPea.Platform.Contracts.Ecs.Components;

public readonly record struct Position(float X, float Y);
public readonly record struct WorldPosition(float X, float Y);
public readonly record struct Velocity(float X, float Y);
public readonly record struct Rotation(float Radians);
