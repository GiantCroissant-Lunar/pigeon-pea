---
doc_id: 'PLAN-2025-00013'
title: 'RFC-004 Implementation: Execution Order'
doc_type: 'plan'
status: 'active'
canonical: true
created: '2025-11-10'
tags: ['rfc-004', 'agent-infrastructure', 'execution-order', 'parallelization']
summary: 'Execution order for implementing RFC-004 issues showing parallel vs serial dependencies'
related: ['RFC-2025-00004', 'PLAN-2025-00008']
---

# RFC-004 Implementation: Execution Order

This document provides a clear execution order for implementing RFC-004 issues, showing which can be done in **parallel** vs **serial**.

## Quick Reference

- 🔴 **Serial** - Must wait for dependencies
- 🟢 **Parallel** - Can work simultaneously with others in same group
- 🔵 **Optional** - Can be done anytime or skipped

---

## Execution Groups

### Group 1: Foundation (SERIAL - Must be first)

**#94: Create .agent subdirectories** 🔴

- **Dependencies**: None
- **Must complete first** - Everything else depends on this
- **Assignee**: 1 agent
- **Estimated time**: 1-2 hours

**Block:** All other issues depend on #94 completing first.

---

### Group 2: Core Manifests (PARALLEL after #94)

All of these can be worked on **simultaneously** by different agents after #94 is complete:

**#95: Create orchestrator agent** 🟢

- **Dependencies**: #94
- **Can work in parallel with**: #96, #97, #98, #99
- **Estimated time**: 2-3 hours

**#96: Create DotNetBuildAgent** 🟢

- **Dependencies**: #94
- **Can work in parallel with**: #95, #97, #98, #99
- **Estimated time**: 2-3 hours

**#97: Create dotnet-build skill** 🟢

- **Dependencies**: #94
- **Can work in parallel with**: #95, #96, #98, #99
- **Estimated time**: 4-6 hours (entry + 2 references)

**#98: Create JSON schemas** 🟢

- **Dependencies**: #94
- **Can work in parallel with**: #95, #96, #97, #99
- **Estimated time**: 3-4 hours

**#99: Create policy files** 🟢

- **Dependencies**: #94
- **Can work in parallel with**: #95, #96, #97, #98
- **Estimated time**: 2-3 hours

**Parallel Execution**: 5 agents can work simultaneously on these issues.

---

### Group 3: Additional Sub-Agents (PARALLEL after #94)

**#100: Create CodeReviewAgent** 🟢

- **Dependencies**: #94
- **Can work in parallel with**: #101, and all Group 2 issues
- **Estimated time**: 2-3 hours

**#101: Create TestingAgent** 🟢

- **Dependencies**: #94
- **Can work in parallel with**: #100, and all Group 2 issues
- **Estimated time**: 2-3 hours

**Parallel Execution**: Can be done alongside Group 2 (total 7 agents in parallel).

---

### Group 4: Additional Skills (PARALLEL after #94 + #97)

These should wait for #97 to establish the skill pattern, but can then run in parallel:

**#102: Create dotnet-test skill** 🟢

- **Dependencies**: #94, #97 (for pattern reference)
- **Can work in parallel with**: #103, #104
- **Estimated time**: 4-6 hours

**#103: Create code-format skill** 🟢

- **Dependencies**: #94, #97
- **Can work in parallel with**: #102, #104
- **Estimated time**: 4-6 hours

**#104: Create code-analyze skill** 🟢

- **Dependencies**: #94, #97
- **Can work in parallel with**: #102, #103
- **Estimated time**: 4-6 hours

**Parallel Execution**: 3 agents can work simultaneously after #97 is done.

---

### Group 5: Validation Scripts (PARALLEL after #98)

**#105: Create skill validation script** 🟢

- **Dependencies**: #98 (schemas must exist)
- **Can work in parallel with**: #106
- **Estimated time**: 3-4 hours

**#106: Create agent validation script** 🟢

- **Dependencies**: #98
- **Can work in parallel with**: #105
- **Estimated time**: 2-3 hours

**Parallel Execution**: 2 agents can work simultaneously after #98.

---

### Group 6: Registry Generation (SERIAL after some manifests)

**#107: Create registry generation script** 🔴

- **Dependencies**: #95, #96, #97 (needs at least one agent + one skill to test with)
- **Should wait for**: At least a few manifests from Groups 2-4
- **Estimated time**: 3-4 hours

**Note**: This can technically start after #95 + #97 are done, but works better with more manifests available.

---

