# PigeonPea.Contracts

Contracts (interfaces and data structures) for the plugin system.

- Target framework: netstandard2.1
- Purpose: Provide a stable API surface between host and plugins
- Contains:
  - `Plugin/` core plugin contracts (`IPlugin`, `IPluginContext`, `IRegistry`, `IPluginHost`, `IEventBus`)
  - `Plugin/PluginManifest.cs` and `Plugin/ServiceMetadata.cs`
  - `DependencyInjection/` (reserved for DI contracts)
  - `Services/` (reserved for service contracts)

No implementation code should live here.
