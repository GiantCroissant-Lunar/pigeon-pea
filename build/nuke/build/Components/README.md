# Nuke Build Components

This directory contains reusable build components following the [Nuke.Build component pattern](https://nuke.build/docs/sharing/build-components/).

## Components

### IBuildConfig
Lightweight build configuration component that reads optional JSON configuration from `build/nuke/build.config.json`.

**Configuration options:**
- `sourceDir`: Source code directory (default: "dotnet")
- `websiteDir`: Website directory (default: "website")
- `frameworkDirs`: Framework project directories (default: ["framework"])
- `pluginDirs`: Plugin project directories (default: ["plugins"])
- `packPlugins`: Whether to pack plugins (default: true)
- `packFramework`: Whether to pack framework (default: true)
- `excludePluginNames`: Plugin names to exclude from packing
- `includePluginNames`: Plugin names to include in packing
- `syncLocalNugetFeed`: Whether to sync to local NuGet feed (default: false)
- `localNugetFeedRoot`: Root directory for local NuGet feed
- `localNugetFeedFlatSubdir`: Subdirectory for flat feed layout (default: "flat")
- `localNugetFeedHierarchicalSubdir`: Subdirectory for hierarchical feed layout (default: "hierarchical")
- `localNugetFeedBaseUrl`: Optional base URL for NuGet feed

### IClean
Provides the `Clean` target to clean build artifacts.
- Removes `bin` and `obj` directories from source tree
- Creates or cleans the artifacts directory

### IRestore
Provides the `Restore` target to restore NuGet packages.

### ICompile
Provides the `Compile` target to build the solution.
- Depends on `Restore`
- Uses the configured `Configuration` parameter

### ITest
Provides the `Test` target to run tests.
- Depends on `Compile`
- Runs tests without rebuilding

### IPublish
Provides the `Publish` target to publish executables.
- Depends on `Compile`
- Publishes all projects with `OutputType=Exe`
- Supports runtime and self-contained configuration

## Usage

To use these components in your Build.cs, implement the interfaces:

```csharp
class Build : NukeBuild, 
    IBuildConfig,
    IClean,
    IRestore,
    ICompile,
    ITest,
    IPublish
{
    public static int Main () => Execute<Build>(x => ((ICompile)x).Compile);
}
```

## Configuration Example

Create `build/nuke/build.config.json`:

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

See `build.config.json.example` for a complete example.
