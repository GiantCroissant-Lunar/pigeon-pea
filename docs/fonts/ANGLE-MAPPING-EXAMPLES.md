# Angle → Glyph Mapping Examples

This mapping assumes 36 angle buckets (every 10°) starting at `U+E000`.

## C#

```csharp
public static class MapGlyphs
{
    public static char GetAngleGlyph(double angleDegrees)
    {
        var normalized = angleDegrees % 360.0;
        if (normalized < 0) normalized += 360.0;
        int bucket = (int)Math.Round(normalized / 10.0) % 36;
        int codepoint = 0xE000 + bucket;
        return (char)codepoint;
    }
}
```

## Pseudocode

```
bucket = round(angle / 10) mod 36
codepoint = 0xE000 + bucket
```

