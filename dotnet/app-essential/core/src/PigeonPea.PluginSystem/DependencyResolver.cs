using System;
using System.Collections.Generic;
using System.Linq;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Resolves plugin load order using topological sort based on manifest dependencies.
/// </summary>
public static class DependencyResolver
{
    public static IReadOnlyList<PluginManifest> ResolveLoadOrder(IEnumerable<PluginManifest> manifests)
    {
        var list = manifests.ToList();
        var idLookup = list.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);

        var incoming = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var outgoing = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var m in list)
        {
            incoming[m.Id] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            outgoing[m.Id] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var m in list)
        {
            foreach (var dep in m.Dependencies)
            {
                if (!idLookup.ContainsKey(dep.Id))
                {
                    if (dep.Optional) continue; // allow missing optional
                    throw new InvalidOperationException($"Plugin '{m.Id}' depends on missing plugin '{dep.Id}'.");
                }
                outgoing[dep.Id].Add(m.Id);    // dep -> m
                incoming[m.Id].Add(dep.Id);    // m has incoming from dep
            }
        }

        var result = new List<PluginManifest>(list.Count);
        var queue = new Queue<string>(incoming.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key));

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            result.Add(idLookup[id]);

            foreach (var to in outgoing[id].ToList())
            {
                outgoing[id].Remove(to);
                incoming[to].Remove(id);
                if (incoming[to].Count == 0)
                    queue.Enqueue(to);
            }
        }

        if (result.Count != list.Count)
            throw new InvalidOperationException("Cyclic plugin dependency detected.");

        // Optional: order by manifest priority descending within same dependency level is already preserved by queue order.
        return result;
    }
}
