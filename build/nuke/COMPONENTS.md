# Nuke Build Components Migration

This document describes the adoption of Nuke.Build components from lablab-bean to pigeon-pea.

## What Was Done

The build components pattern from [Nuke.Build documentation](https://nuke.build/docs/sharing/build-components/) has been successfully adopted from lablab-bean project.

### Created Components

The following reusable build components were created in `build/nuke/build/Components/`:

1. **IBuildConfig.cs** - Configuration management component
   - Loads optional JSON configuration from `build.config.json`
   - Provides defaults for source directories, plugins, and NuGet feed settings
   - Supports flexible project structure configuration

2. **IClean.cs** - Clean target component
   - Removes bin/obj directories
   - Cleans artifacts directory

3. **IRestore.cs** - Restore target component
   - Restores NuGet packages for the solution

4. **ICompile.cs** - Compile target component
   - Builds the solution with configured settings
   - Depends on Restore

5. **ITest.cs** - Test target component
   - Runs unit tests
   - Depends on Compile

6. **IPublish.cs** - Publish target component
   - Publishes executable projects
   - Supports runtime and self-contained configuration
   - Depends on Compile

### Updated Files

- **build/nuke/build/Build.cs** - Refactored to use component interfaces
  - Implements all build component interfaces
  - Simplified from explicit target definitions to interface composition

### Added Files

- **build/nuke/build.config.json.example** - Example configuration file
- **build/nuke/build/Components/README.md** - Component documentation

## Benefits

1. **Modularity**: Build logic is separated into reusable components
2. **Maintainability**: Each component handles a specific concern
3. **Extensibility**: Easy to add new components or override existing behavior
4. **Consistency**: Share build patterns across projects
5. **Configuration**: External JSON configuration for project-specific settings

## Usage

### Basic Usage

Run build targets using the build scripts:

```powershell
# Windows
.\build.ps1 --target Clean
.\build.ps1 --target Compile
.\build.ps1 --target Test
.\build.ps1 --target Publish

# Linux/macOS
./build.sh --target Compile
```

### Configuration

Create `build/nuke/build.config.json` to customize build settings:

```json
{
  "sourceDir": "dotnet",
  "websiteDir": "website",
  "frameworkDirs": ["framework"],
  "pluginDirs": ["plugins"],
  "packPlugins": true,
  "packFramework": true
}
```

### Available Targets

- **Clean** - Clean build artifacts
- **Restore** - Restore NuGet packages
- **Compile** - Build the solution (default)
- **Test** - Run tests
- **Publish** - Publish executables

## Differences from lablab-bean

The pigeon-pea implementation includes:

1. Updated for Nuke.Common 9.0.4 (vs 8.0.0 in lablab-bean)
2. Removed deprecated FileSystemTasks static imports
3. Simplified IPublish to auto-detect executable projects
4. Cast fix for Main entry point: `Execute<Build>(x => ((ICompile)x).Compile)`

## Next Steps

Consider adding these additional components as needed:

- IPack - NuGet package creation
- IFormat - Code formatting
- IAnalyze - Code analysis
- ICoverage - Test coverage reporting

## References

- [Nuke.Build Components Documentation](https://nuke.build/docs/sharing/build-components/)
- Source: `D:\lunar-snake\personal-work\yokan-projects\lablab-bean\build\nuke\Components`
- Target: `D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\build\nuke\build\Components`
