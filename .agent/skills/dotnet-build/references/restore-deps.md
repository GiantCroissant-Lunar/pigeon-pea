# Restore .NET Dependencies - Detailed Procedure

## Overview

This guide covers NuGet package restoration for the PigeonPea solution. Restoring dependencies downloads all required NuGet packages and ensures they're available for building and running the application.

## Prerequisites

- **.NET SDK installed**: Check with `dotnet --version`
- **Internet connection**: Required to download packages from NuGet.org
- **Solution/project files**: `./dotnet/PigeonPea.sln` and all `.csproj` files
- **Disk space**: At least 1GB for NuGet global cache

## When to Restore Dependencies

Restore is needed when:

- **First time setup**: After cloning the repository
- **After pulling changes**: When `.csproj` files or package references changed
- **Build fails with missing references**: Error messages about missing assemblies
- **Package corruption**: When NuGet cache is corrupted
- **Switching branches**: If branches have different dependencies
- **CI/CD builds**: Always restore explicitly before building

## Standard Restore Flow

### Step 1: Navigate to Solution Directory

```bash
cd /home/runner/work/pigeon-pea/pigeon-pea/dotnet
```

### Step 2: Restore All Dependencies

```bash
dotnet restore PigeonPea.sln
```

**What happens:**

1. Reads all `.csproj` files in the solution
2. Analyzes `<PackageReference>` elements
3. Resolves dependency graph (including transitive dependencies)
4. Downloads packages from configured NuGet sources
5. Extracts packages to global NuGet cache
6. Creates `project.assets.json` in each `obj/` directory

**Expected output:**

```
  Determining projects to restore...
  Restored /path/to/console-app/PigeonPea.Console.csproj (in 1.2 sec).
  Restored /path/to/shared-app/PigeonPea.Shared.csproj (in 0.8 sec).
  Restored /path/to/windows-app/PigeonPea.Windows.csproj (in 0.9 sec).
  Restored /path/to/console-app.Tests/PigeonPea.Console.Tests.csproj (in 1.5 sec).
  ...
```

### Step 3: Verify Restore Success

```bash
echo $?  # Should be 0
```

Check for `project.assets.json` in each project:

```bash
ls -la console-app/obj/project.assets.json
ls -la shared-app/obj/project.assets.json
```

## Restore for Individual Projects

To restore a single project and its dependencies:

```bash
dotnet restore console-app/PigeonPea.Console.csproj
```

**Use case:** Faster restore when working on a specific project

## Advanced Restore Options

### Force Re-download

Ignore cached packages and re-download everything:

```bash
dotnet restore PigeonPea.sln --force
```

**Use case:** Suspected package corruption or cache issues

### Restore with Specific Source

Use a specific NuGet source instead of defaults:

```bash
dotnet restore PigeonPea.sln --source https://api.nuget.org/v3/index.json
```

**Use case:** Bypass local sources or use a private feed

### No-Cache Restore

Download packages without using the local cache:

```bash
dotnet restore PigeonPea.sln --no-cache
```

**Use case:** Troubleshooting cache-related issues

### Locked Mode (CI/CD)

Fail restore if packages don't match lock file:

```bash
dotnet restore PigeonPea.sln --locked-mode
```

**Use case:** Ensure reproducible builds in CI/CD pipelines

### Verbose Restore

Show detailed restore operations:

```bash
dotnet restore PigeonPea.sln --verbosity detailed
```

**Use case:** Debugging restore issues

## Common Errors and Solutions

### Error: NU1301 - Unable to load service index

**Symptom:**

```
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json
```

**Causes:**

- No internet connection
- NuGet.org is down
- Firewall/proxy blocking access
- DNS resolution issues

**Solutions:**

```bash
# Check internet connectivity
ping api.nuget.org

# List configured sources
dotnet nuget list source

# Add/verify NuGet.org source
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org

# Try restore with explicit source
dotnet restore PigeonPea.sln --source https://api.nuget.org/v3/index.json

# Clear and retry
dotnet nuget locals all --clear
dotnet restore PigeonPea.sln
```

### Error: NU1101/NU1102 - Package not found

**Cause:** Package name misspelled or version doesn't exist

**Solution:**

```bash
# Search for package
dotnet package search SomePackage

# Verify in .csproj and update version
grep -r "SomePackage" *.csproj
```

### Error: NU1107 - Version conflict

**Cause:** Multiple projects reference incompatible versions

**Solution:** Standardize versions in all `.csproj` files:

```xml
<PackageReference Include="PackageX" Version="2.0.0" />
```

## NuGet Cache Management

### View Cache Locations

```bash
dotnet nuget locals all --list
```

**Output shows:**

- `global-packages`: Main package cache (~/.nuget/packages)
- `http-cache`: HTTP request cache
- `temp`: Temporary files
- `plugins-cache`: Plugin cache

### Clear All Caches

```bash
dotnet nuget locals all --clear
```

**Use case:** Resolve corrupted cache or free disk space

### Clear Specific Cache

```bash
# Clear only global packages
dotnet nuget locals global-packages --clear

# Clear only HTTP cache
dotnet nuget locals http-cache --clear
```

## Package Sources Configuration

```bash
# List sources
dotnet nuget list source

# Add source
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org

# Enable/disable source
dotnet nuget enable source SourceName
dotnet nuget disable source SourceName
```

## Understanding project.assets.json

After restore, each project gets `obj/project.assets.json`:

- Lock file for resolved dependencies
- Used by MSBuild for compilation
- Never commit (in `.gitignore`)
- Delete and re-restore to fix issues

## Restore in CI/CD

```bash
# CI/CD best practice: use locked mode
dotnet restore PigeonPea.sln --locked-mode --no-cache
dotnet build PigeonPea.sln --no-restore
```

## Package Vulnerability Scanning

```bash
# List all packages
dotnet list package

# Check for outdated or vulnerable packages
dotnet list package --outdated
dotnet list package --vulnerable
```

## Performance Tips

```bash
# Skip restore when dependencies haven't changed
dotnet build PigeonPea.sln --no-restore

# Use local package source for offline work
dotnet nuget add source /path/to/offline-cache -n offline
```

## Success Criteria

A successful restore means:

- Exit code: `0`
- All projects show "Restored" message
- `project.assets.json` files created in each `obj/` directory
- No error messages in output
- Subsequent build succeeds

## Related Procedures

- **Build solution**: See `build-solution.md` for building after restore
- **Package management**: See `dotnet list package` and `dotnet add package` commands

## Next Steps

After successful restore:

1. **Build the solution**: `dotnet build PigeonPea.sln --no-restore`
2. **Run tests**: `dotnet test PigeonPea.sln`
3. **Run application**: `dotnet run --project console-app/PigeonPea.Console.csproj`
