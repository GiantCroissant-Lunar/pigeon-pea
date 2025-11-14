# Pigeon Pea Documentation

Welcome to the Pigeon Pea documentation! This directory contains planning documents, RFCs, and technical discussions for the project.

## Contents

### Directory Index

- [architecture/](architecture) – Architecture plans, rendering design, protocol notes
- [rfcs/](rfcs) – Formal proposals and technical specs
- [handovers/](handovers) – Handover summaries and session notes
- [notes/](notes) – Scratch notes, snapshots, and fragments
- [assets/](assets) – Images and static assets used by docs

### Enhancement Planning

- **[ENHANCEMENT_PLAN.md](ENHANCEMENT_PLAN.md)**: Comprehensive plan for platform-specific enhancements after completing core game logic features

### Implementation Guide

- **[GITHUB_ISSUES.md](GITHUB_ISSUES.md)**: Breakdown of all RFCs into 50 actionable GitHub issues
  - Each issue is sized for 1-3 days of work
  - Includes clear acceptance criteria and code examples
  - Suitable for GitHub coding agents
  - Tracks dependencies between issues

### RFCs (Requests for Comments)

RFCs provide detailed technical specifications for major features and architectural decisions.

- **[RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)**
  - Unified rendering abstraction for Windows (SkiaSharp) and Console (Kitty/Sixel/Braille/ASCII)
  - Sprite atlases, particles, animated tiles
  - Multi-tier terminal graphics support
  - Status: Draft

- **[RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)**
  - Reactive programming with System.Reactive, ReactiveUI, ObservableCollections
  - Shared view models in platform-agnostic layer
  - MVVM architecture for both Windows and Console apps
  - CLI argument parsing with System.CommandLine
  - Status: Draft

- **[RFC-003: Testing and Verification](rfcs/003-testing-verification.md)**
  - Visual regression testing with asciinema (console) and FFmpeg (windows)
  - PTY-based terminal testing with node-pty
  - Automated screenshot comparison
  - Performance benchmarking
  - Status: Draft

## Development Process

### Reading RFCs

1. RFCs are written in draft form and open for discussion
2. Each RFC has a status (Draft, Accepted, Implemented, Deprecated)
3. Comment on RFCs by opening GitHub issues or discussions

### Implementing RFCs

1. RFCs should be accepted before implementation begins
2. Use [GITHUB_ISSUES.md](GITHUB_ISSUES.md) to break down RFCs into actionable tasks
3. Implementation can be done in phases as outlined in each RFC
4. Create GitHub issues from the breakdown document
5. Update RFC status to "Implemented" when complete

### Creating New RFCs

To propose a new feature or architectural change:

1. Create a new RFC in `docs/rfcs/`
2. Use the next available number (e.g., `004-feature-name.md`)
3. Follow the RFC template structure:
   - Status
   - Summary
   - Motivation
   - Design
   - Implementation Plan
   - Testing Strategy
   - Open Questions
   - References

## RFC Template

```markdown
# RFC-XXX: Title

## Status

**Status**: Draft
**Created**: YYYY-MM-DD
**Author**: Your Name

## Summary

Brief overview of the proposal.

## Motivation

Why is this needed? What problems does it solve?

## Design

Detailed technical design with code examples, diagrams, and architecture.

## Implementation Plan

Step-by-step plan for implementing the RFC.

## Testing Strategy

How will this be tested?

## Performance Considerations

Performance implications and optimizations.

## Open Questions

Unresolved questions and areas for discussion.

## References

Links to related documentation, libraries, and resources.
```

## Quick Links

### Main Documentation

- [Main README](../README.md) - Project overview
- [.NET README](../dotnet/README.md) - Build and run instructions
- [Architecture](../dotnet/ARCHITECTURE.md) - Current architecture overview

### External Resources

- [Arch ECS](https://github.com/genaray/Arch) - Entity Component System
- [GoRogue](https://github.com/Chris3606/GoRogue) - Roguelike algorithms
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform UI framework
- [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) - Terminal UI framework
- [SkiaSharp](https://github.com/mono/SkiaSharp) - 2D graphics library
- [Kitty Graphics Protocol](https://sw.kovidgoyal.net/kitty/graphics-protocol/)
- [Sixel Graphics](https://en.wikipedia.org/wiki/Sixel)

## Contributing

We welcome contributions! Please:

1. Review existing RFCs before proposing new features
2. Open discussions for major changes
3. Follow the RFC process for architectural decisions
4. Keep documentation up-to-date with implementation

## Questions and Discussion

- Open a GitHub issue for specific questions
- Use GitHub Discussions for broader topics
- Comment on RFCs for feedback on proposals
