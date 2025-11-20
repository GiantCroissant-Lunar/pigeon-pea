---
created: '2025-11-20'
doc_id: RFC-00026
doc_type: rfc
status: draft
summary: Establish clear RFC numbering rules, resolve filename vs doc_id conflicts,
  and define allocation strategy for document identifiers
tags:
- documentation
- rfc
- numbering
- convention
- governance
title: RFC Numbering Convention and Document Identification
---


# RFC: RFC Numbering Convention and Document Identification

- **Status:** Draft
- **Date:** 2025-11-20
- **Related:** RFC-00012 (Documentation Organization Management)

## Summary

Establish a clear, consistent numbering convention for RFCs and other documentation to prevent:
- Duplicate filename numbers
- Gaps in numbering without explanation
- Confusion between filename numbers and doc_id numbers
- Conflicts when multiple agents create documents simultaneously

## Current Problems

### Problem 1: Duplicate Filename Numbers

Multiple RFCs share the same filename prefix:
```
013-plugin-architecture-refinement-tiered.md
013-yazi-integrated-rust-cli.md

014-adopt-dispose-pattern-generator.md
014-scene-management-ecs.md

020-font-pipeline-and-pua-layout.md
020-nexus-goap-ai-system.md

021-nexus-perception-system.md
021-terminal-distribution-and-rio-integration.md
```

### Problem 2: Two Numbering Systems

**Filename:** `013-plugin-architecture-refinement-tiered.md` (3 digits)
**doc_id:** `RFC-00013` (5 digits)

Which is the source of truth?

### Problem 3: Gaps Without Explanation

```
RFC-00001 ✓
RFC-00003 ✓ (where's 00002?)
RFC-00006 ✓
RFC-00012 ✓ (jump from 6 to 12)
RFC-00020 ✓ (jump from 12 to 20)
```

Are these gaps intentional (reserved ranges) or accidental?

### Problem 4: Phase/Variant Files

```
010-color-scheme-configuration.md
010-color-scheme-configuration-PHASE-2-DETAILED-PLAN.md
010-color-scheme-configuration-PHASE-2-INSTRUCTIONS.md
010-color-scheme-configuration-PHASE-3-DETAILED-PLAN.md
010-color-scheme-configuration-PHASE-3-REVIEW.md
010-color-scheme-configuration-PHASE-4-DETAILED-PLAN.md
```

Should these be separate RFCs or sub-documents?

## Proposed Solution

### Rule 1: doc_id is Source of Truth

**Decision:** The `doc_id` in YAML front-matter is the canonical identifier.

**Rationale:**
- Survives file renames
- Machine-readable
- Unique across all document types (RFC, GUIDE, ADR, etc.)
- Already enforced by validation

**Format:** `RFC-NNNNN` (5 digits, zero-padded, no year)
- `RFC-00001`
- `RFC-00042`
- `RFC-00150`

**Rationale:**
- Year is already in `created` field (no redundancy)
- Shorter, cleaner IDs
- Matches industry standard (Rust RFCs, Python PEPs, IETF RFCs)
- Easier to reference: "See RFC-42" instead of "RFC-00042"

### Rule 2: Filename Must Match doc_id Number

**Decision:** Filename MUST use the same number as doc_id (without prefix/year).

**Format:** `NNN-title-slug.md` or `NNNNN-title-slug.md`

**Examples:**
- `doc_id: RFC-00013` → filename: `013-plugin-architecture-refinement.md`
- `doc_id: RFC-00042` → filename: `042-game-services-architecture.md`

**Rationale:**
- Easy to find files by number
- Filename and doc_id stay in sync
- Alphabetical sort = numerical sort

### Rule 3: No Duplicate Numbers

**Decision:** Each RFC gets a unique number. No exceptions.

**For phase/variant documents:**
- **Option A (Recommended):** Create separate RFCs with different numbers
  - `RFC-00010` - Color Scheme Configuration (Main)
  - `RFC-00023` - Color Scheme Configuration Phase 2 Plan
  - `RFC-00024` - Color Scheme Configuration Phase 3 Plan

- **Option B:** Use sub-document structure in one RFC
  - Single `010-color-scheme-configuration.md`
  - Contains all phases as sections
  - No separate files

**For superseded RFCs:**
- Old RFC keeps its number
- New RFC gets new number
- Use `supersedes: [RFC-00010]` to link

