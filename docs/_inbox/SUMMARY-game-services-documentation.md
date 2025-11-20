---
created: '2025-11-20'
doc_type: rfc
status: draft
summary: 'Created: 2025-11-20 For: Implementation by AI agents'
tags:
- agents
- architecture
- documentation
- ecs
- plugins
- rfc
- testing
title: 'Summary: Game Services Documentation Package'
---

# Summary: Game Services Documentation Package

**Created:** 2025-11-20
**For:** Implementation by AI agents

## What Has Been Created

I've created comprehensive documentation for the six core game services architecture:

### 1. Main RFC (Umbrella Document)
**File:** `docs/_inbox/game-services-architecture.md`
- Overview of all six services
- Architecture principles
- Design patterns
- Implementation phases
- Project structure
- Testing strategy

### 2. Detailed Service RFCs

**File:** `docs/_inbox/stats-service-rfc.md`
- Complete Stats Service specification
- Service contract (interface + DTOs)
- ECS components
- Data-driven configuration (JSON schema)
- Plugin implementation outline
- Unit test examples
- Use cases for characters, weapons, items, traps

**File:** `docs/_inbox/world-management-service-rfc.md`
- Complete World Management Service specification
- Multiple world patterns (interpolation, simulation, scene management)
- Entity transfer between worlds
- World cloning and snapshots
- Critical decision: Why World is NOT abstracted as a service
- Performance considerations

### 3. Implementation Guide

**File:** `docs/_inbox/game-services-implementation-guide.md`
- Step-by-step implementation instructions
- Implementation order (dependencies)
- Code examples for each phase
- Command reference
- Common pitfalls and solutions
- Validation checklists
- Complete project structure reference

## What's Included

### For Each Service:

✅ **Service Contract** - Interface definition with all methods
✅ **DTOs** - Data transfer objects (Views, Configs, etc.)
✅ **ECS Components** - Arch-based component definitions
✅ **Proxy Service** - Source-generated routing pattern
✅ **Plugin Structure** - Project layout and dependencies
✅ **Data Files** - JSON schemas for data-driven configuration
✅ **Implementation Examples** - Code snippets showing key logic
✅ **Unit Tests** - Test examples for validation
✅ **Use Cases** - Real-world usage scenarios

## Services Covered

1. **Stats Service** ⭐ (Detailed RFC)
   - Universal stat management for any entity
   - Modifier system (buffs/debuffs)
   - Derived stat formulas
   - Data-driven stat definitions

2. **Character Service** (Summary in umbrella RFC)
   - Character identity, class, progression
   - Experience/leveling system
   - Data-driven character classes

3. **Avatar Service** (Summary in umbrella RFC)
   - Visual appearance customization
   - Cosmetic equipment
   - Avatar presets

4. **Animation Service** (Summary in umbrella RFC)
   - Animation state management
   - Data-driven animation definitions
   - Playback control

5. **Persistence Service** (Summary in umbrella RFC)
   - Universal save/load system
   - Entity serialization
   - Save file management

6. **World Management Service** ⭐ (Detailed RFC)
   - Multiple ECS worlds
   - World cloning and snapshots
   - Entity transfer between worlds
   - Interpolation support
   - Critical architectural decision documented

## Key Architectural Decisions

### 1. World-Aware Services
All services accept `World` as a parameter:
```csharp
statsService.GetStats(world, entity);  // ✅ Correct
statsService.GetStats(entity);          // ❌ Wrong
```

### 2. No World Abstraction
**Critical:** `Arch.Core.World` is NOT abstracted as a service.
- World is shared infrastructure (like memory allocator)
- Services operate on World directly for performance
- World Management Service manages multiple worlds, not World operations

### 3. ECS-First Design
- Services operate on `Arch.Core.Entity` and components
- No EntityId abstraction layer
- Components in `PigeonPea.Shared.ECS`

### 4. Data-Driven Configuration
- Stats definitions: `stats-definitions.json`
- Character classes: `character-classes.json`
- Animations: `animations.json`
- Modder-friendly, no recompilation needed

### 5. Four-Tier Architecture
Each service follows standard pattern:
- Tier 1: Contract (interface + DTOs)
- Tier 2: Proxy (source-generated routing)
- Tier 3: Plugin Implementation
- Tier 4: Providers (optional strategies)

## Implementation Order (Critical!)

Implement in this order due to dependencies:

```
1. World Management Service  (foundation)
   ↓
2. Stats Service  (used by Character)
   ↓
3. Character Service  (uses Stats)
   ↓
4. Avatar Service  (standalone)
   ↓
5. Animation Service  (standalone)
   ↓
6. Persistence Service  (uses all above)
```

**Estimated Time:** 4-5 weeks (1 agent full-time)

## How to Use This Documentation

### For Implementation Agents:

1. **Start here:** Read `game-services-architecture.md` (umbrella RFC)
2. **Follow this:** Use `game-services-implementation-guide.md` for step-by-step
3. **Reference:** Check detailed RFCs for specific services
4. **Validate:** Use checklists in implementation guide

### For Review Agents:

1. Review service contracts for completeness
2. Check DTOs for data modeling correctness
3. Verify ECS component design
4. Validate data-driven configuration schemas
5. Check implementation examples for correctness

## Next Steps

### Before Implementation:

1. **Validate Documents**
   ```bash
   python scripts/validate-docs.py
   ```

2. **Move from Inbox to Final Location**
   - Once validated, move from `_inbox/` to `rfcs/` or `guides/`
   - Update `docs/index/registry.json`

3. **Assign doc_id**
   - Follow format: `RFC-2025-NNNNN` for RFCs
   - Follow format: `GUIDE-2025-NNNNN` for guides

### During Implementation:

1. Follow implementation guide phase-by-phase
2. Create branches for each service
3. Write unit tests first (TDD)
4. Validate with checklists
5. Create integration tests
6. Document any deviations or decisions

## Questions or Issues?

If agents encounter problems:

1. Check the relevant RFC for detailed specifications
2. Review implementation guide for common pitfalls
3. Consult existing unit tests for examples
4. Ask clarifying questions before implementing

## File Locations

All documents are currently in `docs/_inbox/`:

```
docs/_inbox/
├── game-services-architecture.md           (Umbrella RFC)
├── stats-service-rfc.md                    (Detailed RFC)
├── world-management-service-rfc.md         (Detailed RFC)
├── game-services-implementation-guide.md   (Implementation Guide)
└── SUMMARY-game-services-documentation.md  (This file)
```

## Success Criteria

Implementation is complete when:

- ✅ All six services implemented and tested
- ✅ Data-driven configuration working
- ✅ Multi-world support functional
- ✅ Save/load system working
- ✅ Services coordinate via events
- ✅ Integration tests passing
- ✅ Example application using all services
- ✅ Documentation updated with actual implementation

## Benefits of This Architecture

✅ **Universal & Reusable** - Stats work for characters, weapons, items, traps
✅ **Data-Driven** - All definitions in JSON (modder-friendly)
✅ **ECS-First** - Efficient, cache-friendly
✅ **Plugin-Based** - Implementations are swappable
✅ **Multi-World** - Enables interpolation, simulation, scene management
✅ **Future-Proof** - Ready for networking, advanced features
✅ **Testable** - Each tier independently testable
✅ **Maintainable** - Clear separation of concerns

---

**Ready for Implementation!** 🚀

All the documentation needed to implement the six core game services is now available. Other agents can follow the implementation guide step-by-step to build a complete, production-ready game services architecture.
