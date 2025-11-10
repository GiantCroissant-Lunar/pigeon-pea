# Dependency Vulnerability Check - Detailed Procedure

## Overview

This guide provides step-by-step instructions for checking NuGet package dependencies for known vulnerabilities and security issues in the PigeonPea .NET solution.

## Prerequisites

- **.NET SDK 9.0** or later (check: `dotnet --version`)
- Solution file: `./dotnet/PigeonPea.sln`
- Internet connection (to query vulnerability databases)
- Dependencies restored: `dotnet restore PigeonPea.sln`

## Dependency Vulnerability Tools

### 1. dotnet list package --vulnerable

Built-in .NET CLI command that checks NuGet packages against known vulnerability databases.

**Data Sources:**
- GitHub Advisory Database
- National Vulnerability Database (NVD)
- NuGet Advisory Database

**What it detects:**
- Known CVEs (Common Vulnerabilities and Exposures)
- Security advisories for NuGet packages
- Vulnerable transitive dependencies

### 2. dotnet list package --outdated

Checks for newer versions of installed packages (not security-specific but useful for maintenance).

## Standard Dependency Check Flow

### Step 1: Navigate to .NET Directory

```bash
cd ./dotnet
```

All dependency commands should be run from the `./dotnet` directory.

### Step 2: Check for Vulnerable Packages

```bash
dotnet list package --vulnerable
```

This scans all projects in the solution for vulnerable packages.

### Step 3: Include Transitive Dependencies

```bash
dotnet list package --vulnerable --include-transitive
```

This includes indirect dependencies that might have vulnerabilities.

### Step 4: Check Specific Project

```bash
dotnet list console-app/PigeonPea.Console.csproj package --vulnerable --include-transitive
```

### Step 5: Check for Outdated Packages

```bash
dotnet list package --outdated
```

This shows available updates (not just security fixes).

## Command Options

```bash
# Usage: dotnet list [PROJECT|SOLUTION] package [options]

# Vulnerability scanning
--vulnerable                            # Show only vulnerable packages
--include-transitive                    # Include transitive (indirect) dependencies

# Version checking
--outdated                             # Show packages with newer versions
--highest-patch                        # Show highest patch version
--highest-minor                        # Show highest minor version

# Formatting
--format <console|json>                # Output format
--output <file>                        # Write output to file

# Filtering
--include-prerelease                   # Include pre-release versions
--framework <tfm>                      # Filter by target framework
--source <source>                      # NuGet source to check
```

## Understanding Vulnerability Output

### Clean Report (No Vulnerabilities)

```bash
$ dotnet list package --vulnerable --include-transitive

Project 'PigeonPea.Console' has no vulnerable packages.
Project 'PigeonPea.Shared' has no vulnerable packages.
Project 'PigeonPea.Windows' has no vulnerable packages.
```

✅ No known vulnerabilities in dependencies.

### Vulnerable Package Detected

```bash
$ dotnet list package --vulnerable --include-transitive

The following sources were used:
   https://api.nuget.org/v3/index.json

Project `PigeonPea.Console` has the following vulnerable packages
   [net9.0]:
   Top-level Package         Requested   Resolved   Severity   Advisory URL
   > Newtonsoft.Json         12.0.1      12.0.1     High       https://github.com/advisories/GHSA-5crp-9r3c-p9vr

   Transitive Package        Resolved   Severity   Advisory URL
   > System.Text.Json        6.0.0      Critical   https://github.com/advisories/GHSA-8g4q-xg66-9fp4
```

❌ **Action Required:** Update vulnerable packages.

### Vulnerability Details

**Fields:**
- **Package**: Name of vulnerable package
- **Requested**: Version specified in `.csproj`
- **Resolved**: Version actually used
- **Severity**: Low, Moderate, High, Critical
- **Advisory URL**: Link to detailed vulnerability information

## Vulnerability Severity Levels

1. **Critical**: Immediate action required, exploitable with severe impact
2. **High**: Urgent action required, significant security risk
3. **Moderate**: Schedule fix soon, moderate security risk
4. **Low**: Address in next maintenance cycle, minor risk