### Rule 4: Sequential Numbering (No Reserved Ranges)

**Decision:** RFCs are numbered sequentially in order of creation (draft creation date).

**Numbering Strategy:**
- ✅ Sequential: 001, 002, 003, 004, 005...
- ❌ No topic-based ranges (010-019 for rendering, 020-029 for fonts)
- ❌ No gaps (unless RFC was deleted/rejected)

**Rationale:**
- Simpler to manage
- No need to decide which "range" an RFC belongs to
- Easy to find next available number

**Exception:** Gaps are allowed if:
- RFC was rejected and removed (document in git history)
- RFC number was reserved but never used (rare)

### Rule 5: Number Allocation Process

**When creating a new RFC:**

1. **Check registry:**
   ```bash
   python scripts/validate-docs.py --list-rfcs
   # Or check docs/index/registry.json
   ```

2. **Find next available number:**
   - Look at highest existing RFC number
   - Add 1

3. **Reserve the number:**
   - Create draft in `docs/_inbox/NNN-title.md`
   - Add minimal front-matter with `doc_id`
   - Commit to git (reserves the number)

4. **Complete the RFC:**
   - Write content
   - Run validation
   - Move to `docs/rfcs/`

**For multiple agents working in parallel:**
- Each agent claims a number by creating the draft file
- First to commit wins
- Others must rebase and pick new number

### Rule 6: Handling Existing Duplicates

**Resolution Strategy:**

1. **Identify canonical version** (most complete, most recent)
2. **Assign new numbers to duplicates:**
   ```
   OLD: 013-yazi-integrated-rust-cli.md
   NEW: 030-yazi-integrated-rust-cli.md

   OLD: 014-scene-management-ecs.md
   NEW: 031-scene-management-ecs.md
   ```

3. **Update doc_id in front-matter**
4. **Rename files**
5. **Update cross-references**

## Document ID Format Reference

### Prefix Conventions

| Prefix | Document Type | Example |
|--------|---------------|---------|
| `RFC` | Request for Comments | `RFC-00042` |
| `ADR` | Architecture Decision Record | `ADR-00001` |
| `GUIDE` | Implementation Guide | `GUIDE-00001` |
| `SPEC` | Technical Specification | `SPEC-00001` |
| `PLAN` | Implementation Plan | `PLAN-00015` |
| `REFERENCE` | Reference Documentation | `REFERENCE-00001` |

### Year Information

