namespace PigeonPea.Input.Core.Bindings;

/// <summary>
/// Combines multiple inputs into a single value (e.g., WASD → Vector2).
/// </summary>
public sealed class BindingComposite
{
    public string Name { get; set; } = string.Empty;
    public CompositeType Type { get; set; } = CompositeType.None;

    /// <summary>
    /// Part bindings (e.g., "up" → "<Keyboard>/w", "down" → "<Keyboard>/s")
    /// </summary>
    public Dictionary<string, InputControlPath> Bindings { get; } = new();

    public override string ToString() => $"{Name} ({Type})";

    /// <summary>
    /// Gets a binding part by name.
    /// </summary>
    public InputControlPath? GetBinding(string partName)
    {
        return Bindings.TryGetValue(partName, out var path) ? path : null;
    }

    /// <summary>
    /// Sets a binding part.
    /// </summary>
    public void SetBinding(string partName, InputControlPath path)
    {
        Bindings[partName] = path;
    }

    /// <summary>
    /// Checks if this composite has all required parts for its type.
    /// </summary>
    public bool IsValid()
    {
        return Type switch
        {
            CompositeType.TwoDVector => HasAllParts("up", "down", "left", "right"),
            CompositeType.OneDAxis => HasAllParts("negative", "positive"),
            CompositeType.ButtonWithOneModifier => HasAllParts("button", "modifier"),
            CompositeType.None => true,
            _ => false
        };
    }

    private bool HasAllParts(params string[] requiredParts)
    {
        return requiredParts.All(part => Bindings.ContainsKey(part));
    }
}
