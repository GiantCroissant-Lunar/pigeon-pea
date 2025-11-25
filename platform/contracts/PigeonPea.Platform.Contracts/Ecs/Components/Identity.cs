namespace PigeonPea.Platform.Contracts.Ecs.Components;

public readonly record struct Layer(int Value)
{
    public const int MaxLayers = 32;
    public static implicit operator int(Layer l) => l.Value;
    public static implicit operator Layer(int v) => new(Math.Clamp(v, 0, MaxLayers - 1));
}

public readonly record struct Tag(int Value)
{
    public const int MaxTags = 64;
}

public readonly record struct LayerMask(uint Mask)
{
    public bool Contains(Layer layer) => (Mask & (1u << layer.Value)) != 0;
    public static LayerMask FromLayers(params int[] layers)
    {
        uint mask = 0;
        foreach (var layer in layers)
            mask |= 1u << layer;
        return new(mask);
    }
}
