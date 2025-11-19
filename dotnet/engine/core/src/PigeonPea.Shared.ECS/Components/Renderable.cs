namespace PigeonPea.Shared.ECS.Components;

public readonly record struct Renderable(bool Visible, int Layer = 0);
