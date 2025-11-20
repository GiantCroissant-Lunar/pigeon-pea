# RFC Numbering Convention - Quick Reference

**Source**: RFC-00026 (see `docs/_inbox/rfc-numbering-convention.md`)

## Key Rules

### 1. Doc ID Format
- **Format**: `PREFIX-NNNNN` (5 digits, no year)
- **Example**: `RFC-00042`, `PRD-00001`, `ADR-00015`
- **Rationale**: Year is in `created` field; shorter IDs; matches industry standards

### 2. Filename Must Match Doc ID
- **Format**: `NNN-title-slug.md` or `NNNNN-title-slug.md`
- **Example**: `doc_id: RFC-00013` → `013-plugin-architecture.md`
- **Rule**: Filename number MUST match doc_id number (without prefix)

### 3. Sequential Numbering
- **Strategy**: Sequential in order of creation (001, 002, 003...)
- **No reserved ranges**: Don't reserve 010-019 for topics
- **Use tags**: Group by topic using front-matter tags, not numbers

### 4. No Duplicates
- Each RFC gets unique number
- Phase/variant docs: Create separate RFCs with different numbers OR merge into one
- Superseded RFCs: Keep old number, new RFC gets new number

### 5. Finding Next Number
```bash
# Check highest existing number and add 1
ls docs/rfcs/ | grep "^[0-9]" | sort -n | tail -1
```

## Current Issues (as of 2025-11-20)

**Duplicate Doc IDs**:
- RFC-00014: `014-scene-management-ecs.md` AND `014-adopt-dispose-pattern-generator.md`
- RFC-00020: `020-font-pipeline-and-pua-layout.md` AND `020-nexus-goap-ai-system.md`  
- RFC-00021: `021-terminal-distribution-and-rio-integration.md` AND `021-nexus-perception-system.md`

**Resolution**: Assign new numbers to duplicates (e.g., 030, 031, 032)

## PRD-RFC Relationships

### PRD Front-Matter
```yaml
doc_id: 'PRD-00001'
doc_type: 'prd'
implementation:
  rfcs: ['RFC-00015', 'RFC-00016']  # RFCs implementing this PRD
  status: 'in-progress'
```

### RFC Front-Matter
```yaml
doc_id: 'RFC-00015'
doc_type: 'rfc'
implements: 'PRD-00001'  # PRD this RFC implements
dependencies:
  rfcs: ['RFC-00013']    # Other RFCs this depends on
```

## Migration Status

- ✅ RFC-00026 created (defines convention)
- ✅ PRD-RFC schema defined
- ✅ Sync tool created (`scripts/sync-rfc-index.py`)
- ⏳ DOCUMENTATION-SCHEMA.md updated
- ⏳ Duplicate resolution pending
- ⏳ Validation script update pending

## See Also

- Full RFC: `docs/_inbox/rfc-numbering-convention.md` (RFC-00026)
- Schema: `docs/DOCUMENTATION-SCHEMA.md`
- Sync tool: `scripts/sync-rfc-index.py`
- PRD-RFC schema: `docs/PRD-RFC-SCHEMA.md`
