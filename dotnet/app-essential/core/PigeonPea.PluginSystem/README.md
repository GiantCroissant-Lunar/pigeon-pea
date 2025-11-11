# PigeonPea.PluginSystem

Core plugin infrastructure for Pigeon Pea.

## Components

- PluginLoader: discovers, loads, and manages plugin lifecycles
- ServiceRegistry: cross-ALC service registry with priority selection
- EventBus: simple type-safe pub/sub for inter-plugin messaging
- PluginHost: host-provided services for plugins
- PluginContext: initialization context passed to plugins
- PluginRegistry: tracks loaded plugin state
- ManifestParser: parses `plugin.json`
- DependencyResolver: topological sort based on dependencies
- PluginLoadContext: AssemblyLoadContext isolating plugin assemblies
- ServiceCollectionExtensions: `AddPluginSystem()` DI wiring
- PluginLoaderHostedService: background discovery and load on startup

## Configuration

In `appsettings.json`:

```json
{
  "PluginSystem": {
    "PluginPaths": ["./plugins", "~/.config/pigeon-pea/plugins"],
    "Profile": "dotnet.console",
    "HotReload": false
  }
}
```

## Usage

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddPluginSystem(builder.Configuration);
var app = builder.Build();
await app.RunAsync();
```

## Notes

- Contracts (`PigeonPea.Contracts`) are resolved from the default ALC to preserve type identity.
- Cross-ALC interface checks compare `FullName` instead of reference equality.
- Hot reload is supported by `PluginLoader.ReloadPluginAsync`, basic MVP implementation.
