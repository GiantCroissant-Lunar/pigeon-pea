namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Sprite component for visual representation.
/// </summary>
public struct Sprite
{
    public string Id { get; set; }          // Texture/sprite ID
    public char AsciiChar { get; set; }     // For terminal rendering
    public byte R { get; set; }             // Color (red)
    public byte G { get; set; }             // Color (green)
    public byte B { get; set; }             // Color (blue)

    public Sprite(string id, char asciiChar = '?', byte r = 255, byte g = 255, byte b = 255)
    {
        Id = id;
        AsciiChar = asciiChar;
        R = r;
        G = g;
        B = b;
    }

    public Sprite(string id) : this(id, '?', 255, 255, 255) { }
}
