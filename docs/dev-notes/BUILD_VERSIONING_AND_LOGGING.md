---
canonical: true
created: '2025-11-20'
doc_id: GUIDE-2025-00004
doc_type: guide
related: []
status: active
summary: This note summarizes how we plan to version builds and organize logs under
  build/artifacts/{version}/.
supersedes: []
tags:
  - ci-cd
  - guide
  - terminal
title: Build Versioning and Logging
---

# Build Versioning and Logging

This note summarizes how we plan to version builds and organize logs under `build/_artifacts/{version}/`.

## 1. Version Source: GitVersion

We use **GitVersion.Tool** via Nuke’s `[GitVersion]` attribute:

- Nuke build field:
  - `readonly GitVersion GitVersion;`
- Helper property:
  - `GitVersionNuGet => GitVersion?.NuGetVersionV2 ?? "0.0.0-local";`
- Artifacts root:
  - `build/_artifacts/{GitVersionNuGet}/...`

Target state:

- `{version}` is a **real semantic version** derived from git (tags/branches), e.g.:
  - `0.1.0-alpha.5`, `0.3.2-feature-mapstack.3`, etc.
- `0.0.0-local` is a **fallback only** when GitVersion fails; we want to treat that as “something is wrong”, not normal.

Implementation details:

- Use `GitVersion.Tool` (6.x) as a `PackageDownload` in `_build.csproj`.
- Configure `[GitVersion]` with `NoFetch = true` to avoid SSH fetch issues in local/dev and CI.
- Log the resolved `GitVersionNuGet` at build start so we can see when GitVersion is failing and falling back.

## 2. Artifact Layout by Version

Artifacts are grouped under a versioned folder:

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

- `{version}`:
  - Derived from GitVersion once it works correctly.
  - Used as the anchor for builds, logs, and future release bundles.
- `latest/`:
  - Alias folder updated by Nuke `PublishPlayers`.
  - Mirrors the latest published players for quick dev runs and Rio integration.

## 3. Build Logs per Version

We’ll introduce a per-version **build logs** directory:

```text
build/_artifacts/{version}/build-logs/
```

Intended usage:

- Every build/publish run that produces `{version}` can drop one or more log files, e.g.:
  - `publish-players-{timestamp}.log`
  - `compile-{target}-{timestamp}.log`
- Optionally, a small metadata file (e.g. `build-metadata.json`) with:
  - Git SHA
  - Branch name
  - GitVersion info (NuGet version, informational version)
  - Target RIDs and configuration

This makes it easy to answer “what happened when we built this exact version?” without digging through CI logs.

## 4. Runtime Logs per App & Version

Each app’s published folder gets its own **runtime logs** directory:

```text
build/_artifacts/{version}/PigeonPea.Console/runtime-logs/
build/_artifacts/{version}/PigeonPea.Windows/runtime-logs/
```

Guidelines:

- When running from a published folder, the app should log to `./runtime-logs/` by default.
- Logs are tied to the exact binaries under the same `{version}` folder.
- This plays nicely with release bundles; the same layout can be mirrored inside the `releases/{version}/{platform}` bundles.

Implementation ideas:

- Use Serilog (or existing logging) with a file sink pointing to `runtime-logs`.
- Ensure the directory exists at startup (`Directory.CreateDirectory("runtime-logs")`).
- Use filenames including timestamp and maybe a short identifier for the run (e.g. `console-{yyyyMMddHHmmss}.log`).

## 5. Planned Steps

1. **Fix GitVersion integration** (Step 1)
   - Configure `[GitVersion]` with `NoFetch = true`.
   - Log the resolved GitVersion values.
   - Verify that `{version}` is no longer `0.0.0-local` when building from a proper git checkout.

2. **Add build-logs directory support in Nuke**
   - Add `BuildLogsDirectory => PublishDirectory / "build-logs"`.
   - Ensure it exists during relevant targets.
   - Optionally tee Nuke output into a per-run log file.

3. **Add runtime-logs behavior in apps**
   - On startup, create `runtime-logs` under the app base path.
   - Configure logging to write per-run log files there.

4. **Wire into release bundles**
   - Ensure the same layout appears under `releases/{version}/{platform}`.
   - Confirm that logs remain colocated with the exact app binaries and assets for debugging.
