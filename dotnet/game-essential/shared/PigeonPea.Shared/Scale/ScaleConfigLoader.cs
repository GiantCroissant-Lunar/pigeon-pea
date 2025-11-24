using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PigeonPea.Shared.Scale;

public sealed class ScaleConfigLoader
{
    public static ScaleConfigSet LoadFromFiles(string scalesPath, string transitionsPath)
    {
        var scales = LoadScales(scalesPath);
        var transitions = LoadTransitions(transitionsPath);
        return new ScaleConfigSet(scales, transitions);
    }

    public static ScaleConfigSet LoadFromDirectory(string configDirectory)
    {
        var scalesPath = Path.Combine(configDirectory, "scales.json");
        var transitionsPath = Path.Combine(configDirectory, "transitions.json");
        return LoadFromFiles(scalesPath, transitionsPath);
    }

    private static List<ScaleConfig> LoadScales(string path)
    {
        if (!File.Exists(path))
        {
            return GetDefaultScales();
        }

        try
        {
            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("scales", out var scalesElement) ||
                scalesElement.ValueKind != JsonValueKind.Array)
            {
                return GetDefaultScales();
            }

            var scales = new List<ScaleConfig>();
            foreach (var el in scalesElement.EnumerateArray())
            {
                var scale = ParseScale(el);
                if (scale != null)
                {
                    scales.Add(scale);
                }
            }

            return scales.Count > 0 ? scales : GetDefaultScales();
        }
        catch
        {
            return GetDefaultScales();
        }
    }

    private static ScaleConfig? ParseScale(JsonElement el)
    {
        var id = el.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

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

        List<string>? overlayLayers = null;
        if (el.TryGetProperty("overlayLayers", out var layersEl) && layersEl.ValueKind == JsonValueKind.Array)
        {
            overlayLayers = new List<string>();
            foreach (var layer in layersEl.EnumerateArray())
            {
                if (layer.ValueKind == JsonValueKind.String && layer.GetString() is { } layerStr)
                {
                    overlayLayers.Add(layerStr);
                }
            }
        }

        Dictionary<string, OverlayRule>? overlayRules = null;
        if (el.TryGetProperty("overlayRules", out var rulesEl) && rulesEl.ValueKind == JsonValueKind.Object)
        {
            overlayRules = new Dictionary<string, OverlayRule>();
            foreach (var ruleProp in rulesEl.EnumerateObject())
            {
                var layerId = ruleProp.Name;
                var ruleObj = ruleProp.Value;
                var minZ = ruleObj.GetProperty("minZoom").GetDouble();
                var maxZ = ruleObj.GetProperty("maxZoom").GetDouble();
                var filter = ruleObj.TryGetProperty("filter", out var f) && f.ValueKind == JsonValueKind.String
                    ? f.GetString()
                    : null;
                overlayRules[layerId] = new OverlayRule(minZ, maxZ, filter);
            }
        }

        return new ScaleConfig(id, environment, metersPerCell, minZoom, maxZoom, chunkSizeCells,
            description, overlayLayers, overlayRules);
    }

    private static List<ScaleTransition> LoadTransitions(string path)
    {
        if (!File.Exists(path))
        {
            return GetDefaultTransitions();
        }

        try
        {
            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("transitions", out var transitionsElement) ||
                transitionsElement.ValueKind != JsonValueKind.Array)
            {
                return GetDefaultTransitions();
            }

            var transitions = new List<ScaleTransition>();
            foreach (var el in transitionsElement.EnumerateArray())
            {
                var transition = ParseTransition(el);
                if (transition != null)
                {
                    transitions.Add(transition);
                }
            }

            return transitions.Count > 0 ? transitions : GetDefaultTransitions();
        }
        catch
        {
            return GetDefaultTransitions();
        }
    }

    private static ScaleTransition? ParseTransition(JsonElement el)
    {
        var id = el.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var from = el.GetProperty("from").GetString();
        var to = el.GetProperty("to").GetString();
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            return null;
        }

        var triggerStr = el.GetProperty("trigger").GetString();
        if (!Enum.TryParse<TransitionTrigger>(triggerStr, true, out var trigger))
        {
            return null;
        }

        double? threshold = null;
        if (el.TryGetProperty("threshold", out var thresholdEl) && thresholdEl.ValueKind == JsonValueKind.Number)
        {
            threshold = thresholdEl.GetDouble();
        }

        TransitionDirection? direction = null;
        if (el.TryGetProperty("direction", out var directionEl) && directionEl.ValueKind == JsonValueKind.String)
        {
            var directionStr = directionEl.GetString();
            if (Enum.TryParse<TransitionDirection>(directionStr, true, out var dir))
            {
                direction = dir;
            }
        }

        var description = el.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
            ? descEl.GetString() ?? ""
            : "";

        return new ScaleTransition(id, from, to, trigger, threshold, direction, description);
    }

    private static List<ScaleConfig> GetDefaultScales()
    {
        return new List<ScaleConfig>
        {
            new ScaleConfig(
                Id: "world",
                Environment: "world",
                MetersPerCell: 1000.0,
                MinZoom: 0.75,
                MaxZoom: 2.0,
                ChunkSizeCells: 32,
                Description: "Overland map (1 km per cell)",
                OverlayLayers: new[] { "world.capitals", "world.settlements", "world.dungeons" },
                OverlayRules: new Dictionary<string, OverlayRule>
                {
                    ["world.settlements"] = new OverlayRule(0.0, 0.6, "tier == 'city'"),
                    ["world.dungeons"] = new OverlayRule(0.0, 0.7)
                }),
            new ScaleConfig(
                Id: "town",
                Environment: "world",
                MetersPerCell: 20.0,
                MinZoom: 0.75,
                MaxZoom: 2.0,
                ChunkSizeCells: 64,
                Description: "Town/block view (20 m per cell)",
                OverlayLayers: new[] { "town.buildings", "town.roads", "town.npcs" },
                OverlayRules: null),
            new ScaleConfig(
                Id: "dungeon-coarse",
                Environment: "dungeon",
                MetersPerCell: 5.0,
                MinZoom: 1.0,
                MaxZoom: 2.0,
                ChunkSizeCells: 64,
                Description: "Dungeon overview (5 m per tile)",
                OverlayLayers: null,
                OverlayRules: null),
            new ScaleConfig(
                Id: "dungeon-fine",
                Environment: "dungeon",
                MetersPerCell: 2.0,
                MinZoom: 1.0,
                MaxZoom: 1.5,
                ChunkSizeCells: 64,
                Description: "Dungeon gameplay (2 m per tile)",
                OverlayLayers: null,
                OverlayRules: null),
            new ScaleConfig(
                Id: "vehicle-fast",
                Environment: "vehicle",
                MetersPerCell: 100.0,
                MinZoom: 0.8,
                MaxZoom: 1.5,
                ChunkSizeCells: 64,
                Description: "Fast travel with mount/vehicle (100 m per cell)",
                OverlayLayers: null,
                OverlayRules: null)
        };
    }

    private static List<ScaleTransition> GetDefaultTransitions()
    {
        return new List<ScaleTransition>
        {
            new ScaleTransition(
                Id: "world-to-town-zoom",
                FromScaleId: "world",
                ToScaleId: "town",
                Trigger: TransitionTrigger.ZoomThreshold,
                Threshold: 2.0,
                Direction: TransitionDirection.ZoomIn,
                Description: "Zoom in on world transitions to town view"),
            new ScaleTransition(
                Id: "town-to-world-zoom",
                FromScaleId: "town",
                ToScaleId: "world",
                Trigger: TransitionTrigger.ZoomThreshold,
                Threshold: 0.75,
                Direction: TransitionDirection.ZoomOut,
                Description: "Zoom out on town transitions to world view"),
            new ScaleTransition(
                Id: "world-to-dungeon",
                FromScaleId: "world",
                ToScaleId: "dungeon-coarse",
                Trigger: TransitionTrigger.EnterDungeon,
                Threshold: null,
                Direction: null,
                Description: "Enter dungeon from world map"),
            new ScaleTransition(
                Id: "dungeon-coarse-to-fine",
                FromScaleId: "dungeon-coarse",
                ToScaleId: "dungeon-fine",
                Trigger: TransitionTrigger.ZoomThreshold,
                Threshold: 2.0,
                Direction: TransitionDirection.ZoomIn,
                Description: "Zoom in on dungeon overview for detailed view"),
            new ScaleTransition(
                Id: "world-to-vehicle",
                FromScaleId: "world",
                ToScaleId: "vehicle-fast",
                Trigger: TransitionTrigger.MountVehicle,
                Threshold: null,
                Direction: null,
                Description: "Mount vehicle for fast travel"),
            new ScaleTransition(
                Id: "vehicle-to-world",
                FromScaleId: "vehicle-fast",
                ToScaleId: "world",
                Trigger: TransitionTrigger.DismountVehicle,
                Threshold: null,
                Direction: null,
                Description: "Dismount vehicle and return to normal travel")
        };
    }
}

public sealed record ScaleConfigSet(
    IReadOnlyList<ScaleConfig> Scales,
    IReadOnlyList<ScaleTransition> Transitions);
