---
canonical: true
created: '2025-11-13'
doc_id: REFERENCE-00002
doc_type: reference
related:
  - RFC-00020
  - SPEC-00001
status: active
summary: Code examples for mapping angles to PUA glyph codepoints (36 angle buckets
  starting at U+E000)
supersedes: []
tags:
  - fonts
  - glyphs
  - mapping
  - code-examples
  - pua
title: Angle → Glyph Mapping Examples
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
