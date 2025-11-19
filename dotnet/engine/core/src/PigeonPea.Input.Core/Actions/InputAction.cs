using PigeonPea.Shared.Input.Bindings;
using PigeonPea.Shared.Input.Controls;
using PigeonPea.Shared.Input.Events;

namespace PigeonPea.Shared.Input.Actions;

/// <summary>
/// Represents a logical input action (e.g., "Jump", "Fire").
/// </summary>
public sealed class InputAction
{
    public string Name { get; set; } = string.Empty;
    public InputActionType Type { get; set; } = InputActionType.Button;
    public string ExpectedControlType { get; set; } = "Button"; // "Button", "Axis", "Vector2"
    public InputActionPhase Phase { get; private set; } = InputActionPhase.Waiting;

    public List<InputBinding> Bindings { get; } = new();

    private readonly List<Action<InputContext>> _startedCallbacks = new();
    private readonly List<Action<InputContext>> _performedCallbacks = new();
    private readonly List<Action<InputContext>> _canceledCallbacks = new();

    /// <summary>
    /// Registers a callback for when the action starts.
    /// </summary>
    public void OnStarted(Action<InputContext> callback)
    {
        _startedCallbacks.Add(callback);
    }

    /// <summary>
    /// Registers a callback for when the action is performed.
    /// </summary>
    public void OnPerformed(Action<InputContext> callback)
    {
        _performedCallbacks.Add(callback);
    }

    /// <summary>
    /// Registers a callback for when the action is canceled.
    /// </summary>
    public void OnCanceled(Action<InputContext> callback)
    {
        _canceledCallbacks.Add(callback);
    }

    /// <summary>
    /// Removes all callbacks for this action.
    /// </summary>
    public void ClearCallbacks()
    {
        _startedCallbacks.Clear();
        _performedCallbacks.Clear();
        _canceledCallbacks.Clear();
    }

    /// <summary>
    /// Triggers the action with the given value.
    /// </summary>
    internal void Trigger(InputValue value, InputActionPhase phase, double time)
    {
        Phase = phase;
        var context = new InputContext(Name, value, phase, time);

        var callbacks = phase switch
        {
            InputActionPhase.Started => _startedCallbacks,
            InputActionPhase.Performed => _performedCallbacks,
            InputActionPhase.Canceled => _canceledCallbacks,
            _ => null
        };

        if (callbacks != null)
        {
            foreach (var callback in callbacks)
            {
                callback(context);
            }
        }
    }

    /// <summary>
    /// Resets the action to waiting state.
    /// </summary>
    public void Reset()
    {
        Phase = InputActionPhase.Waiting;
    }

    /// <summary>
    /// Enables the action.
    /// </summary>
    public void Enable()
    {
        Phase = InputActionPhase.Waiting;
    }

    /// <summary>
    /// Disables the action.
    /// </summary>
    public void Disable()
    {
        Phase = InputActionPhase.Disabled;
    }

    public override string ToString() => $"{Name} ({Type})";
}
