---
name: dotnet-build
version: 0.2.0
kind: cli
description: Build .NET solution/projects using dotnet CLI. Use when task involves compiling, restoring dependencies, or building artifacts.
inputs:
  target: [solution, project, all]
  configuration: [Debug, Release]
  project_path: string # Optional, defaults to ./dotnet/PigeonPea.sln
contracts:
  success: 'Build completes with zero errors; artifacts in bin/'
  failure: 'Non-zero exit code or compilation errors'
---

# .NET Build Skill (Entry Map)

> **Goal:** Guide agent to the exact build procedure needed for the PigeonPea .NET solution.

## Quick Start (Pick One)

- **Build entire solution** → `references/build-solution.md`
- **Restore dependencies only** → `references/restore-deps.md`

## When to Use

Use this skill when you need to:

- Compile .NET code from solution (.sln) or project (.csproj) files
- Restore NuGet package dependencies
- Build specific configurations (Debug/Release)
- Generate build artifacts (binaries, assemblies)
- Prepare code for testing or deployment
- Verify that code compiles without errors

**Do NOT use for:**

- Running tests (use `dotnet-test` skill instead)
- Code formatting (use `code-format` skill instead)
- Static analysis (use `code-analyze` skill instead)
- Publishing/packaging applications

## Inputs & Outputs

### Inputs

- **target**: `solution` | `project` | `all`
  - `solution`: Build the entire PigeonPea.sln
  - `project`: Build a specific .csproj file
  - `all`: Build all projects individually
- **configuration**: `Debug` | `Release`
  - `Debug`: Include debug symbols, no optimizations
  - `Release`: Optimizations enabled, minimal debug info
- **project_path**: Path to solution or project file
  - Default: `./dotnet/PigeonPea.sln`
  - Can specify individual project like `./dotnet/console-app/PigeonPea.Console.csproj`

### Outputs

- **artifact_path**: Location of build outputs (`bin/` directories)
- **build_log**: Console output with errors, warnings, and build status
- **exit_code**: 0 for success, non-zero for failure

## Guardrails

- **Operating Directory**: Work within `./dotnet` directory only
- **Never Commit**: Do not commit `bin/`, `obj/`, or build artifacts
- **Idempotent**: Safe to run multiple times; rebuilding is deterministic
- **Dependency Check**: Always restore dependencies before building
- **Clean State**: Build artifacts are placed in standard output directories

## Navigation

### 1. Build Entire Solution

**When to use:** Most common scenario - build all projects in PigeonPea.sln

**Reference:** `references/build-solution.md`

**Quick command:**

```bash
cd ./dotnet
dotnet build PigeonPea.sln
```

### 2. Restore Dependencies

**When to use:** NuGet packages missing or need to be refreshed

**Reference:** `references/restore-deps.md`

**Quick command:**

```bash
cd ./dotnet
dotnet restore PigeonPea.sln
```

## Common Patterns

```bash
# Standard build (Debug)
cd ./dotnet && dotnet build PigeonPea.sln

# Production build (Release)
cd ./dotnet && dotnet build PigeonPea.sln -c Release

# Build specific project
cd ./dotnet && dotnet build console-app/PigeonPea.Console.csproj

# Clean and rebuild
cd ./dotnet && dotnet clean PigeonPea.sln && dotnet build PigeonPea.sln

# Restore + build workflow
cd ./dotnet && dotnet restore PigeonPea.sln && dotnet build PigeonPea.sln --no-restore
```

## Troubleshooting

### Problem: Build Fails with Errors

**Solution:** Read the detailed procedure in `references/build-solution.md`

Common causes:

- Syntax errors in C# code
- Missing dependencies (run `dotnet restore` first)
- Target framework mismatch
- SDK version incompatibility

### Problem: Missing NuGet Packages

**Solution:** Read `references/restore-deps.md` for dependency restoration

Quick fix:

```bash
dotnet restore PigeonPea.sln --force
```

### Problem: Build is Slow

**Possible causes:**

- First build (all dependencies being restored)
- Full rebuild instead of incremental build
- Large solution with many projects

**Optimization:**

```bash
# Build specific project instead of entire solution
dotnet build console-app/PigeonPea.Console.csproj
```

### Problem: Stale or Corrupted Artifacts

**Solution:**

```bash
# Clean all build outputs
dotnet clean PigeonPea.sln

# Delete obj and bin directories
find . -type d -name "bin" -o -name "obj" | xargs rm -rf

# Rebuild from scratch
dotnet build PigeonPea.sln
```

## Solution Structure

The PigeonPea solution (`./dotnet/PigeonPea.sln`) contains:

- **console-app**, **shared-app**, **windows-app** - Main projects
- **console-app.Tests**, **shared-app.Tests**, **windows-app.Tests** - Unit tests
- **benchmarks** - Performance benchmarks

## Success Criteria

A successful build produces:

- Exit code: 0
- Build artifacts in each project's `bin/` directory
- No compilation errors
- Warnings are acceptable but should be reviewed

## Related Skills

- **dotnet-test**: Run unit tests after building
- **code-format**: Format code before building
- **code-analyze**: Run static analysis on compiled code

## Version History

- **0.2.0**: Progressive disclosure pattern with reference files
- **0.1.0**: Initial skill definition
