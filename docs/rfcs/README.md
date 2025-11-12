# Request for Comments (RFCs)

This directory contains RFCs (Request for Comments) for architectural decisions, feature proposals, and significant changes to the Pigeon Pea project.

## Active RFCs

### RFC-001 to RFC-006

See existing RFCs (001-004 in main branch, 005-006 in other branch).

### RFC-007: Domain-Driven Architecture Reorganization (Map & Dungeon)

**Status**: Proposed (Revised 2025-11-13)
**Created**: 2025-11-13
**Priority**: P0 (Critical)

**Summary**: Reorganize into **domain-driven design** with separate Map and Dungeon domains, each following **Core/Control/Rendering** trinity pattern. Enables Arch ECS integration per domain and future extensibility.

**Key Points**:

- **Fine-grained projects**: `Map.Core`, `Map.Control`, `Map.Rendering`, `Dungeon.*`, `Shared.*`
- **Adapter pattern**: Wrap FantasyMapGenerator.Core and GoRogue behind clean interfaces
- **Mapsui integration**: Map.Control uses Mapsui directly (no wrapper)
- **Arch ECS support**: Separate ECS worlds for map entities vs dungeon entities
- **6-week implementation timeline**
- Foundation for all other RFCs (008-011)

**Implementation**: [007-consolidate-rendering-projects.md](./007-consolidate-rendering-projects.md)

---

### RFC-008: Determinism Test Suite

**Status**: Proposed
**Created**: 2025-11-13
**Priority**: P0 (High)

**Summary**: Establish comprehensive determinism testing with snapshot tests, topological invariants, and cross-platform verification to ensure same seed → same world.

**Key Points**:

- Snapshot tests with SHA256 checksums for 5+ reference seeds
- Topological invariant tests (rivers flow downhill, coastlines valid, etc.)
- Statistical distribution tests (land/water ratio, river count)
- Critical for save/load, multiplayer, and regression detection

**Implementation**: [008-determinism-test-suite.md](./008-determinism-test-suite.md)

---

### RFC-009: Performance Benchmarking

**Status**: Proposed
**Created**: 2025-11-13
**Priority**: P1 (Medium-High)

**Summary**: Add BenchmarkDotNet infrastructure to measure and track generation/rendering performance across different map sizes and configurations.

**Key Points**:

- Benchmarks for map generation (1k, 8k, 16k, 32k points)
- Phase-specific benchmarks (Voronoi, heightmap, rivers, biomes)
- Rendering benchmarks (Skia rasterization, Braille conversion)
- RNG comparison (PCG vs System.Random vs Alea)
- CI integration for regression tracking

**Implementation**: [009-performance-benchmarking.md](./009-performance-benchmarking.md)

---

### RFC-010: Color Scheme Configuration

**Status**: Proposed
**Created**: 2025-11-13
**Priority**: P1 (Medium-High)

**Summary**: Implement configurable color schemes (Original, Realistic, Fantasy, HighContrast, Monochrome, Parchment) with ViewModel integration and UI controls.

**Key Points**:

- 6 predefined color schemes
- Integration with `MapRenderViewModel` (ReactiveUI)
- UI controls for console (Terminal.Gui) and desktop (Avalonia)
- Unified `ColorSchemes` class replaces hardcoded color logic

**Implementation**: [010-color-scheme-configuration.md](./010-color-scheme-configuration.md)

---

### RFC-011: Water Shimmer Animation

**Status**: Proposed
**Created**: 2025-11-13
**Priority**: P2 (Medium-Low)

**Summary**: Add subtle water shimmer animation using existing `timeSeconds` parameter via sinusoidal color modulation, opt-in via `PP_WATER_SHIMMER=1` flag.

**Key Points**:

- Leverages already-plumbed `timeSeconds` parameter
- Simple sin-wave based shimmer (±10 RGB brightness modulation)
- Opt-in via environment variable (disabled by default)
- <5% performance overhead
- Foundation for future time-based effects (river flow, day/night)

**Implementation**: [011-water-shimmer-animation.md](./011-water-shimmer-animation.md)

---

## RFC Priority Guide

| Priority | Level    | Description                                      |
| -------- | -------- | ------------------------------------------------ |
| **P0**   | Critical | Architectural foundations, must be done first    |
| **P1**   | High     | Important features, quality-of-life improvements |
| **P2**   | Medium   | Nice-to-have enhancements, polish                |
| **P3**   | Low      | Future considerations, backlog                   |

## Implementation Order Recommendation

Based on dependencies and priorities:

1. **RFC-007: Consolidate Rendering** (P0) - Week 1-3
   - Clears architectural confusion
   - Prerequisite for color scheme work

2. **RFC-008: Determinism Tests** (P0) - Week 1-3 (parallel with RFC-007)
   - Can be implemented independently
   - Critical for quality assurance

3. **RFC-009: Performance Benchmarking** (P1) - Week 4-6
   - Depends on stable rendering architecture
   - Informs optimization priorities

4. **RFC-010: Color Schemes** (P1) - Week 4-6 (after RFC-007)
   - Depends on consolidated rendering
   - Can be done in parallel with RFC-009

5. **RFC-011: Water Shimmer** (P2) - Week 7-8
   - Pure enhancement, no dependencies
   - Can be deferred if time-constrained

## How to Use This Directory

### For Implementers

1. Read the RFC thoroughly
2. Check "Implementation Plan" section for phased tasks
3. Follow the timeline and success criteria
4. Update "Status" field as you progress:
   - `Proposed` → `Accepted` (after review)
   - `Accepted` → `In Progress` (when starting)
   - `In Progress` → `Completed` (when done)
   - `Completed` → `Superseded` (if replaced by newer RFC)

### For Reviewers

1. Check "Motivation" and "Goals" alignment with project vision
2. Review "Design" section for technical soundness
3. Evaluate "Risks and Mitigations"
4. Approve or request changes in "Approval" section

### For Users/Stakeholders

1. Read "Summary" for high-level overview
2. Check "Success Criteria" to understand outcomes
3. Review "Timeline" for delivery expectations

## RFC Template

When creating new RFCs, use this structure:

```markdown
# RFC-XXX: Title

## Status

**Status**: Proposed
**Created**: YYYY-MM-DD
**Author**: Name

## Summary

One-paragraph overview

## Motivation

Why are we doing this?

## Design

How will we do it?

## Implementation Plan

Step-by-step phases

## Testing Strategy

How will we verify it works?

## Alternatives Considered

What else did we think about?

## Risks and Mitigations

What could go wrong?

## Success Criteria

How do we know when we're done?

## Timeline

Estimated effort

## Open Questions

Unresolved issues

## References

Related docs/links

## Approval

- [ ] Checklist items
```

## Related Documentation

- [ARCHITECTURE_MAP_RENDERING.md](../../ARCHITECTURE_MAP_RENDERING.md) - Overall rendering architecture
- [ARCHITECTURE_PLAN.md](../../ARCHITECTURE_PLAN.md) - General project architecture
- [docs/testing-guide.md](../testing-guide.md) - Testing conventions (if exists)

## Questions or Feedback

For questions about RFCs, open an issue or discussion in the repository with the `RFC` label.