## Fixing Vulnerable Dependencies

### Method 1: Update Package (Direct Dependency)

For packages directly referenced in `.csproj`:

```bash
# Update to latest version
dotnet add console-app/PigeonPea.Console.csproj package Newtonsoft.Json

# Update to specific version
dotnet add console-app/PigeonPea.Console.csproj package Newtonsoft.Json --version 13.0.3
```

Or edit `.csproj` directly:

```xml
<ItemGroup>
  <!-- Before: Vulnerable -->
  <PackageReference Include="Newtonsoft.Json" Version="12.0.1" />

  <!-- After: Fixed -->
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

### Method 2: Update Transitive Dependency

For indirect (transitive) dependencies:

**Option A:** Update parent package that depends on it
```bash
dotnet add package ParentPackage --version <newer-version>
```

**Option B:** Add explicit reference to fixed version
```xml
<ItemGroup>
  <!-- Force specific version of transitive dependency -->
  <PackageReference Include="System.Text.Json" Version="8.0.0" />
</ItemGroup>
```

**Option C:** Use PackageReference with VersionOverride (Central Package Management)
```xml
<ItemGroup>
  <PackageVersion Include="System.Text.Json" Version="8.0.0" />
</ItemGroup>
```

### Method 3: Verify Fix

After updating:

```bash
# Restore with updated packages
dotnet restore PigeonPea.sln

# Verify vulnerability resolved
dotnet list package --vulnerable --include-transitive

# Build to ensure compatibility
dotnet build PigeonPea.sln
```

## Outdated Package Management

### Check for Updates

```bash
# All outdated packages
dotnet list package --outdated

# Include pre-release
dotnet list package --outdated --include-prerelease

# Show highest patch version only
dotnet list package --outdated --highest-patch

# Show highest minor version
dotnet list package --outdated --highest-minor
```

### Sample Outdated Output

```bash
$ dotnet list package --outdated

Project `PigeonPea.Console` has the following updates to its packages
   [net9.0]:
   Top-level Package         Requested   Resolved   Latest
   > Terminal.Gui            2.0.0       2.0.0      2.1.3
   > System.CommandLine      2.0.0-rc.2  2.0.0-rc.2 2.0.0-rc.3
```

### Selective Updates

**Update only patch versions** (safest):
```bash
dotnet add package Terminal.Gui --version 2.0.3
```

**Update to minor version** (review breaking changes):
```bash
dotnet add package Terminal.Gui --version 2.1.3
```

**Update to major version** (expect breaking changes):
```bash
dotnet add package Terminal.Gui --version 3.0.0
```

## Advanced Scenarios

### Generate JSON Report

```bash
# Vulnerability report
dotnet list package --vulnerable --include-transitive --format json > vulnerabilities.json

# Outdated packages report
dotnet list package --outdated --format json > outdated.json
```

### Check Specific Framework

```bash
dotnet list package --vulnerable --framework net9.0
```

### Use Custom NuGet Source

```bash
dotnet list package --vulnerable --source https://api.nuget.org/v3/index.json
```

### Automate with Script

```bash
#!/bin/bash
# check-dependencies.sh

echo "Checking for vulnerable packages..."
VULN_OUTPUT=$(dotnet list package --vulnerable --include-transitive)

if echo "$VULN_OUTPUT" | grep -q "has the following vulnerable packages"; then
    echo "❌ Vulnerable packages found!"
    echo "$VULN_OUTPUT"
    exit 1
else
    echo "✅ No vulnerable packages found"
    exit 0
fi
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Dependency Vulnerability Check

on: [push, pull_request]

jobs:
  check-dependencies:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore ./dotnet/PigeonPea.sln

      - name: Check for vulnerable packages
        run: |
          cd ./dotnet
          dotnet list package --vulnerable --include-transitive

      - name: Fail on vulnerabilities
        run: |
          cd ./dotnet
          VULN=$(dotnet list package --vulnerable --include-transitive)
          if echo "$VULN" | grep -q "Severity"; then
            echo "Vulnerable packages detected!"
            exit 1
          fi
