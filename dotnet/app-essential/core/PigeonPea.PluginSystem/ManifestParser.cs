using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Parses plugin.json into <see cref="PluginManifest"/>.
/// </summary>
public static class ManifestParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static PluginManifest Parse(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("plugin.json not found", jsonPath);

        var json = File.ReadAllText(jsonPath);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(json, Options)
                       ?? throw new InvalidOperationException($"Failed to parse manifest at {jsonPath}");

        if (string.IsNullOrWhiteSpace(manifest.Id))
            throw new InvalidOperationException("Plugin manifest must specify 'id'.");
        if (string.IsNullOrWhiteSpace(manifest.Name))
            manifest.Name = manifest.Id;
        if (string.IsNullOrWhiteSpace(manifest.Version))
            manifest.Version = "1.0.0";

        return manifest;
    }
}
