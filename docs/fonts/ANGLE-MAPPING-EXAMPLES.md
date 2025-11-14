---
doc_id: "SPEC-2025-00002"
title: "Angle → Glyph Mapping Examples"
doc_type: "reference"
status: "active"
canonical: true
created: "2025-11-13"
tags: ["fonts", "glyphs", "mapping", "code-examples", "pua"]
summary: "Code examples for mapping angles to PUA glyph codepoints (36 angle buckets starting at U+E000)"
supersedes: []
related: ["RFC-2025-00020", "SPEC-2025-00001"]
---

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
