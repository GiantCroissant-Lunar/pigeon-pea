namespace PigeonPea.Rendering.Contracts;

[Flags]
public enum TileFlags
{
    None = 0,
    Animated = 1 << 0,
    Particle = 1 << 1,
    Transparent = 1 << 2,
}
