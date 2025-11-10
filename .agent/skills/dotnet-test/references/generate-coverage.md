# Generate Code Coverage - Detailed Procedure

## Overview

This guide covers generating code coverage reports for the PigeonPea solution using coverlet.collector (integrated with dotnet test) and optional HTML report generation with ReportGenerator.

## What is Code Coverage?

Code coverage measures which lines of code are executed during test runs, helping identify untested code paths.

**Coverage Metrics:**
- **Line Coverage:** Percentage of lines executed
- **Branch Coverage:** Percentage of conditional branches taken
- **Method Coverage:** Percentage of methods called

## Prerequisites

- .NET SDK 9.0+
- coverlet.collector package (already in test projects)
- (Optional) ReportGenerator tool for HTML reports

## Standard Coverage Flow

### Step 1: Navigate to .NET Directory

```bash
cd ./dotnet
```

### Step 2: Run Tests with Coverage Collection

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Generates coverage data in Cobertura XML format.

### Step 3: Locate Coverage File

```bash
# Coverage saved in ./TestResults/{guid}/coverage.cobertura.xml
ls -la ./TestResults/*/coverage.cobertura.xml
```

### Step 4: (Optional) Generate HTML Report

```bash
# Install ReportGenerator (first time only)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:"./TestResults/*/coverage.cobertura.xml" \
  -targetdir:"./TestResults/CoverageReport" \
  -reporttypes:"Html"

# Open report
open ./TestResults/CoverageReport/index.html
```

## Coverage Collection Options

```bash
# Basic coverage
dotnet test --collect:"XPlat Code Coverage"

# Coverage with custom results directory
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Coverage in Release configuration
dotnet test --collect:"XPlat Code Coverage" --configuration Release

# Coverage for specific project
dotnet test console-app.Tests/PigeonPea.Console.Tests.csproj --collect:"XPlat Code Coverage"

# Coverage with verbose output
dotnet test --collect:"XPlat Code Coverage" --verbosity normal
```

## Coverage Configuration

### runsettings File (Optional)

Create `./dotnet/coverage.runsettings` for advanced configuration:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>cobertura,opencover,json</Format>
          <Exclude>[*.Tests]*</Exclude>
          <ExcludeByAttribute>Obsolete,GeneratedCode,CompilerGenerated</ExcludeByAttribute>
          <ExcludeByFile>**/Migrations/*.cs</ExcludeByFile>
          <IncludeTestAssembly>false</IncludeTestAssembly>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

Use runsettings:

```bash
dotnet test --collect:"XPlat Code Coverage" --settings ./dotnet/coverage.runsettings
```

### Exclude Test Assemblies

By default, test assemblies are excluded from coverage. To include:

```bash
dotnet test --collect:"XPlat Code Coverage" /p:IncludeTestAssembly=true
```

## Report Formats

### Cobertura XML (Default)

```bash
dotnet test --collect:"XPlat Code Coverage"
# Output: ./TestResults/{guid}/coverage.cobertura.xml
```

Used by CI/CD tools (Azure DevOps, GitHub Actions).

### OpenCover XML

```bash
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

### JSON Format

```bash
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=json
```

### Multiple Formats

```bash
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura,opencover,json
```

## HTML Report Generation

### Install ReportGenerator

```bash
# Global tool (recommended)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Local tool (project-specific)
dotnet tool install dotnet-reportgenerator-globaltool
```

### Generate HTML Report

```bash
# Basic HTML report
reportgenerator \
  -reports:"./TestResults/*/coverage.cobertura.xml" \
  -targetdir:"./TestResults/CoverageReport" \
  -reporttypes:"Html"

# HTML with badges
reportgenerator \
  -reports:"./TestResults/*/coverage.cobertura.xml" \
  -targetdir:"./TestResults/CoverageReport" \
  -reporttypes:"Html;Badges"

# Multiple formats (HTML + XML)
reportgenerator \
  -reports:"./TestResults/*/coverage.cobertura.xml" \
  -targetdir:"./TestResults/CoverageReport" \
  -reporttypes:"Html;XmlSummary;Badges"
```

### Open Report

```bash
# Linux/macOS
open ./TestResults/CoverageReport/index.html

# Windows
start ./TestResults/CoverageReport/index.html

# WSL
explorer.exe ./TestResults/CoverageReport/index.html
```

## Analyzing Coverage Results

### Reading Cobertura XML

```xml
<coverage line-rate="0.85" branch-rate="0.72">
  <packages>
    <package name="PigeonPea.Console" line-rate="0.88" branch-rate="0.75">
      <classes>
        <class name="PigeonPea.Console.Frame" line-rate="0.95" branch-rate="0.80">
          <!-- Line-by-line coverage data -->
        </class>
      </classes>
    </package>
  </packages>
