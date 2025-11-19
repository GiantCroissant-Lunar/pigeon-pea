namespace PigeonPea.Shared.Input.Actions;

/// <summary>
/// Container for multiple action maps (loaded from JSON).
/// </summary>
public sealed class InputActionAsset
{
    public string Name { get; set; } = string.Empty;
    public List<InputActionMap> ActionMaps { get; } = new();

    /// <summary>
    /// Gets an action map by name.
    /// </summary>
    public InputActionMap? GetMap(string name)
    {
        return ActionMaps.FirstOrDefault(m => m.Name == name);
    }

    /// <summary>
    /// Enables all action maps.
    /// </summary>
    public void EnableAllMaps()
    {
        foreach (var map in ActionMaps)
        {
            map.Enable();
        }
    }

    /// <summary>
    /// Disables all action maps.
    /// </summary>
    public void DisableAllMaps()
    {
        foreach (var map in ActionMaps)
        {
            map.Disable();
        }
    }

    /// <summary>
    /// Gets all enabled maps.
    /// </summary>
    public IEnumerable<InputActionMap> GetEnabledMaps()
    {
        return ActionMaps.Where(m => m.Enabled);
    }

    /// <summary>
    /// Adds an action map to this asset.
    /// </summary>
    public void AddMap(InputActionMap map)
    {
        ActionMaps.Add(map);
    }

    /// <summary>
    /// Removes an action map from this asset.
    /// </summary>
    public bool RemoveMap(string name)
    {
        var map = GetMap(name);
        if (map != null)
        {
            ActionMaps.Remove(map);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets an action from any map by name.
    /// </summary>
    public InputAction? GetAction(string actionName)
    {
        return ActionMaps
            .SelectMany(m => m.Actions)
            .FirstOrDefault(a => a.Name == actionName);
    }

    public override string ToString() => $"{Name} ({ActionMaps.Count} maps)";
}
