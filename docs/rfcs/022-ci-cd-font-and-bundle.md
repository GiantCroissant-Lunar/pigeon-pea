---
doc_id: "RFC-2025-00022"
title: "CI/CD for Custom Font and Developer Bundle"
doc_type: "rfc"
status: "draft"
canonical: true
created: "2025-11-13"
tags: ["ci-cd", "fonts", "automation", "build", "deployment"]
summary: "CI jobs to build patched fonts, validate outputs, and publish versioned artifacts suitable for local development and downstream packaging with Taskfile/Nuke hooks"
supersedes: []
related: ["RFC-2025-00020", "RFC-2025-00021"]
---

# RFC-022: CI/CD for Custom Font and Developer Bundle

Status: Draft
Date: 2025-11-13
Author: Codex

## Summary

Establish CI jobs to build patched fonts, validate outputs, and publish versioned artifacts suitable for local development and downstream packaging. Provide Taskfile/Nuke hooks to run the same pipeline locally.

## Goals

- Automate font patching (PUA import) for Regular/Bold.
- Produce artifacts: `LunarSarasaMono-<version>.zip` with TTFs + LICENSE + README.
- Provide a `bundle-skeleton.zip` containing `fonts/` and `config/` templates.

## Pipeline Overview

1. Checkout repo, set up Python and FontForge (headless) + ttfautohint.
2. Build or fetch base Sarasa font (cache artifacts).
3. Patch Nerd Fonts (optional if starting from NF-patched base).
4. Run FontForge script to import `fonts/src/svg/**` into PUA.
5. Run QA checks (ttfinfo, presence of E000–E023, width metrics).
6. Upload artifacts and attach to GitHub Releases on tagged builds.

## Artifacts

- `fonts/dist/LunarSarasaMono-Regular.ttf`
- `fonts/dist/LunarSarasaMono-Bold.ttf`
- `LunarSarasaMono-<semver>.zip` (TTFs + LICENSE + README)
- `bundle-skeleton-<semver>.zip` (config/rio.toml + fonts/ placeholders)

## Local Developer Tasks

Integrate with existing `Taskfile.yml` and/or Nuke:

```
task fonts:build        # run FontForge script, produce TTFs
task fonts:test         # basic smoketests (presence and widths)
task bundle:rio:dev     # create bundle/ skeleton with local fonts
```

Nuke equivalents may wrap these where appropriate.

## Versioning

- Font package version tracks repo semver tags (e.g., `v0.2.0`).
- Embed version in font metadata name table for traceability.

## Acceptance Criteria

- CI job builds fonts deterministically on Ubuntu/Windows runners and uploads artifacts.
- Taskfile provides `fonts:build` and `bundle:rio:dev` that pass locally and in CI.

## Open Questions

- Use Nerd-patched Sarasa as base (faster) vs. patch Nerd symbols ourselves.
- Whether to include 5° angle set by default or behind a feature flag.
