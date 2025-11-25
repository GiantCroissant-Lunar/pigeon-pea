namespace PigeonPea.Platform.Input.Actions;

/// <summary>
/// Collection of related actions (e.g., "Gameplay", "UI").
/// </summary>
public sealed class InputActionMap
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; private set; } = true;

    public List<InputAction> Actions { get; } = new();

    /// <summary>
    /// Enables this action map.
    /// </summary>
    public void Enable()
    {
        Enabled = true;
        foreach (var action in Actions)
        {
            action.Enable();
        }
    }

    /// <summary>
    /// Disables this action map.
    /// </summary>
    public void Disable()
    {
        Enabled = false;
        foreach (var action in Actions)
        {
            action.Disable();
        }
    }

    /// <summary>
    /// Gets an action by name.
    /// </summary>
    public InputAction? GetAction(string name)
    {
        return Actions.FirstOrDefault(a => a.Name == name);
    }

    /// <summary>
    /// Adds an action to this map.
    /// </summary>
    public void AddAction(InputAction action)
    {
        Actions.Add(action);
        if (Enabled)
        {
            action.Enable();
        }
        else
        {
            action.Disable();
        }
    }

    /// <summary>
    /// Removes an action from this map.
    /// </summary>
    public bool RemoveAction(string name)
    {
        var action = GetAction(name);
        if (action != null)
        {
            Actions.Remove(action);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all enabled actions in this map.
    /// </summary>
    public IEnumerable<InputAction> GetEnabledActions()
    {
        return Enabled ? Actions : Enumerable.Empty<InputAction>();
    }

    public override string ToString() => $"{Name} ({Actions.Count} actions, {(Enabled ? "Enabled" : "Disabled")})";
}
