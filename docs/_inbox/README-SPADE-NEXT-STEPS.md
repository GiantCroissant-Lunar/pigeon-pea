# Spade Adoption - Next Steps RFCs

This directory contains three RFCs that outline the next steps following the successful migration to Spade in the fantasy-map-generator-port.

## Overview

The fantasy-map-generator-port has been fully migrated to use Spade for all Voronoi diagram and Delaunay triangulation operations. These RFCs describe follow-up work to optimize, validate, and clean up the implementation.

## RFCs Created

### RFC-032: Remove Delaunator Dependency

**Status:** Draft
**File:** `032-remove-delaunator-dependency.md`
**Estimated Effort:** 2-3 hours
**Priority:** Medium

**Summary:** Remove the obsolete Delaunator JavaScript port dependency now that all functionality has been replaced with Spade.

**Key Tasks:**

- Search for any remaining Delaunator usage
- Remove package references from all projects
- Remove legacy Delaunator constructor from Voronoi.cs
- Update documentation
- Verify builds and tests

**Benefits:**

- Cleaner dependency graph
- Smaller binary size (~50KB reduction)
- Removes unmaintained package (last updated 2019)
- Eliminates confusion about which library to use

---

### RFC-033: Spade Performance Benchmarking Suite

**Status:** Draft
**File:** `033-spade-performance-benchmarks.md`
**Estimated Effort:** 8-12 hours
**Priority:** High

**Summary:** Create comprehensive performance benchmarks using BenchmarkDotNet to measure and validate Spade's performance improvements.

**Key Components:**

1. **Voronoi Generation Benchmarks** - Compare Spade vs NetTopologySuite vs Delaunator
2. **Lloyd Relaxation Benchmarks** - Measure iteration efficiency and quality
3. **Safety Check Overhead** - Quantify the cost of infinite loop protection
4. **End-to-End Benchmarks** - Full map generation pipeline timing

**Expected Results:**

- 10-20% faster Voronoi generation than NetTopologySuite
- 20-25% less memory allocation during Lloyd relaxation
- <2% overhead from safety checks

**CI/CD Integration:**

- Automated benchmark execution on PR
- Performance regression detection
- Results published to GitHub artifacts

---

### RFC-034: Spade Integration Test Suite

**Status:** Draft
**File:** `034-spade-integration-tests.md`
**Estimated Effort:** 12-16 hours
**Priority:** High

**Summary:** Comprehensive integration tests to ensure Spade-based geometry operations work correctly across all scenarios.

**Test Categories:**

1. **Voronoi Correctness Tests** - Topology validation, neighbor consistency
2. **Lloyd Relaxation Quality Tests** - Convergence, uniformity metrics
3. **Edge Case Tests** - Degenerate geometry, boundary conditions
4. **Integration Tests** - Full map generation pipeline
5. **Regression Tests** - Snapshot testing for known-good maps

**Success Criteria:**

- 100+ integration tests covering all scenarios
- > 80% code coverage for geometry operations
- All tests pass consistently
- Visual regression tests for key scenarios

---

## Recommended Implementation Order

### Phase 1: Validation First (Week 1-2)

**Start with RFC-034: Integration Tests**

- Establish baseline correctness
- Build confidence in the migration
- Catch any issues early

**Rationale:** Testing ensures the migration is solid before optimization and cleanup.

### Phase 2: Optimization (Week 3-4)

**Implement RFC-033: Performance Benchmarks**

- Quantify performance improvements
- Identify optimization opportunities
- Establish performance baselines

**Rationale:** Benchmarks provide data to validate performance claims and guide future optimization.

### Phase 3: Cleanup (Week 5)

**Execute RFC-032: Remove Delaunator**

- Clean up dependencies
- Remove dead code
- Finalize documentation

**Rationale:** Safe to remove once tests and benchmarks confirm everything works.

## Quick Start Guide

### For Implementation Agents