### Group 7: CI/CD Integration (SERIAL after validation scripts)

**#108: Add pre-commit hooks integration** 🔴

- **Dependencies**: #105, #106 (validation scripts must exist)
- **Must wait for**: Both validation scripts
- **Estimated time**: 2-3 hours

**#109: Create Taskfile.yml** 🔴

- **Dependencies**: #105, #106, #107
- **Should wait for**: All automation scripts
- **Estimated time**: 2-3 hours

---

### Group 8: Optional Enhancements (PARALLEL - anytime after #94)

**#110: Provider-specific hints** 🔵

- **Dependencies**: #94
- **Can be done anytime or skipped**
- **Estimated time**: 2-3 hours

**#111: Nuke build skill** 🔵

- **Dependencies**: #94, #97
- **Only if Nuke is added to the project**
- **Can be done anytime or skipped**
- **Estimated time**: 4-6 hours

---

### Group 9: Documentation (SERIAL - last)

**#112: Update AGENTS.md** 🔴

- **Dependencies**: #107 (registry generation)
- **Should be done last** after most issues complete
- **Estimated time**: 2-3 hours

---

## Recommended Execution Strategies

### Strategy 1: Maximum Parallelism (7+ agents available)

**Wave 1** (1 agent):

- Agent A: #94

**Wave 2** (7 agents, after #94):

- Agent A: #95 (orchestrator)
- Agent B: #96 (DotNetBuildAgent)
- Agent C: #97 (dotnet-build skill)
- Agent D: #98 (schemas)
- Agent E: #99 (policies)
- Agent F: #100 (CodeReviewAgent)
- Agent G: #101 (TestingAgent)

**Wave 3** (5 agents, after #97 + #98):

- Agent A: #102 (dotnet-test skill)
- Agent B: #103 (code-format skill)
- Agent C: #104 (code-analyze skill)
- Agent D: #105 (skill validation)
- Agent E: #106 (agent validation)

**Wave 4** (1 agent, after Wave 3):

- Agent A: #107 (registry generation)

**Wave 5** (2 agents, after #105 + #106):

- Agent A: #108 (pre-commit hooks)
- Agent B: #109 (Taskfile)

**Wave 6** (1 agent, after #107):

- Agent A: #112 (AGENTS.md update)

**Optional** (anytime after #94):

- #110, #111 as needed

**Total time**: ~4 waves (1-2 days with multiple agents)

---

### Strategy 2: Moderate Parallelism (3-4 agents available)

**Wave 1** (1 agent):

- Agent A: #94

**Wave 2** (3 agents, after #94):

- Agent A: #95, #97 (orchestrator + dotnet-build skill)
- Agent B: #96, #98 (DotNetBuildAgent + schemas)
- Agent C: #99, #100, #101 (policies + sub-agents)

**Wave 3** (3 agents, after #97 + #98):

- Agent A: #102, #105 (dotnet-test + skill validation)
- Agent B: #103, #106 (code-format + agent validation)
- Agent C: #104 (code-analyze)

**Wave 4** (1 agent, after Wave 3):

- Agent A: #107, #108 (registry + pre-commit)

**Wave 5** (1 agent):

- Agent A: #109, #112 (Taskfile + AGENTS.md)

**Total time**: ~5 waves (2-3 days)

---

### Strategy 3: Serial Execution (1 agent)

Follow this order if only one agent is available:

1. #94 (foundation)
2. #98 (schemas - needed for validation later)
3. #99 (policies)
4. #95 (orchestrator)
5. #96 (DotNetBuildAgent)
6. #97 (dotnet-build skill - sets pattern)
7. #100 (CodeReviewAgent)
8. #101 (TestingAgent)
9. #102 (dotnet-test skill)
10. #103 (code-format skill)
11. #104 (code-analyze skill)
12. #105 (skill validation - needs #98)
13. #106 (agent validation - needs #98)
14. #107 (registry generation - needs manifests)
15. #108 (pre-commit hooks - needs #105, #106)
16. #109 (Taskfile - needs #105, #106, #107)
17. #112 (AGENTS.md - needs #107)
18. #110, #111 (optional)

**Total time**: ~5-7 days (1 agent, sequential)

---

## Dependency Graph (Visual)

```
#94 (Foundation)
 │
 ├─────┬─────┬─────┬─────┬─────┬─────┐
 │     │     │     │     │     │     │
 #95   #96   #97   #98   #99  #100  #101
 │     │     │     │     │     │     │
 │     │     │     │     │     │     │
 │     │     ├─────┼─────┼─────┘     │
 │     │     │     │     │           │
 │     │    #102  #103  #104         │
 │     │           │     │           │
 │     │           └─────┴───────────┘
 │     │                 │
 │     │                #105 (skill validation)
 │     │                 │
 │     │                #106 (agent validation)
 │     │                 │
 ├─────┴─────────────────┤
 │                       │
#107 (registry)         #108 (pre-commit)
 │                       │
 │                      #109 (Taskfile)
 │                       │
 └───────────────────────┤
                         │
                        #112 (AGENTS.md)

Optional (anytime after #94):
#110 (provider hints)
#111 (nuke skill)
```

---

## Critical Path

The **critical path** (longest serial dependency chain) is:

```
#94 → #98 → #105 → #108 → #109
```

This is the minimum time required even with infinite parallel agents.

**Critical path time**: ~12-15 hours

---

## Validation Checkpoints

After each wave, verify:

✅ **After Group 2**:

- Directory structure exists
- At least one agent and one skill manifest created
- Schemas validate correctly

✅ **After Group 4**:

- All skills follow the same pattern
- Progressive disclosure maintained (entry ~200 lines, refs 200-300)

✅ **After Group 5**:

- Validation scripts work on existing manifests
- No schema violations

✅ **After Group 7**:

- Pre-commit hooks run successfully
- `task check` command works

✅ **After Group 9**:

- AGENTS.md has complete registry
- All manifests documented

---

## Tips for Parallel Execution

1. **Assign clear ownership**: Each agent should own specific issue numbers
2. **Avoid conflicts**: Different directories/files = no merge conflicts
3. **Share patterns early**: First agent doing #97 (skill) should share pattern
4. **Regular syncs**: If working in parallel, sync every 2-4 hours
5. **Branch strategy**:
   - Each agent can work on separate branches
   - OR use feature branch per group/wave
6. **Communication**: Use issue comments to coordinate

---

## Issue Summary Table

| Issue | Title               | Type          | Dependencies     | Parallel Group     | Estimated |
| ----- | ------------------- | ------------- | ---------------- | ------------------ | --------- |
| #94   | Create directories  | Foundation    | None             | Wave 1 (Serial)    | 1-2h      |
| #95   | Orchestrator        | Agent         | #94              | Wave 2 (Parallel)  | 2-3h      |
| #96   | DotNetBuildAgent    | Agent         | #94              | Wave 2 (Parallel)  | 2-3h      |
| #97   | dotnet-build skill  | Skill         | #94              | Wave 2 (Parallel)  | 4-6h      |
| #98   | JSON schemas        | Schema        | #94              | Wave 2 (Parallel)  | 3-4h      |
| #99   | Policy files        | Policy        | #94              | Wave 2 (Parallel)  | 2-3h      |
| #100  | CodeReviewAgent     | Agent         | #94              | Wave 2 (Parallel)  | 2-3h      |
| #101  | TestingAgent        | Agent         | #94              | Wave 2 (Parallel)  | 2-3h      |
| #102  | dotnet-test skill   | Skill         | #94, #97         | Wave 3 (Parallel)  | 4-6h      |
| #103  | code-format skill   | Skill         | #94, #97         | Wave 3 (Parallel)  | 4-6h      |
| #104  | code-analyze skill  | Skill         | #94, #97         | Wave 3 (Parallel)  | 4-6h      |
| #105  | Skill validation    | Automation    | #98              | Wave 3 (Parallel)  | 3-4h      |
| #106  | Agent validation    | Automation    | #98              | Wave 3 (Parallel)  | 2-3h      |
| #107  | Registry generation | Automation    | #95, #96, #97    | Wave 4 (Serial)    | 3-4h      |
| #108  | Pre-commit hooks    | CI/CD         | #105, #106       | Wave 5 (Serial)    | 2-3h      |
| #109  | Taskfile            | Automation    | #105, #106, #107 | Wave 5 (Serial)    | 2-3h      |
| #110  | Provider hints      | Optional      | #94              | Anytime (Optional) | 2-3h      |
| #111  | Nuke skill          | Optional      | #94, #97         | Anytime (Optional) | 4-6h      |
| #112  | AGENTS.md update    | Documentation | #107             | Wave 6 (Serial)    | 2-3h      |

**Total estimated time**:

- Serial (1 agent): ~55-75 hours (5-7 days)
- Parallel (7 agents): ~12-20 hours (1-2 days)
- Moderate (3-4 agents): ~20-30 hours (2-3 days)
