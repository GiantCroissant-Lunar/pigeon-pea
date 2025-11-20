---
canonical: true
created: '2025-11-20'
doc_id: SPEC-2025-00002
doc_type: spec
related: []
status: active
summary: This document enumerates the Private Use Area (PUA) codepoints used by the
  project.
supersedes: []
tags:
- fonts
- spec
title: PUA Layout for Map Glyphs
---

# PUA Layout for Map Glyphs

This document enumerates the Private Use Area (PUA) codepoints used by the project.

Range: BMP PUA U+E000–U+F8FF (6,400 slots)

## Angled Lines

Base angles (10° buckets):

```
U+E000  0°
U+E001  10°
U+E002  20°
...
U+E023  350°
```

Variants:

```
U+E030–U+E053  Thick angles 0°..350°
U+E060–U+E083  Dashed angles 0°..350°
```

## Future Blocks

```
U+E100–U+E1FF  Junctions/connectors (L, T, X, specials)
U+E200–U+E2FF  Terrain tiles (water, forest, mountains, fields)
U+E300–U+E3FF  POIs/icons (towns, ports, ruins, towers)
```

Keep this file updated when allocating new glyphs.