- **Not in doc_id** - Year is in `created` field only
- `doc_id: RFC-00042` (no year)
- `created: 2025-11-20` (year here)
- Numbering continues across years (don't reset to 00001 in new year)

### Number Component

- **5 digits, zero-padded**
- Sequential across all time (don't reset each year)
- Examples: `00001`, `00042`, `00150`, `01234`
- First RFC in 2025: `RFC-00001`
- Last RFC in 2025: `RFC-00150` (example)
- First RFC in 2026: `RFC-00151` (continues numbering)

## Validation Rules

### Automated Checks (in `scripts/validate-docs.py`)

1. ✅ doc_id matches filename number
2. ✅ No duplicate doc_ids
3. ✅ No duplicate filenames
4. ✅ doc_id format: `PREFIX-NNNNN` (no year)
5. ✅ Number is within valid range (00001-99999)
6. ✅ No gaps in sequence (warning only)

### Manual Review Checks

1. ✅ Number matches creation order (chronological)
2. ✅ Superseded RFCs properly linked
3. ✅ Variant documents justified (or merged)

## Migration Plan

### Phase 1: Audit Current RFCs (Week 1)

**Tasks:**
1. List all RFCs with duplicate filename numbers
2. Identify canonical version for each
3. Generate renumbering plan
4. Document in tracking issue

**Deliverable:** RFC Renumbering Plan (spreadsheet or markdown table)

### Phase 2: Resolve Duplicates (Week 2)

**For each duplicate:**
1. Assign new doc_id
2. Rename file
3. Update YAML front-matter
4. Update cross-references in other docs
5. Commit with clear message: `docs: renumber RFC-013b → RFC-030`

**Automation:**
```bash
# Script to renumber an RFC
./scripts/renumber-rfc.sh 013-yazi-cli.md 030
```

### Phase 3: Update Validation (Week 2)

**Enhance `scripts/validate-docs.py`:**
1. Add filename-to-doc_id match check
2. Add duplicate filename detection
3. Add sequential numbering check (warning for gaps)
4. Update error messages with examples

### Phase 4: Update Documentation (Week 2)

**Update:**
1. `docs/DOCUMENTATION-SCHEMA.md` - Add numbering rules
2. `CLAUDE.md` - Reference RFC numbering convention
3. `.agent/rules/documentation-management.md` - Add numbering rules

### Phase 5: Verify (Week 3)

**Final checks:**
1. Run validation on all docs
2. Verify no duplicates remain
3. Check cross-references still work
4. Update `docs/index/registry.json`

## Examples

### Correct RFC Structure

**File:** `docs/rfcs/042-game-services-architecture.md`

```yaml
---
doc_id: 'RFC-00042'
title: 'Game Services Architecture'
doc_type: 'rfc'
status: 'draft'
created: '2025-11-20'
tags: ['architecture', 'services']
summary: 'Six core game services with tiered plugin architecture'
---
```

**Validation:** ✅ Filename number (042) matches doc_id number (00042)

### Incorrect RFC Structure

**File:** `docs/rfcs/013-my-rfc.md`

```yaml
---
doc_id: 'RFC-00025'  # ❌ Number mismatch!
title: 'My RFC'
doc_type: 'rfc'
status: 'draft'
created: '2025-11-20'
---
```

**Validation:** ❌ Filename (013) doesn't match doc_id (00025)

**Fix:** Rename file to `025-my-rfc.md`

## FAQ

### Q: What if I create an RFC draft but later decide not to use it?

**A:**
- If still in `_inbox/`: Delete it (number can be reused)
- If in `docs/rfcs/`: Set `status: rejected`, keep file (don't reuse number)

### Q: Can I reserve a number for an RFC I'm planning?

**A:**
- Yes, create minimal draft in `_inbox/` with doc_id
- Complete within 2 weeks or release the number

### Q: What if two agents pick the same number?

**A:**
- First to commit wins
- Second must rebase and pick new number
- Validation will catch conflicts

### Q: Should RFCs be numbered by topic area?

**A:**
- No, use sequential numbering
- Use `tags` in front-matter for topic grouping
- Use `related` field to link related RFCs

### Q: How do I find the next available RFC number?

**A:**
```bash
# Option 1: Check registry
python scripts/validate-docs.py --next-rfc-number

# Option 2: Manual check
ls docs/rfcs/ | grep "^[0-9]" | sort -n | tail -1
# Add 1 to the last number
```

## Success Criteria

- ✅ No duplicate RFC filename numbers
- ✅ All doc_ids match filenames
- ✅ Validation enforces rules automatically
- ✅ Clear documentation of numbering convention
- ✅ Migration complete for existing duplicates
- ✅ All cross-references updated

## References

- [RFC-00012: Documentation Organization Management](../rfcs/012-documentation-organization-management.md)
- [Documentation Schema](../DOCUMENTATION-SCHEMA.md)

## Appendix: Renumbering Script

```bash
#!/bin/bash
# scripts/renumber-rfc.sh
# Usage: ./scripts/renumber-rfc.sh old-file.md new-number

OLD_FILE="$1"
NEW_NUMBER="$2"

if [ ! -f "docs/rfcs/$OLD_FILE" ]; then
    echo "Error: File not found: docs/rfcs/$OLD_FILE"
    exit 1
fi

# Extract current number from filename
OLD_NUMBER=$(echo "$OLD_FILE" | grep -oP '^\d+')

# Extract title slug from filename
TITLE_SLUG=$(echo "$OLD_FILE" | sed "s/^${OLD_NUMBER}-//")

# New filename
NEW_FILE="${NEW_NUMBER}-${TITLE_SLUG}"

# Update doc_id in front-matter
sed -i "s/RFC-${OLD_NUMBER}/RFC-${NEW_NUMBER}/g" "docs/rfcs/$OLD_FILE"

# Rename file
git mv "docs/rfcs/$OLD_FILE" "docs/rfcs/$NEW_FILE"

echo "✅ Renumbered: $OLD_FILE → $NEW_FILE"
echo "   doc_id updated: RFC-$(printf "%05d" $OLD_NUMBER) → RFC-$(printf "%05d" $NEW_NUMBER)"
```