1. **Read the RFC** - Each RFC contains detailed specifications
2. **Check Dependencies** - Ensure prerequisites are met
3. **Follow the Implementation Plan** - Step-by-step tasks provided
4. **Run Success Criteria Checklist** - Verify completion
5. **Update Documentation** - Mark RFC status as implemented

### RFC Structure

Each RFC follows this format:

- **Summary** - One-paragraph overview
- **Motivation** - Why this work is needed
- **Design** - Detailed technical design
- **Implementation Plan** - Step-by-step tasks
- **Success Criteria** - Definition of done
- **Timeline** - Estimated duration

### Moving RFCs to Active

Once you start implementation:

1. Move RFC from `_inbox/` to `rfcs/`
2. Update status to `active`
3. Add implementation tracking fields
4. Create GitHub issues if needed

Example:

```bash
mv docs/_inbox/032-remove-delaunator-dependency.md docs/rfcs/
```

Then update front-matter:

```yaml
status: active
implementation:
  status: in-progress
  started: '2025-11-21'
  completion: 50
  tasks: ['task-001']
  issues: [123]
```

## Dependencies Between RFCs

```
RFC-034 (Integration Tests)
    ↓
    └─→ Should complete first (validates correctness)
         ↓
RFC-033 (Performance Benchmarks)
    ↓
    └─→ Requires working implementation
         ↓
RFC-032 (Remove Delaunator)
    └─→ Safe to execute once tests/benchmarks confirm Spade works
```

## Expected Outcomes

### After RFC-032 (Delaunator Removal)

- ✅ Cleaner dependency graph
- ✅ Smaller binaries (~50KB reduction)
- ✅ No deprecated packages
- ✅ Updated documentation

### After RFC-033 (Performance Benchmarks)

- ✅ Quantified performance improvements (10-20% faster)
- ✅ Established baseline metrics
- ✅ Automated performance regression detection
- ✅ Optimization roadmap based on data

### After RFC-034 (Integration Tests)

- ✅ 100+ integration tests
- ✅ >80% code coverage
- ✅ Regression prevention
- ✅ Quality metrics established
- ✅ Visual verification suite

## Success Metrics

| Metric        | Target        | Verification            |
| ------------- | ------------- | ----------------------- |
| Test Coverage | >80%          | Code coverage report    |
| Performance   | 10-20% faster | Benchmark results       |
| Memory Usage  | 20-25% less   | Benchmark results       |
| Binary Size   | -50KB         | Build output comparison |
| Test Count    | 100+          | Test suite execution    |

## Resources

### Documentation

- `SPADE_ADOPTION.md` - Migration documentation
- `INFINITE_LOOP_FIX.md` - Safety improvements
- `DOCUMENTATION-SCHEMA.md` - RFC format guide

### Code

- Spade Port: `dotnet/_lib/spade-port/`
- SpadeAdapter: `src/FantasyMapGenerator.Core/Geometry/SpadeAdapter.cs`
- Fantasy Map Generator: `dotnet/_lib/fantasy-map-generator-port/`

### Related RFCs

- RFC-003: Testing & Verification
- RFC-006: Plugin System Architecture
- RFC-009: Performance Benchmarking

## Questions?

If you have questions about implementing these RFCs:

1. Read the detailed RFC document
2. Check the code references in the RFC
3. Review related RFCs mentioned in dependencies
4. Consult `SPADE_ADOPTION.md` for migration context

## Timeline Summary

| RFC                  | Duration    | Effort     | Priority |
| -------------------- | ----------- | ---------- | -------- |
| RFC-034 (Tests)      | 2 weeks     | 12-16h     | High     |
| RFC-033 (Benchmarks) | 2 weeks     | 8-12h      | High     |
| RFC-032 (Cleanup)    | 1 week      | 2-3h       | Medium   |
| **Total**            | **5 weeks** | **22-31h** | -        |

## Next Actions

1. **For Planning:** Review RFCs and allocate resources
2. **For Implementation:** Start with RFC-034 (testing first approach)
3. **For Review:** Provide feedback on RFC designs before implementation
4. **For Documentation:** Move approved RFCs from `_inbox/` to `rfcs/`
