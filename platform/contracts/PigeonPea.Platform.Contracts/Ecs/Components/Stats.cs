namespace PigeonPea.Platform.Contracts.Ecs.Components;

public readonly record struct Health(int Current, int Max);
public readonly record struct Energy(int Current, int Max);
