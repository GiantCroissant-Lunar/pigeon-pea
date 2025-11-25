namespace PigeonPea.Platform.Contracts.Ecs.Components;

public readonly record struct Renderable(bool Visible = true);
public readonly record struct ZOrder(int Value);
