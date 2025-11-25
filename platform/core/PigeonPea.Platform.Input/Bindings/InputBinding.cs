namespace PigeonPea.Platform.Input.Bindings;

/// <summary>
/// Maps a physical control to an action.
/// </summary>
public sealed class InputBinding
{
    public string Name { get; set; } = string.Empty;
    public InputControlPath Path { get; set; }
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Optional composite (for multi-input bindings like WASD).
    /// </summary>
    public BindingComposite? Composite { get; set; }

    public bool IsComposite => Composite != null;

    public override string ToString()
    {
        if (IsComposite)
        {
            return $"{Name} (Composite: {Composite!.Type})";
        }
        return $"{Path} → {Action}";
    }

    /// <summary>
    /// Creates a simple binding from control path to action.
    /// </summary>
    public static InputBinding Simple(string path, string action)
    {
        return new InputBinding
        {
            Name = $"{path} → {action}",
            Path = new InputControlPath(path),
            Action = action
        };
    }

    /// <summary>
    /// Creates a composite binding.
    /// </summary>
    public static InputBinding CreateComposite(string name, string action, CompositeType type, Dictionary<string, string> parts)
    {
        var composite = new BindingComposite
        {
            Name = name,
            Type = type
        };

        foreach (var (partName, partPath) in parts)
        {
            composite.SetBinding(partName, new InputControlPath(partPath));
        }

        return new InputBinding
        {
            Name = name,
            Action = action,
            Path = new InputControlPath(""), // Empty for composites
            Composite = composite
        };
    }
}
