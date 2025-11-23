# RFC Numbering & Related Docs – Remaining Work (2025-11-21)

This note captures the remaining work on the RFC numbering and cross-reference alignment so we can safely pause and resume later.

## 1. Duplicate `doc_id` sweep (beyond 032–035 / Spade)

- **Goal:** Ensure every RFC has a unique `doc_id` across `docs/rfcs` and `docs/_inbox`.
- **Current state:**
  - Conflicts around `RFC-00033` and `RFC-00032`/`00034` have been resolved by:
    - Keeping canonical RFCs 032–035 in `docs/rfcs`:
      - `RFC-00032` – Multi-Backend Rendering Architecture
      - `RFC-00033` – Scale Config System Implementation
      - `RFC-00034` – Unified Dungeon Overlay System
      - `RFC-00035` – RFC‑014 Scene Management Completion
    - Renumbering Spade follow-up RFCs in `docs/_inbox`:
      - `RFC-00046` – Spade Performance Benchmarking Suite (`033-spade-performance-benchmarks.md`)
      - `RFC-00047` – Spade Integration Test Suite (`034-spade-integration-tests.md`)
      - `RFC-00048` – Remove Delaunator Dependency (`032-remove-delaunator-dependency.md`)
- **Remaining work:**
  - [ ] Run a full scan over `docs/` to detect any other duplicate `doc_id`s (not just around 032–035).
    - Suggested approach: script that builds a map of `doc_id` → list of files and flags any with count > 1.
  - [ ] For any additional duplicates found:
    - Decide which document keeps the original `doc_id` (usually the canonical `docs/rfcs` entry).
    - Assign a new unused RFC number to the other document(s).
    - Update:
      - YAML front-matter `doc_id` and `title`.
      - Top-level H1 heading.
      - Any `related` / `dependencies.rfcs` references.
      - `docs/index/registry.json` entry for that path.
    - Optionally: `git mv` filenames to match new numbers (see section 4).

## 2. Color Scheme RFC family (036–041)

- **Current state:**
  - Main spec: `docs/rfcs/036-color-scheme-configuration.md`
    - `doc_id: RFC-00036` (canonical)
    - Title/H1 updated to `RFC-036: Color Scheme Configuration System`.
  - Phase / implementation RFCs:
    - `RFC-00037` – Phase 2 detailed plan
    - `RFC-00038` – Phase 2 instructions
    - `RFC-00039` – Phase 3 detailed plan
    - `RFC-00040` – Phase 3 review
    - `RFC-00041` – Phase 4 detailed plan
    - These currently use "RFC-010 Phase X ..." style in titles/H1, reflecting that they were originally drafted as phases under conceptual RFC‑010.
- **Open design decision:**
  - **Option A (keep conceptual RFC‑010 naming):**
    - Leave the phase docs' titles/H1 as "RFC‑010 Phase 2/3/4 ..." while their machine identifiers remain RFC‑00037–41.
    - Pros: Preserves the idea that these are phases of one conceptual RFC.
    - Cons: Visible RFC number in H1 != `doc_id` for 037–041.
  - **Option B (fully normalize to 036–041):**
    - Rename titles/H1 so each document's visible number matches its `doc_id`:
      - `RFC-037: Color Scheme Configuration – Phase 2 Detailed Plan`
      - `RFC-038: ... Phase 2 Instructions`
      - `RFC-039: ... Phase 3 Detailed Plan`
      - `RFC-040: ... Phase 3 Review`
      - `RFC-041: ... Phase 4 Detailed Plan`
    - Update internal text and cross-references so they consistently refer to 036–041 instead of 010.
- **Remaining work:**
  - [ ] Decide between Option A vs Option B for how strictly you want H1/title to reflect `doc_id`.
  - If **Option B** is chosen:
    - [ ] Update titles and H1 headings in 037–041 to use their own RFC numbers.
    - [ ] Update any prose references that say "RFC‑010" where they should now reference 036–041.
    - [ ] Check `docs/rfcs/README.md`, `docs/INDEX.md`, and `docs/index/registry.json` for any references to 010 that should be updated to 036–041.

