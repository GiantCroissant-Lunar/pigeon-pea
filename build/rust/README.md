# Rust Build System

This directory contains the cargo-make build configuration for Rust projects in pigeon-pea.

## Overview

The build system uses [cargo-make](https://github.com/sagiegurari/cargo-make) to orchestrate Rust builds and integrate with the existing GitVersion-based versioning system. Built artifacts are published to `build/_artifacts/{version}/` alongside C# artifacts from Nuke builds.

## Prerequisites

### Windows

- Rust toolchain
- GitVersion (installed globally)
- PowerShell

### Linux/macOS

- Rust toolchain
- .NET SDK (for GitVersion)
- GitVersion.Tool: `dotnet tool install --global GitVersion.Tool`
- Bash

## Installation

Install cargo-make:

```bash
cargo install --force cargo-make
```

Or use the Taskfile command:

```bash
task rust:install-cargo-make
```

## Usage

### Using Taskfile (Recommended)

The easiest way to build Rust projects is through the Taskfile tasks:

```bash
# Build debug version
task rust:build:debug

# Build release version
task rust:build:release

# Run tests
task rust:test

# Build and publish versioned artifacts
task rust:publish

# Launch Rio terminal with latest built binary
task rust:run-latest

# Build, publish, and run in one command
task rust:build-and-run

# Clean build artifacts
task rust:clean
```

#### Quick Start - Development Workflow

```bash
# First time: Install cargo-make
task rust:install-cargo-make

# Build and run the latest version in Rio terminal
task rust:build-and-run
```

This will:

1. Build the release binary with cargo
2. Copy it to `build/_artifacts/{version}/dev-tool-server/`
3. Launch Rio terminal configured to run the built binary

### Using cargo-make Directly

You can also use cargo-make directly from this directory:

```bash
cd build/rust

# Build release
cargo make build-release

# Build and publish
cargo make publish

# See all available tasks
cargo make --list-all-steps
```

## Available Tasks

### Taskfile Tasks

#### `rust:build:debug`

Build the project in debug mode.

#### `rust:build:release`

Build the project in release mode with optimizations.

#### `rust:test`

Run all tests for the project.

#### `rust:publish`

Complete workflow: build release binary and copy to versioned artifacts directory.

#### `rust:run-latest`

Launch Rio terminal with the latest built dev-tool-server binary. Automatically finds the latest version in `build/_artifacts/`.

#### `rust:build-and-run`

Convenience task that builds, publishes, and launches the latest version in Rio terminal.

#### `rust:clean`

Remove all build artifacts from the target directory.

### cargo-make Tasks (Internal)

#### `build-debug`

Build the project in debug mode.

#### `build-release`

Build the project in release mode with optimizations.

#### `test`

Run all tests for the project.

#### `clean`

Remove all build artifacts from the target directory.

#### `publish`

Complete workflow: build release binary and copy to versioned artifacts directory.

#### `get-version`

Get the semantic version from GitVersion. Used internally by other tasks.

#### `copy-artifacts`

Copy built binaries and metadata to the versioned artifacts directory. Used internally by `publish`.

## Output Structure

After running `task rust:publish` or `cargo make publish`, artifacts are created in:

```
build/_artifacts/{version}/dev-tool-server/
├── dev-tool-server(.exe)    # The built binary
├── version.txt               # Version metadata
└── README.md                 # Usage instructions
```

Example version: `build/_artifacts/0.0.1-alpha.1/dev-tool-server/`

## Configuration

The build is configured in `Makefile.toml`:

- **PROJECT_NAME**: Name of the Rust project binary
- **PROJECT_PATH**: Relative path to the Rust project
- **ARTIFACTS_BASE**: Base directory for versioned artifacts

To build additional Rust projects, create a new `Makefile.toml` or modify the existing one to support multiple projects.

## Cross-Platform Support

The build system works across:

- **Windows**: Uses PowerShell scripts and `.exe` binaries
- **Linux**: Uses Bash scripts and ELF binaries
- **macOS**: Uses Bash scripts and Mach-O binaries

Platform-specific logic is handled automatically by cargo-make's conditional execution.

## Integration with GitVersion

The build system integrates with GitVersion to maintain consistent versioning:

1. GitVersion calculates the semantic version from git history
2. The version is embedded in artifact directory names
3. Version metadata is saved in `version.txt`

This ensures Rust artifacts follow the same versioning scheme as C# artifacts built with Nuke.

## Troubleshooting

### "GitVersion not found" on Linux/macOS

Install GitVersion.Tool:

```bash
dotnet tool install --global GitVersion.Tool
```

### "dotnet not found" on Linux/macOS

Install the .NET SDK for your platform:

- Ubuntu/Debian: https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu
- macOS: https://learn.microsoft.com/en-us/dotnet/core/install/macos

### Scripts not executable on Linux/macOS

On Windows (WSL) or when cloning the repo, the scripts may not be executable. This is normal and will be handled automatically when cargo-make runs them. If you need to run them manually:

```bash
chmod +x build/rust/scripts/*.sh
```

### Build fails with "manifest not found"

Ensure the PROJECT_PATH in `Makefile.toml` points to the correct location of your Rust project's `Cargo.toml`.

## Adding New Rust Projects

To add additional Rust projects to the build system:

1. Create a new `Makefile.toml` (e.g., `Makefile-project2.toml`)
2. Update the `PROJECT_NAME` and `PROJECT_PATH` variables
3. Add corresponding tasks to `Taskfile.yml`

Alternatively, modify the existing `Makefile.toml` to support multiple projects using cargo-make's workspace features.

## References

- [cargo-make Documentation](https://github.com/sagiegurari/cargo-make)
- [GitVersion Documentation](https://gitversion.net/)
- [Taskfile Documentation](https://taskfile.dev/)
