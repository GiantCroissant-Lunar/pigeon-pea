---
canonical: true
created: '2025-11-20'
doc_id: GUIDE-2025-00003
doc_type: guide
related: []
status: active
summary: This document describes where build, runtime, and benchmark artifacts are
  written in the PigeonPea repository. The goal is to centralize generated outputs
  under build/artifacts so they are easy to dis
supersedes: []
tags:
- ci-cd
- documentation
- guide
- rendering
- terminal
title: Artifacts Layout
---

# Artifacts Layout

This document describes where build, runtime, and benchmark artifacts are written in the PigeonPea repository. The goal is to centralize generated outputs under `build/_artifacts` so they are easy to discover, easy to clean, and excluded from source control.

## Git Ignore Conventions

- `build/.gitignore` contains:
  - `_artifacts/`
  - `bin/`
  - `obj/`
  - `*.nupkg`
  - `.nuget/`
- Everything under `build/_artifacts/` is treated as generated output and **must not** be committed.

## Game / Player Artifacts

Canonical location for published game players (console + Windows):

```text
build/_artifacts/
  {version}/
    PigeonPea.Console/
    PigeonPea.Windows/
    build-logs/
  latest/
    PigeonPea.Console/
    PigeonPea.Windows/
```

- `{version}` is derived from GitVersion (`GitVersion.SemVer`) via Nuke.
- `latest/` is an alias folder that is cleaned and repopulated on each `PublishPlayers` run.
- The `Taskfile` uses these locations:
  - `game:publish-latest` → runs Nuke `PublishPlayers` (writes to `{version}` and `latest`).
  - `game:run-latest-console` → runs `build/_artifacts/latest/PigeonPea.Console/PigeonPea.Console.exe`.

### Build Logs

Per-version build logs live alongside the published players:

```text
build/_artifacts/{version}/build-logs/
  publish-players-*.log
```

- Logs are written by Nuke `Publish` and `PublishPlayers` targets.
- Each log entry captures:
  - Timestamp
  - Resolved version
  - Runtime identifier (RID)
  - Configuration
  - Project name

### Runtime Logs

Individual players are responsible for their own runtime logs. They write into subfolders under their publish directory, e.g.:

```text
build/_artifacts/{version}/PigeonPea.Console/runtime-logs/
build/_artifacts/{version}/PigeonPea.Windows/runtime-logs/
```

- Exact filenames and formats are defined by each app (console / Windows).
- These logs are per-run and safe to delete when troubleshooting or cleaning local state.

## Benchmark Artifacts

BenchmarkDotNet artifacts are also centralized under `build/_artifacts`:

```text
build/_artifacts/benchmarks/BenchmarkDotNet.Artifacts/
  results/
  logs/
```

- The benchmarks runner (`dotnet/benchmarks/Program.cs`) configures BenchmarkDotNet to use this path by default.
- The `BENCHMARKDOTNET_ARTIFACTS` environment variable, if set, still overrides the path.

### CI Integration

The GitHub `Performance Benchmarks` workflow (`.github/workflows/benchmarks.yml`):

- Uploads benchmark results from:

  ```text
  build/_artifacts/benchmarks/BenchmarkDotNet.Artifacts/**/*
  ```

- Reads rendering benchmark JSON results from (relative to `working-directory: dotnet`):

  ```text
  ../build/_artifacts/benchmarks/BenchmarkDotNet.Artifacts/results/PigeonPea.Benchmarks.RenderingBenchmarks-report.json
  ```

- Comments on PRs using the same `build/_artifacts/benchmarks/BenchmarkDotNet.Artifacts/results/` path.

## Legacy / Tooling Locations

The following locations are legacy or tooling-only and should not be used for new artifacts:

- `build/artifacts/BenchmarkDotNet/`
  - Contains logs from earlier benchmark runs.
  - New runs do **not** write here anymore.
  - Safe to prune locally as needed.

- `build/nuke/artifacts/`
  - Originally created by the Nuke template.
  - Currently unused.
  - Safe to delete; should remain empty going forward.

- Root `artifacts/`
  - Previously used for `dotnet-format-analyzers.json` (now empty).
  - Currently not part of the core artifact flow.

## Rules of Thumb

- **All build and runtime artifacts for game players** must live under `build/_artifacts/`.
- **Benchmarks** must write under `build/_artifacts/benchmarks/BenchmarkDotNet.Artifacts/`.
- If a new tool or workflow needs to emit files, prefer a subfolder under `build/_artifacts/` rather than creating new top-level `artifacts` directories.
- Source-controlled configuration or reference files should live under `docs/`, `dotnet/`, or other appropriate source directories—not under `build/_artifacts`.
