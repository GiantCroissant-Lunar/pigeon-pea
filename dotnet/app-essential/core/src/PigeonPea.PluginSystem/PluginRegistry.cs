using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Tracks loaded plugins and their runtime state.
/// </summary>
public class PluginRegistry
{
    public class PluginRecord
    {
        public string Id { get; }
        public string DirectoryPath { get; }
        public PluginManifest Manifest { get; }
        public PluginLoadContext LoadContext { get; }
        public IPlugin Instance { get; }

        public PluginRecord(string id, string directoryPath, PluginManifest manifest, PluginLoadContext loadContext, IPlugin instance)
        {
            Id = id;
            DirectoryPath = directoryPath;
            Manifest = manifest;
            LoadContext = loadContext;
            Instance = instance;
        }
    }

    private readonly ConcurrentDictionary<string, PluginRecord> _byId = new();

    public bool TryAdd(PluginRecord record) => _byId.TryAdd(record.Id, record);
    public bool TryGet(string id, [NotNullWhen(true)] out PluginRecord? record) => _byId.TryGetValue(id, out record);
    public bool Remove(string id) => _byId.TryRemove(id, out _);
    public IEnumerable<PluginRecord> All() => _byId.Values;
}
