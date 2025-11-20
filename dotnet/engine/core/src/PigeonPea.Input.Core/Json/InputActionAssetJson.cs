using System.Text.Json;
using System.Text.Json.Serialization;
using PigeonPea.Input.Core.Actions;
using PigeonPea.Input.Core.Bindings;

namespace PigeonPea.Input.Core.Json;

/// <summary>
/// JSON representation of InputActionAsset (matches Unity format).
/// </summary>
public sealed class InputActionAssetJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("maps")]
    public List<ActionMapJson> Maps { get; set; } = new();

    public sealed class ActionMapJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("actions")]
        public List<ActionJson> Actions { get; set; } = new();

        [JsonPropertyName("bindings")]
        public List<BindingJson> Bindings { get; set; } = new();
    }

    public sealed class ActionJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Button"; // "Button", "Value", "PassThrough"

        [JsonPropertyName("expectedControlType")]
        public string? ExpectedControlType { get; set; }
    }

    public sealed class BindingJson
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("composite")]
        public string? Composite { get; set; }

        [JsonPropertyName("compositeParts")]
        public List<CompositePartJson>? CompositeParts { get; set; }
    }

    public sealed class CompositePartJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }

    /// <summary>
    /// Converts JSON to InputActionAsset.
    /// </summary>
    public InputActionAsset ToInputActionAsset()
    {
        var asset = new InputActionAsset { Name = Name };

        foreach (var mapJson in Maps)
        {
            var map = new InputActionMap { Name = mapJson.Name };

            // Create actions
            foreach (var actionJson in mapJson.Actions)
            {
                var action = new InputAction
                {
                    Name = actionJson.Name,
                    Type = ParseActionType(actionJson.Type),
                    ExpectedControlType = actionJson.ExpectedControlType ?? actionJson.Type
                };
                map.Actions.Add(action);
            }

            // Create bindings
            foreach (var bindingJson in mapJson.Bindings)
            {
                var binding = new InputBinding
                {
                    Name = bindingJson.Name ?? string.Empty,
                    Path = new InputControlPath(bindingJson.Path),
                    Action = bindingJson.Action
                };

                // Handle composites
                if (!string.IsNullOrEmpty(bindingJson.Composite) && bindingJson.CompositeParts != null)
                {
                    var composite = new BindingComposite
                    {
                        Name = bindingJson.Name ?? bindingJson.Composite,
                        Type = ParseCompositeType(bindingJson.Composite)
                    };

                    foreach (var part in bindingJson.CompositeParts)
                    {
                        composite.SetBinding(part.Name, new InputControlPath(part.Path));
                    }

                    binding.Composite = composite;
                }

                // Add binding to action
                var targetAction = map.GetAction(bindingJson.Action);
                targetAction?.Bindings.Add(binding);
            }

            asset.ActionMaps.Add(map);
        }

        return asset;
    }

    /// <summary>
    /// Converts InputActionAsset to JSON.
    /// </summary>
    public static InputActionAssetJson FromInputActionAsset(InputActionAsset asset)
    {
        var json = new InputActionAssetJson { Name = asset.Name };

        foreach (var map in asset.ActionMaps)
        {
            var mapJson = new ActionMapJson { Name = map.Name };

            // Convert actions
            foreach (var action in map.Actions)
            {
                mapJson.Actions.Add(new ActionJson
                {
                    Name = action.Name,
                    Type = action.Type.ToString(),
                    ExpectedControlType = action.ExpectedControlType
                });
            }

            // Convert bindings
            foreach (var action in map.Actions)
            {
                foreach (var binding in action.Bindings)
                {
                    var bindingJson = new BindingJson
                    {
                        Name = binding.Name,
                        Path = binding.Path.Path,
                        Action = binding.Action
                    };

                    if (binding.Composite != null)
                    {
                        bindingJson.Composite = binding.Composite.Type.ToString();
                        bindingJson.CompositeParts = binding.Composite.Bindings
                            .Select(kvp => new CompositePartJson
                            {
                                Name = kvp.Key,
                                Path = kvp.Value.Path
                            })
                            .ToList();
                    }

                    mapJson.Bindings.Add(bindingJson);
                }
            }

            json.Maps.Add(mapJson);
        }

        return json;
    }

    /// <summary>
    /// Loads from JSON string.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when JSON string is empty</exception>
    /// <exception cref="InvalidDataException">Thrown when JSON is invalid or deserialized to null</exception>
    public static InputActionAsset FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

        try
        {
            var jsonObj = JsonSerializer.Deserialize<InputActionAssetJson>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (jsonObj == null)
                throw new InvalidDataException("JSON deserialized to null - ensure JSON contains valid InputActionAsset structure");

            return jsonObj.ToInputActionAsset();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Failed to parse input actions JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves to JSON string.
    /// </summary>
    public static string ToJson(InputActionAsset asset)
    {
        var jsonObj = FromInputActionAsset(asset);
        return JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static InputActionType ParseActionType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "button" => InputActionType.Button,
            "value" => InputActionType.Value,
            "passthrough" => InputActionType.PassThrough,
            _ => InputActionType.Button
        };
    }

    private static CompositeType ParseCompositeType(string composite)
    {
        return composite.ToLowerInvariant() switch
        {
            "2dvector" => CompositeType.TwoDVector,
            "1daxis" => CompositeType.OneDAxis,
            "buttonwithonemodifier" => CompositeType.ButtonWithOneModifier,
            _ => CompositeType.None
        };
    }
}

/// <summary>
/// Extension methods for string to handle case-insensitive operations.
/// </summary>
internal static class StringExtensions
{
    public static string ToLowerInvariant(this string value) => value?.ToLowerInvariant() ?? string.Empty;
}
