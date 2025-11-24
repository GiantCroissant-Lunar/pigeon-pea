---
doc_id: RFC-00059
title: Agent Rules Hub and Distribution Strategy
doc_type: rfc
status: draft
canonical: false
created: 2025-11-24
tags: [agents, rules, docs, hub, architecture]
summary: Proposes a generic agent rules hub and distribution model decoupled from any single workspace or project.
source:
  author: human+agent
  agent: cascade
---

# Context and Motivation

The current agent rules and documentation ecosystem has evolved organically across several repositories:

- **pigeon-pea** has a `.agent/` directory and documentation schema that reflect the latest thinking.
- **lunar-snake-hub** contains an older `agents/` tree and pointer scripts that are tightly coupled to the historical Lablab-Bean project.
- Several library projects (e.g., Modern Satsuma, Modern Edgar, Fantasy Map Generator Port) now include local `.agent/` folders and `AGENTS.md` / `CLAUDE.md` files.

This creates a risk of **rule drift**, duplicate maintenance, and accidental coupling between otherwise independent libraries and workspaces.

This RFC captures the desired end-state and proposed migration path for a **generic agent rules hub** that can serve multiple workspaces without hard-wiring any specific project (such as pigeon-pea or Lablab-Bean) into library repositories.

# Goals

- **G1: Generic rules hub**
  - Define a single, generic source of truth for agent rules, adapters, and scripts that is not tied to a specific app or game.

- **G2: Workspace-level consumption**
  - Allow *workspaces* (like pigeon-pea) to consume and extend the rules hub, without individual libraries knowing about the hub or its repository layout.

- **G3: Decoupled libraries**
  - Ensure library repos only reference abstract concepts like `<workspace-root>/.agent`, `<workspace-root>/AGENTS.md`, and optional shared caches (for example `~/.cache/lunar-rules/vX.Y.Z`).

- **G4: Documentation alignment**
  - Align the hub with the existing `DOCUMENTATION-SCHEMA.md` and documentation practices used in pigeon-pea.

- **G5: Incremental migration**
  - Avoid breaking existing workspaces; migration from legacy rules to the new hub can be staged and opt-in.

# Non-Goals

- **N1: Immediate rebuild of lunar-snake-hub**
  - This RFC does not require that `lunar-snake-hub` be fully refactored immediately. It defines a direction and a phased approach.

- **N2: Forcing all projects to use the hub**
  - Workspaces and repos may continue to use local `.agent/` rules without consuming the hub, as long as they remain self-consistent.

- **N3: Finalizing package technology**
  - This document does not choose between Python (PyPI), npm, NuGet, or other packaging ecosystems. It describes patterns that can be implemented in one or more ecosystems later.

# Current State (Summary)

## pigeon-pea

- Uses `.agent/` at the workspace root for rules, policies, and workflows.
- Uses `docs/DOCUMENTATION-SCHEMA.md` as the canonical schema for documentation front-matter.
- New documentation typically starts in `docs/_inbox/` and is promoted once it is stable and schema-compliant.
- Several library repos under `dotnet/_lib/` (Modern Satsuma, Modern Edgar, Fantasy Map Generator Port) have local `.agent/local/overrides.md` and project-specific `AGENTS.md` / `CLAUDE.md` files that:
  - Describe rule priority (workspace → project → stack/base).
  - Refer only to generic `<workspace-root>` concepts, not to a particular parent repo.

## lunar-snake-hub (Legacy State)

- Contains an `agents/` directory with rules, adapters, and scripts originally cloned from the Lablab-Bean project.
- `agents/rules/00-index.md` and related files are strongly coupled to the Lablab-Bean domain (dungeon crawler, xterm.js, PM2, etc.).
- `agents/scripts/generate_pointers.py` generates `CLAUDE.md`, `AGENTS.md`, `.github/copilot-instructions.md`, `.windsurf/rules.md`, and Kiro steering pointers with Lablab-Bean-specific wording and tech stack.
- Pre-commit checks (e.g., `precommit/checks/general/validate_agent_pointers.py`) assume the presence of a `.agent/scripts/generate_pointers.py` script and root-level pointer files.

This structure makes `lunar-snake-hub` unsuitable as a **generic** hub without significant cleanup and generalization.

# Proposed Direction

## 1. Use pigeon-pea/.agent as the design reference

