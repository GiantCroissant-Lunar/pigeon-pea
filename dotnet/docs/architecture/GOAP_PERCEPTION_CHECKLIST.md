# GOAP Perception World-State Checklist

This document captures the current perception/knowledge world-state keys that a GOAP agent can safely consume, along with their sources and reliability notes.

The content below is adapted from the design notes for NexusPerception-like systems and current game wiring.

---

## 1. Visual / position–based keys

**Examples (from adapter):**

- `PlayerVisible`
- `PlayerDistance`
- `PlayerHealth`
- `PlayerDirection`
- `VisibleEnemyCount`
- `HasVisibleEnemies`
- `VisibleItemCount`

**Source / semantics**

- Backed by `PerceptionData.Visual` + `VisionSensor`:
  - `GetClosestEntity("Player")`
  - `GetEntitiesOfType("Enemy")`
  - `GetEntitiesOfType("Item")`
- Entity type classification:
  - `PlayerTag` → `"Player"`
  - `ItemTag` → `"Item"`
  - `MonsterTag` or `AIComponent` → `"Enemy"`

**Reliability**

- Ready for use.
- As long as entities have those tags/components, these flags and counters reflect actual FOV.
- Caveat: FOV currently uses "all tiles transparent" → agents can see through walls for now.

---

## 2. Knowledge / memory–based keys

**Examples:**

- `KnownEnemyCount`
- `HasKnownThreats`
- Any "last known position"–style keys

**Source / semantics**

- Backed by `PerceptionData.Knowledge`:
  - `UpdateKnowledge` updates `LastKnownPositions` with visible entities.
  - `EntityType == "Enemy" / "Monster"` → `KnownEnemies`.
  - `"Ally"` / `"NPC"` reserved for future tags.

**Reliability**

- Ready for use given correct visual classification.
- Counts and `HasKnownThreats` behave as expected, but:
  - Forgetting of old positions depends on `MemoryMaintenanceSystem` intervals and thresholds.

---

## 3. Hearing / sound–based keys

**Examples (if adapter uses them):**

- `HeardFootsteps`
- `HeardCombat`
- `LoudSoundDirection`
- `LoudSoundDistance`

**Source / semantics**

- Backed by `PerceptionData.Auditory` + `HearingSensor`:
  - `HearingSensor` pulls recent `GlobalSoundEvents` from `ISoundEventBus`.
  - Filters by:
    - `Auditory.HearingRange`
    - `Auditory.HearingThreshold`
  - Fills `HeardSounds` with core `SoundEvent`s; helpers like `HeardCombat()` / `HeardFootsteps()` work on that list.

**Reliability**

- Conditionally ready:
  - Requires a **shared** `ISoundEventBus` instance between:
    - The `SoundEmitter` used by gameplay systems, and
    - The `PerceptionUpdateSystem` (via its `(world, w, h, logger, ISoundEventBus)` ctor).
  - Requires gameplay to actually call `SoundEmitter.Emit*` on significant events.
- If no bus wiring / no emits: these keys will effectively remain "no sound detected".

---

## 4. Awareness / threat–related keys

**Examples:**

- `IsAlert`, `IsSuspicious`, `IsInDanger`
- Threat/alert level–derived flags
- Possibly `InvestigatePosition` / `SuspiciousPosition`–style keys

**Source / semantics**

- Backed by `PerceptionData.Awareness`.
- Currently:
  - `AwarenessUpdateSystem` uses custom logic based on:
    - Visible enemies / player
    - `HeardCombat()` / `HeardFootsteps()`
    - Time since last threat.
  - Core also provides `ThreatAssessment` (not yet wired in the game layer) for a unified scoring policy.

**Reliability**

- Defined and usable, but:
  - Exact thresholds and transitions are currently in the game’s `AwarenessUpdateSystem`.
  - Sound-driven transitions depend on the hearing wiring described above.

---

## 5. Self / inventory / ability–related keys

**Examples:**

- `SelfHealth`, `SelfMaxHealth`, `SelfHealthPercent`, `IsLowHealth`
- Inventory-related (`HasInventory`, `InventoryItemCount`, `HasHealthPotion`, etc.)
- Ability/mana-related (`HasFireballAbility`, `Mana`, cooldown flags, etc.)

**Source / semantics**

- These come from ECS components (health, inventory, abilities) and **not** from `NexusPerception.Core`.

**Reliability**

- Already reliable.
- They are independent of perception and should be safe to use for goal scoring now.

---

## 6. TL;DR for GOAP

You can **trust**:

- All self/health/inventory/ability keys.
- Visual/knowledge keys, as long as entities carry `PlayerTag` / `MonsterTag` / `ItemTag` or `AIComponent` as appropriate (vision still ignores walls).

You can **trust sound-based keys** once:

- A shared `ISoundEventBus` is used by both gameplay **and** `PerceptionUpdateSystem`.
- Gameplay calls `SoundEmitter.Emit*` on significant events.

Awareness/threat keys work, but their exact semantics are currently defined by the game’s `AwarenessUpdateSystem` rather than the core `ThreatAssessment` helper.
