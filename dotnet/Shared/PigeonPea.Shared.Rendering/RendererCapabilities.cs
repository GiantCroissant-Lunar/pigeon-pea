namespace PigeonPea.Shared.Rendering;

[Flags]
public enum RendererCapabilities
{
    None = 0,
    TrueColor = 1 << 0,
    Sprites = 1 << 1,
    Particles = 1 << 2,
    Animation = 1 << 3,
    PixelGraphics = 1 << 4,
    CharacterBased = 1 << 5,
    MouseInput = 1 << 6,
}

public static class RendererCapabilitiesExtensions
{
    public static bool Supports(this RendererCapabilities caps, RendererCapabilities feature)
        => (caps & feature) == feature;
}