- Treat the `.agent/` directory in pigeon-pea as the **current best-practice reference** for:
  - Directory structure.
  - Rule priority semantics (workspace → project → stack/base).
  - Integration with the documentation schema.
- Do **not** have library repos reference pigeon-pea directly; they should only talk about `<workspace-root>` and optional caches.

## 2. Rebuild the hub as a generic rules source

- Re-purpose `lunar-snake-hub` (or a successor repo) into a generic rules hub that:
  - Provides a `.agent/` tree containing **project-agnostic** base rules and adapters.
  - Avoids any direct mention of specific products (Lablab-Bean, pigeon-pea, etc.) in the *base* rule set.
  - May support optional, pluggable profiles for specific workspaces or products if needed.

- As part of this, Lablab-Bean-specific content should be:
  - Removed, or
  - Moved into clearly marked legacy/profile areas that are not part of the generic base.

## 3. Define workspace-level consumption patterns

Workspaces, not libraries, should opt in to using the hub via one or more mechanisms:

- **Pattern A: Git + Task runner**
  - Clone or fetch the hub into a cache location.
  - Copy or sync the generic `.agent/` tree into the workspace.
  - Optionally wire up pre-commit checks from the hub.

- **Pattern B: Package manager transport**
  - Publish a `lunar-rules` package (Python, npm, NuGet, etc.) that ships:
    - A `.agent/` directory tree.
    - Optional pre-commit or CI/validation scripts.
  - Provide a small CLI (e.g., `lunar-rules sync`) that:
    - Syncs rules into `<workspace-root>/.agent/`.
    - Optionally populates `~/.cache/lunar-rules/vX.Y.Z/...` with stack/base rules.

In all cases:

- Library repos remain unaware of the hub’s repository or package identity.
- They only refer to generic locations controlled by the *workspace*.

## 4. Keep libraries self-contained and generic

- Library repos (e.g., Modern Satsuma, Modern Edgar, FMG port) should:
  - Maintain their own `.agent/local/overrides.md` for project-specific behavior.
  - Use `AGENTS.md` and `CLAUDE.md` to describe rule priority and project layout.
  - Refer only to:
    - `<workspace-root>/.agent`, `<workspace-root>/AGENTS.md`.
    - Optional generic caches (e.g., `~/.cache/lunar-rules/v1.0.0/stacks/dotnet/`).
  - Avoid any mention of specific upstream workspaces (e.g., pigeon-pea) or hub repo paths.

# Migration Phases (High-Level)

1. **Phase 0  Documentation (this RFC)**
   - Capture the intent and constraints for the agent rules hub.
   - Mark this RFC as `draft` until a concrete implementation plan is agreed.

2. **Phase 1  Audit and Deprecation**
   - Audit `lunar-snake-hub` for Lablab-Bean-specific content.
   - Add clear deprecation notices indicating that the current `agents/` tree is not generic and should not be used as-is.

3. **Phase 2  Generic Base Rules**
   - Design a project-agnostic base rule set aligned with pigeon-pea’s `.agent/` directory structure and documentation schema.
   - Implement these rules in the hub, separate from any legacy or profile-specific content.

4. **Phase 3  Distribution Mechanism**
   - Choose and implement one or more distribution mechanisms (Task-based sync, Python package, npm package, etc.).
   - Document how a workspace can adopt the hub (bootstrap instructions, validation commands).

5. **Phase 4  Workspace Adoption**
   - Gradually migrate workspaces (including pigeon-pea) to consume the generic hub rules instead of maintaining divergent copies.
   - Ensure that any migration does not break existing automation or agent behavior.

# Open Questions

- **Q1: Packaging ecosystem priority**
  - Which ecosystem should be the first-class transport for `lunar-rules` (Python, npm, both)?

- **Q2: Versioning and compatibility**
  - How should workspaces declare which version of the hub rules they are compatible with?
  - How strict should validation be when versions diverge (warnings vs. hard failures)?

- **Q3: Profiles vs. pure generic base**
  - Should the hub support optional "profiles" for specific products (e.g., a pigeon-pea profile) layered on top of the generic base?

- **Q4: Documentation index integration**
  - How should the hub integrate with documentation indexing and validation tools across multiple workspaces?

- **Q5: Governance**
  - Who owns changes to the generic rules hub?
  - What process should exist for updating rules that affect many workspaces and agents?

# Status

- This RFC is a **draft** used to capture direction and open questions.
- No immediate code changes are required.
- Implementation can proceed in small, incremental steps once priorities allow.
