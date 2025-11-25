namespace PigeonPea.Platform.Plugin.Management;

using System.Text.Json;
using System.Text.Json.Serialization;
using PigeonPea.Platform.Contracts.Core;

/// <summary>
/// Parser and validator for plugin manifest files.
/// </summary>
public static class ManifestParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() }
    };

    public static PluginManifest ParseFile(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Manifest file not found: {manifestPath}");
        }

        var json = File.ReadAllText(manifestPath);
        return Parse(json);
    }

    public static PluginManifest Parse(string json)
    {
        var manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);
        if (manifest == null)
        {
            throw new InvalidOperationException("Failed to deserialize manifest");
        }

        Validate(manifest);
        return manifest;
    }

    public static void Validate(PluginManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            throw new InvalidOperationException("Manifest must have a non-empty Id");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            throw new InvalidOperationException("Manifest must have a non-empty Name");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidOperationException("Manifest must have a non-empty Version");
        }

        bool hasLegacyEntry = !string.IsNullOrWhiteSpace(manifest.EntryAssembly) &&
                              !string.IsNullOrWhiteSpace(manifest.EntryType);
        bool hasModernEntry = manifest.EntryPoint.Count > 0;

        if (!hasLegacyEntry && !hasModernEntry)
        {
            throw new InvalidOperationException(
                "Manifest must have either EntryPoint dictionary or both EntryAssembly and EntryType");
        }
    }
}