```

### Pre-commit Hook (Optional)

Add to `.pre-commit-config.yaml`:

```yaml
- repo: local
  hooks:
    - id: check-vulnerabilities
      name: Check NuGet vulnerabilities
      entry: bash -c 'cd dotnet && dotnet list package --vulnerable | grep -q "has no vulnerable packages"'
      language: system
      pass_filenames: false
      always_run: true
```

## Best Practices

1. **Check regularly**: Run vulnerability scans weekly or before releases
2. **Monitor advisories**: Subscribe to security advisories for packages you use
3. **Update promptly**: Patch critical and high vulnerabilities immediately
4. **Test updates**: Always test after updating packages (run tests, verify functionality)
5. **Use latest LTS**: Prefer latest Long-Term Support versions of packages
6. **Minimize dependencies**: Fewer dependencies = smaller attack surface
7. **Pin versions**: Use explicit versions in `.csproj` to control updates
8. **Document decisions**: If you can't update, document why in code comments or ADR
9. **Enable in CI/CD**: Fail builds on critical/high vulnerabilities
10. **Review transitive**: Don't ignore transitive dependencies

## Handling Unfixable Vulnerabilities

If a vulnerability cannot be patched:

1. **Assess risk**: Review CVE details, determine if it affects your usage
2. **Mitigate**: Implement compensating controls (input validation, network restrictions)
3. **Document**: Create ADR or security doc explaining the risk and mitigation
4. **Monitor**: Watch for patches, plan migration if necessary
5. **Consider alternatives**: Evaluate switching to different package

## Common Issues and Solutions

### Issue: No vulnerabilities shown but advisory exists

**Cause:** Vulnerability database not yet updated or scan cache stale

**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Re-restore and re-scan
dotnet restore --force
dotnet list package --vulnerable --include-transitive
```

### Issue: Transitive dependency vulnerable but can't update

**Cause:** Parent package doesn't support newer version

**Solution 1:** Add explicit reference to fixed version
```xml
<PackageReference Include="VulnerablePackage" Version="1.2.3" />
```

**Solution 2:** Update or replace parent package

**Solution 3:** Contact maintainer of parent package

### Issue: Package update breaks build

**Cause:** Breaking changes in new version

**Solution:**
```bash
# Revert to working version
dotnet add package PackageName --version <previous-version>

# Review changelog for breaking changes
# Update code to accommodate changes
# Test thoroughly before redeploying
```

## Vulnerability Response Workflow

1. **Detect**: Run `dotnet list package --vulnerable --include-transitive`
2. **Assess**: Check severity and advisory URL for details
3. **Prioritize**: Critical/High → immediate, Moderate/Low → next sprint
4. **Update**: Use `dotnet add package` or edit `.csproj`
5. **Test**: Run `dotnet build && dotnet test`
6. **Verify**: Re-run vulnerability scan
7. **Deploy**: Push changes through CI/CD pipeline
8. **Document**: Log actions in security log or ADR

## Related Procedures

- **Static analysis:** See [`static-analysis.md`](static-analysis.md)
- **Security scanning:** See [`security-scan.md`](security-scan.md)
- **Build procedures:** See `dotnet-build` skill

## Quick Reference

```bash
# Check for vulnerable packages
cd ./dotnet
dotnet list package --vulnerable --include-transitive

# Check for outdated packages
dotnet list package --outdated

# Update package to latest
dotnet add console-app/PigeonPea.Console.csproj package Newtonsoft.Json

# Update to specific version
dotnet add console-app/PigeonPea.Console.csproj package Newtonsoft.Json --version 13.0.3

# Generate JSON report
dotnet list package --vulnerable --include-transitive --format json > vuln-report.json

# Verify fix
dotnet restore PigeonPea.sln
dotnet list package --vulnerable --include-transitive
dotnet build PigeonPea.sln
```