</coverage>
```

- `line-rate`: Line coverage (0.85 = 85%)
- `branch-rate`: Branch coverage (0.72 = 72%)

### Coverage Thresholds

Set minimum coverage thresholds in CI/CD:

```bash
# Example: Fail if coverage < 70%
threshold=70
coverage=$(awk -F'"' '/<coverage line-rate=/ {print $2; exit}' ./TestResults/*/coverage.cobertura.xml)
coverage_percent=$(echo "$coverage * 100" | bc | cut -d. -f1)
if [ "$coverage_percent" -lt "$threshold" ]; then
  echo "Coverage $coverage_percent% is below threshold $threshold%"
  exit 1
fi
```

## Common Errors and Solutions

### Error: No coverage data generated

**Cause:** coverlet.collector not installed or not configured

**Solutions:**

1. Check package in test project:
   ```bash
   dotnet list console-app.Tests/PigeonPea.Console.Tests.csproj package | grep coverlet
   ```

2. Add coverlet.collector if missing:
   ```bash
   dotnet add console-app.Tests/PigeonPea.Console.Tests.csproj package coverlet.collector
   ```

3. Ensure test project has `<IsTestProject>true</IsTestProject>` in .csproj

### Error: Coverage file not found

**Cause:** Coverage collection disabled or failed

**Solutions:**

1. Verify coverage enabled:
   ```bash
   dotnet test --collect:"XPlat Code Coverage" --verbosity detailed
   ```

2. Check TestResults directory:
   ```bash
   find ./TestResults -name "coverage.cobertura.xml"
   ```

### Error: ReportGenerator command not found

**Cause:** ReportGenerator not installed or not in PATH

**Fix:**

```bash
# Install globally
dotnet tool install -g dotnet-reportgenerator-globaltool

# Verify installation
reportgenerator --help
```

### Error: Low coverage unexpectedly

**Cause:** Tests not covering code, or exclusions too broad

**Solutions:**

1. Review HTML report to identify uncovered lines
2. Add tests for uncovered code paths
3. Check exclusions in runsettings (ensure not excluding too much)

## Coverage Best Practices

1. **Aim for >70% coverage** on new code (not necessarily 100%)
2. **Focus on critical paths:** Cover core logic, edge cases, error handling
3. **Don't test trivial code:** Properties, simple getters/setters
4. **Exclude generated code:** Use `[ExcludeFromCodeCoverage]` attribute
5. **Review HTML report regularly:** Identify gaps in coverage
6. **Integrate with CI/CD:** Fail builds if coverage drops below threshold

## Exclusions

### Exclude Classes/Methods

```csharp
[ExcludeFromCodeCoverage]
public class GeneratedClass
{
    // Not covered
}

[ExcludeFromCodeCoverage]
public void DebugMethod()
{
    // Not covered
}
```

### Exclude via runsettings

```xml
<Exclude>[*.Tests]*,[*]*.Migrations.*</Exclude>
<ExcludeByAttribute>Obsolete,GeneratedCode,CompilerGenerated</ExcludeByAttribute>
<ExcludeByFile>**/Migrations/*.cs,**/Generated/*.cs</ExcludeByFile>
```

## Integration with CI/CD

### GitHub Actions Example

```yaml
- name: Run tests with coverage
  run: dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

- name: Upload coverage to Codecov
  uses: codecov/codecov-action@v3
  with:
    files: ./TestResults/*/coverage.cobertura.xml
    fail_ci_if_error: true
```

### Azure Pipelines Example

```yaml
- task: DotNetCoreCLI@2
  displayName: Run tests with coverage
  inputs:
    command: test
    arguments: '--collect:"XPlat Code Coverage"'

- task: PublishCodeCoverageResults@1
  inputs:
    codeCoverageTool: Cobertura
    summaryFileLocation: '**/coverage.cobertura.xml'
```

## Automated Script

Create `./dotnet/scripts/coverage.sh`:

```bash
#!/bin/bash
set -e

echo "Running tests with coverage..."
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

echo "Generating HTML report..."
reportgenerator \
  -reports:"./TestResults/*/coverage.cobertura.xml" \
  -targetdir:"./TestResults/CoverageReport" \
  -reporttypes:"Html;Badges"

echo "Opening report..."
open ./TestResults/CoverageReport/index.html

echo "Done!"
```

Make executable: `chmod +x ./dotnet/scripts/coverage.sh`

Run: `./dotnet/scripts/coverage.sh`

## Verification Steps

1. Coverage file exists: `ls ./TestResults/*/coverage.cobertura.xml`
2. Coverage percentage in output or XML
3. HTML report generated (if using ReportGenerator)
4. Coverage meets project standards (e.g., >70%)

## Related Procedures

- **Run tests only:** See [`run-unit-tests.md`](run-unit-tests.md)
- **Benchmarks:** See [`run-benchmarks.md`](run-benchmarks.md)
- **Build before tests:** Use `dotnet-build` skill

## Quick Reference

```bash
# Run tests with coverage
cd ./dotnet
dotnet test --collect:"XPlat Code Coverage"

# Run with custom results directory
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate HTML report (requires ReportGenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"./TestResults/*/coverage.cobertura.xml" \
  -targetdir:"./TestResults/CoverageReport" \
  -reporttypes:"Html"

# Open report
open ./TestResults/CoverageReport/index.html

# Use coverage script (if created)
./scripts/coverage.sh
```
