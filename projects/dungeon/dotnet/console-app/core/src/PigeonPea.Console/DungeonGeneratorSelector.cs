using PigeonPea.Dungeon.Core;

namespace PigeonPea.Console;

/// <summary>
/// Host-level selector that chooses which IDungeonGenerator implementation
/// to use based on a simple string identifier (e.g. from CLI or config).
///
/// For now this is a manual mapping between known generators. Later we can
/// extend this to a plugin-backed registry/descriptor flow similar to inventory.
/// </summary>
public static class DungeonGeneratorSelector
{
    /// <summary>
    /// Creates an IDungeonGenerator implementation based on the provided id.
    /// </summary>
    /// <param name="id">Identifier such as "basic" or "modern-edgar".</param>
    public static IDungeonGenerator Create(string? id)
    {
        var key = (id ?? "modern-edgar").Trim().ToLowerInvariant();

        return key switch
        {
            "basic" or "classic" => new BasicDungeonGenerator(),
            "modern" or "modern-edgar" or "edgar" => new ModernEdgarDungeonGenerator(),
            _ => new ModernEdgarDungeonGenerator(),
        };
    }
}
