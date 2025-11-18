using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PigeonPea.Shared.Scale;

/// <summary>
/// Configuration for a single logical scale level (e.g. world, town, dungeon-coarse).
/// This is intended to be loaded from JSON so that scales can be tweaked without recompiling.
/// </summary>
public sealed record ScaleConfig(
    string Id,
    string Environment,
    double MetersPerCell,
    double MinZoom,
    double MaxZoom,
    int ChunkSizeCells,
    string? Description);

/// <summary>
/// Registry for scale configurations. For now this is a simple static loader
/// that reads a JSON file from the application base directory, with sensible
/// in-code defaults if no file is present.
/// </summary>
public sealed class ScaleRegistry
{
    private readonly Dictionary<string, ScaleConfig> _scales;

    private ScaleRegistry(IEnumerable<ScaleConfig> scales)
    {
        _scales = scales.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
    }

    public ScaleConfig Get(string id)
    {
        if (_scales.TryGetValue(id, out var cfg))
        {
            return cfg;
        }

        throw new KeyNotFoundException($"ScaleConfig with id '{id}' not found.");
    }

    public bool TryGet(string id, out ScaleConfig? config)
    {
        return _scales.TryGetValue(id, out config);
    }

    /// <summary>
    /// Returns the globally shared registry instance. This is loaded once on first access.
    /// If the scales.json file is missing or invalid, in-code defaults are used.
    /// </summary>
    public static ScaleRegistry Default { get; } = LoadDefault();

    private static ScaleRegistry LoadDefault()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "scales.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("scales", out var scalesElement) &&
                    scalesElement.ValueKind == JsonValueKind.Array)
                {
                    var scales = new List<ScaleConfig>();
                    foreach (var el in scalesElement.EnumerateArray())
                    {
                        var id = el.GetProperty("id").GetString() ?? "";
                        var environment = el.GetProperty("environment").GetString() ?? "world";
                        var metersPerCell = el.GetProperty("metersPerCell").GetDouble();
                        var minZoom = el.GetProperty("minZoom").GetDouble();
                        var maxZoom = el.GetProperty("maxZoom").GetDouble();
                        var chunkSizeCells = el.GetProperty("chunkSizeCells").GetInt32();
                        string? description = null;
                        if (el.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String)
                        {
                            description = descEl.GetString();
                        }

                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            scales.Add(new ScaleConfig(id, environment, metersPerCell, minZoom, maxZoom, chunkSizeCells, description));
                        }
                    }

                    if (scales.Count > 0)
                    {
                        return new ScaleRegistry(scales);
                    }
                }
            }
        }
        catch
        {
            // Swallow and fall back to defaults.
        }

        // Fallback defaults if no config is present or parsing fails.
        var fallback = new[]
        {
            new ScaleConfig(
                Id: "world",
                Environment: "world",
                MetersPerCell: 1000.0,
                MinZoom: 0.75,
                MaxZoom: 2.0,
                ChunkSizeCells: 32,
                Description: "Overland map (1 km per cell)"),
            new ScaleConfig(
                Id: "dungeon-fine",
                Environment: "dungeon",
                MetersPerCell: 2.0,
                MinZoom: 1.0,
                MaxZoom: 1.5,
                ChunkSizeCells: 64,
                Description: "Dungeon gameplay (2 m per tile)"),
            new ScaleConfig(
                Id: "dungeon-coarse",
                Environment: "dungeon",
                MetersPerCell: 5.0,
                MinZoom: 1.0,
                MaxZoom: 2.0,
                ChunkSizeCells: 64,
                Description: "Dungeon overview (5 m per tile)")
        };

        return new ScaleRegistry(fallback);
    }
}