## 3. Spade follow-up RFC cross-references

- **Current state:**
  - Spade suite numbers are now:
    - `RFC-00046` – Spade Performance Benchmarking Suite (`docs/_inbox/033-spade-performance-benchmarks.md`).
    - `RFC-00047` – Spade Integration Test Suite (`docs/_inbox/034-spade-integration-tests.md`).
    - `RFC-00048` – Remove Delaunator Dependency (`docs/_inbox/032-remove-delaunator-dependency.md`).
  - `docs/_inbox/README-SPADE-NEXT-STEPS.md` has been updated in headings, textual sections, and timeline summary to reflect 046/047/048.
  - `docs/index/registry.json` entries for these three `_inbox` RFCs now use 046/047/048.
- **Remaining work:**
  - [ ] Tidy up any _embedded diagrams or prose_ that still show old numbers:
    - Example: the ASCII dependency diagram in `README-SPADE-NEXT-STEPS.md` still references `RFC-034`, `RFC-033`, `RFC-032` for the phases; this should be updated to 047/046/048 (or at least annotated) when you next touch this doc.
  - [ ] Run a targeted search to confirm there are no lingering references to the old Spade RFC numbers (especially `RFC-00033` when it should mean the benchmarks suite, now 046).

## 4. Optional: Filename alignment (`git mv` suggestions)

- **Current state:**
  - Some `_inbox` filenames still use their original sequence numbers even though `doc_id` has been renumbered, e.g.:
    - `docs/_inbox/032-remove-delaunator-dependency.md` → `doc_id: RFC-00048`.
    - `docs/_inbox/033-spade-performance-benchmarks.md` → `doc_id: RFC-00046`.
    - `docs/_inbox/034-spade-integration-tests.md` → `doc_id: RFC-00047`.
- **Remaining work (optional):**
  - [ ] Decide whether you want filenames to always match the RFC number.
  - If yes, the following `git mv` operations are recommended (to be done manually):
    - `git mv docs/_inbox/032-remove-delaunator-dependency.md docs/_inbox/048-remove-delaunator-dependency.md`
    - `git mv docs/_inbox/033-spade-performance-benchmarks.md docs/_inbox/046-spade-performance-benchmarks.md`
    - `git mv docs/_inbox/034-spade-integration-tests.md docs/_inbox/047-spade-integration-tests.md`
  - [ ] After renames, update any hard-coded links that mention the old filenames.

## 5. Tooling & guardrails for the future

- **Idea:** Reduce the chance of future numbering drift and duplicates.
- **Possible follow-up tasks:**
  - [ ] Add a small script (Python, Node, or .NET) that:
    - Scans `docs/` for front-matter `doc_id` values.
    - Enforces uniqueness.
    - Optionally checks that RFC filenames match their RFC numbers.
  - [ ] Integrate this script with:
    - Pre-commit (as an optional or local-only hook), or
    - CI (e.g., GitHub Actions) to fail the pipeline when duplicates are found.
  - [ ] Extend `docs/rfcs/026-rfc-numbering-convention.md` with:
    - A short section documenting the Spade renumbering (033 → 046, etc.).
    - Guidance on how to handle `_inbox` vs `rfcs` IDs and when renumbering is appropriate.

## 6. Quick "when we come back" checklist

When you return to this work:

1. **Run duplication check:**
   - Confirm there are no additional `doc_id` collisions beyond those already fixed.
2. **Decide color-scheme strategy:**
   - Choose Option A vs Option B for how strictly to align titles/H1 vs `doc_id` for RFCs 037–041.
3. **Clean up Spade diagrams:**
   - Update remaining diagrams and prose in `README-SPADE-NEXT-STEPS.md` to use 046/047/048.
4. **(Optional) Align filenames:**
   - If you care about filename/number parity, perform the `git mv` operations and fix links.
5. **(Optional) Add tooling:**
   - Implement duplicate-check script and hook it into pre-commit/CI.

This should give you enough context to quickly regain state when you pick up the RFC-numbering work again.
