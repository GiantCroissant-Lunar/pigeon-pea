# Build .NET Solution - Detailed Procedure

## Overview

This guide provides comprehensive instructions for building the entire PigeonPea.sln solution, including all projects and test projects. Building the solution compiles all C# code, generates assemblies, and places artifacts in the appropriate output directories.

## Prerequisites

Before building, ensure:

- **.NET SDK installed**: Check with `dotnet --version`
  - Required: .NET 6.0 or later
  - Verify: Run `dotnet --info` to see installed SDKs
- **Solution file exists**: `./dotnet/PigeonPea.sln`
- **All project files present**: Check all `.csproj` files are accessible
- **Dependencies restored**: Run `dotnet restore` if needed (see `restore-deps.md`)
- **Disk space**: At least 500MB free for build artifacts

## Standard Build Flow

### Step 1: Navigate to Solution Directory

```bash
cd /home/runner/work/pigeon-pea/pigeon-pea/dotnet
```

Always work from the `dotnet` directory where `PigeonPea.sln` is located.

### Step 2: Restore Dependencies (Recommended First)

```bash
dotnet restore PigeonPea.sln
```

**Why:** Ensures all NuGet packages are downloaded before building.

**Expected output:**

```
  Determining projects to restore...
  Restored /path/to/console-app/PigeonPea.Console.csproj (in XXX ms).
  Restored /path/to/shared-app/PigeonPea.Shared.csproj (in XXX ms).
  ...
```

### Step 3: Build Solution (Debug Configuration)

```bash
dotnet build PigeonPea.sln
```

**What happens:**

1. MSBuild analyzes project dependencies
2. Projects are built in dependency order
3. C# compiler (Roslyn) compiles source files
4. References are resolved
5. Assemblies are generated in `bin/Debug/` directories

**Expected output:**

```
Microsoft (R) Build Engine version 17.x.x
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  All projects are up-to-date for restore.
  PigeonPea.Shared -> /path/to/shared-app/bin/Debug/net6.0/PigeonPea.Shared.dll
  PigeonPea.Console -> /path/to/console-app/bin/Debug/net6.0/PigeonPea.Console.dll
  PigeonPea.Windows -> /path/to/windows-app/bin/Debug/net6.0/PigeonPea.Windows.dll
  PigeonPea.Console.Tests -> /path/to/console-app.Tests/bin/Debug/net6.0/PigeonPea.Console.Tests.dll
  ...

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:XX.XX
```

### Step 4: Verify Build Artifacts

```bash
# Check that binaries were generated
ls -la console-app/bin/Debug/net*/
ls -la shared-app/bin/Debug/net*/
ls -la windows-app/bin/Debug/net*/
```

**Expected artifacts:**

- `.dll` files for libraries
- `.exe` files for applications (console-app)
- `.pdb` files for debug symbols
- `.deps.json` for dependency manifests
- `.runtimeconfig.json` for runtime configuration

## Production Build (Release Configuration)

For optimized, production-ready binaries:

### Build Command

```bash
dotnet build PigeonPea.sln --configuration Release
```

Or using shorthand:

```bash
dotnet build PigeonPea.sln -c Release
```

### Differences from Debug Build

| Aspect           | Debug        | Release                      |
| ---------------- | ------------ | ---------------------------- |
| Optimizations    | Disabled     | Enabled                      |
| Debug symbols    | Full (.pdb)  | Minimal or embedded          |
| Build speed      | Faster       | Slower (due to optimization) |
| Binary size      | Larger       | Smaller                      |
| Performance      | Baseline     | Optimized                    |
| Output directory | `bin/Debug/` | `bin/Release/`               |

### When to Use Release Configuration

- Deployment to production
- Performance testing/benchmarking
- Creating distributable packages
- Measuring actual application performance
- Final integration testing

## Advanced Build Options

```bash
# Detailed output for debugging
dotnet build PigeonPea.sln --verbosity detailed

# Build without restore (faster if deps already restored)
dotnet build PigeonPea.sln --no-restore

# Force full rebuild (ignore incremental build)
dotnet build PigeonPea.sln --no-incremental

# Parallel build with limited concurrency
dotnet build PigeonPea.sln -m:2
```

## Common Errors and Solutions

### Error: CS0246 - Type or namespace not found

**Solution:** Restore dependencies and check project references

```bash
dotnet restore PigeonPea.sln
# Check .csproj for missing <PackageReference> or <ProjectReference>
```

### Error: NU1301 - Unable to load service index

**Solution:** Network or NuGet source issue

```bash
dotnet nuget list source
dotnet restore PigeonPea.sln --source https://api.nuget.org/v3/index.json
```

### Error: MSB3644 - Reference assemblies not found

**Solution:** Wrong .NET SDK version

```bash
dotnet --list-sdks  # Check installed SDKs
# Install required SDK or check global.json
```

### Error: File in use

**Solution:**

```bash
pkill -f PigeonPea && dotnet clean PigeonPea.sln && dotnet build PigeonPea.sln
```

## Build Performance Tips

```bash
# Build specific project only
dotnet build console-app/PigeonPea.Console.csproj

# Skip analyzers for faster builds
dotnet build PigeonPea.sln -p:RunAnalyzers=false

# Clean and rebuild if incremental build fails
dotnet clean PigeonPea.sln && dotnet build PigeonPea.sln
```

## Build Output Structure

```
console-app/
├── bin/Debug/net6.0/    # Final build outputs (.dll, .exe, .pdb)
└── obj/Debug/net6.0/    # Intermediate files and build cache
```

**Never commit:** `bin/` and `obj/` directories (in `.gitignore`)

## Integration with CI/CD

Typical CI/CD build workflow:

```bash
#!/bin/bash
# CI build script

# Navigate to solution directory
cd ./dotnet

# Restore dependencies explicitly
dotnet restore PigeonPea.sln --locked-mode

# Build in Release configuration
dotnet build PigeonPea.sln \
  --configuration Release \
  --no-restore \
  -p:TreatWarningsAsErrors=true \
  -p:ContinuousIntegrationBuild=true

# Verify build succeeded
if [ $? -eq 0 ]; then
  echo "Build succeeded"
else
  echo "Build failed"
  exit 1
fi
```

## Related Procedures

- **Restore dependencies**: See `restore-deps.md` for detailed NuGet restore instructions
- **Run tests**: After building, run `dotnet test` (see dotnet-test skill)
- **Clean build**: Use `dotnet clean` before building to remove all artifacts

## Success Criteria

A successful build completion means:

- Exit code: `0`
- Message: "Build succeeded"
- All project DLLs/EXEs generated in `bin/` directories
- No compilation errors (error count: 0)
- Warnings are acceptable but should be reviewed

## Next Steps

After building successfully:

1. **Run tests**: `dotnet test PigeonPea.sln`
2. **Run application**: `dotnet run --project console-app/PigeonPea.Console.csproj`
3. **Package for deployment**: `dotnet publish` (see deployment documentation)
