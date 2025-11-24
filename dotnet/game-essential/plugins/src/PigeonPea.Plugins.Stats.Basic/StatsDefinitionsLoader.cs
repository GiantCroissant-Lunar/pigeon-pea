using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PigeonPea.Game.Contracts.Stats.Services;
using PigeonPea.Game.Contracts.Stats.Models;

namespace PigeonPea.Plugins.Stats.Basic;

internal static class StatsDefinitionsLoader
{
    public static IReadOnlyDictionary<string, StatDefinition> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new Dictionary<string, StatDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        if (!File.Exists(path))
        {
            return new Dictionary<string, StatDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<StatsDefinitionsDocument>(json) ?? new StatsDefinitionsDocument();

        var result = new Dictionary<string, StatDefinition>(StringComparer.OrdinalIgnoreCase);

        if (document.Stats != null)
        {
            foreach (var s in document.Stats)
            {
                var def = new StatDefinition
                {
                    Id = s.Id ?? string.Empty,
                    DisplayName = s.DisplayName ?? string.Empty,
                    Category = s.Category ?? string.Empty,
                    MinValue = s.MinValue,
                    MaxValue = s.MaxValue,
                    DefaultValue = s.DefaultValue,
                    Description = s.Description ?? string.Empty,
                    Formula = null
                };

                if (!string.IsNullOrWhiteSpace(def.Id))
                {
                    result[def.Id] = def;
                }
            }
        }

        if (document.DerivedStats != null)
        {
            foreach (var s in document.DerivedStats)
            {
                var def = new StatDefinition
                {
                    Id = s.Id ?? string.Empty,
                    DisplayName = s.DisplayName ?? string.Empty,
                    Category = s.Category ?? "derived",
                    MinValue = 0f,
                    MaxValue = float.MaxValue,
                    DefaultValue = 0f,
                    Description = s.Description ?? string.Empty,
                    Formula = s.Formula
                };

                if (!string.IsNullOrWhiteSpace(def.Id))
                {
                    result[def.Id] = def;
                }
            }
        }

        return result;
    }

    private sealed class StatsDefinitionsDocument
    {
        [JsonPropertyName("stats")]
        public List<StatDefinitionJson>? Stats { get; set; }

        [JsonPropertyName("derived_stats")]
        public List<DerivedStatDefinitionJson>? DerivedStats { get; set; }
    }

    private sealed class StatDefinitionJson
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("minValue")]
        public float MinValue { get; set; }

        [JsonPropertyName("maxValue")]
        public float MaxValue { get; set; }

        [JsonPropertyName("defaultValue")]
        public float DefaultValue { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private sealed class DerivedStatDefinitionJson
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("formula")]
        public string? Formula { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
