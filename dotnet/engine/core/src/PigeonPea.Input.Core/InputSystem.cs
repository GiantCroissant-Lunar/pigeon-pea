using PigeonPea.Input.Core.Actions;
using PigeonPea.Input.Core.Bindings;
using PigeonPea.Input.Core.Controls;
using PigeonPea.Input.Core.Events;

namespace PigeonPea.Input.Core;

/// <summary>
/// Main input system. Polls devices and triggers actions.
/// </summary>
public sealed class InputSystem
{
    private readonly List<IInputDevice> _devices = new();
    private readonly List<InputActionAsset> _assets = new();
    private readonly Queue<InputContext> _pendingInputs = new();
    private double _currentTime = 0;

    /// <summary>
    /// Registers an input device.
    /// </summary>
    public void RegisterDevice(IInputDevice device)
    {
        _devices.Add(device);
    }

    /// <summary>
    /// Removes an input device.
    /// </summary>
    public bool RemoveDevice(string deviceId)
    {
        var device = _devices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device != null)
        {
            _devices.Remove(device);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all registered devices.
    /// </summary>
    public IReadOnlyList<IInputDevice> GetDevices() => _devices.AsReadOnly();

    /// <summary>
    /// Registers an input action asset.
    /// </summary>
    public void RegisterAsset(InputActionAsset asset)
    {
        _assets.Add(asset);
    }

    /// <summary>
    /// Removes an input action asset.
    /// </summary>
    public bool RemoveAsset(string assetName)
    {
        var asset = _assets.FirstOrDefault(a => a.Name == assetName);
        if (asset != null)
        {
            _assets.Remove(asset);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all registered assets.
    /// </summary>
    public IReadOnlyList<InputActionAsset> GetAssets() => _assets.AsReadOnly();

    /// <summary>
    /// Gets an asset by name.
    /// </summary>
    public InputActionAsset? GetAsset(string name)
    {
        return _assets.FirstOrDefault(a => a.Name == name);
    }

    /// <summary>
    /// Updates all devices and processes input.
    /// Call once per frame/update.
    /// </summary>
    public void Update(double deltaTime)
    {
        _currentTime += deltaTime;

        // Update all devices
        foreach (var device in _devices)
        {
            device.Update();
        }

        // Process all enabled action maps
        foreach (var asset in _assets)
        {
            foreach (var map in asset.ActionMaps.Where(m => m.Enabled))
            {
                ProcessActionMap(map);
            }
        }
    }

    /// <summary>
    /// Resets the input system time.
    /// </summary>
    public void ResetTime()
    {
        _currentTime = 0;
    }

    /// <summary>
    /// Gets the current system time.
    /// </summary>
    public double GetCurrentTime() => _currentTime;

    /// <summary>
    /// Waits for any input action to be triggered (blocking).
    /// Useful for turn-based games.
    /// </summary>
    /// <param name="timeoutSeconds">Maximum seconds to wait. 0 = wait forever.</param>
    /// <returns>Input context or null if timeout expires</returns>
    public InputContext? WaitForInput(float timeoutSeconds = 0f)
    {
        var startTime = _currentTime;

        while (true)
        {
            Update(0.016); // Simulate 60 FPS frame

            if (_pendingInputs.Count > 0)
                return _pendingInputs.Dequeue();

            if (timeoutSeconds > 0 && (_currentTime - startTime) >= timeoutSeconds)
                return null;

            Thread.Sleep(16); // Sleep for ~60 FPS
        }
    }

    /// <summary>
    /// Gets all pending input events without blocking.
    /// Clears the pending queue.
    /// </summary>
    public IReadOnlyList<InputContext> GetPendingInputs()
    {
        var result = _pendingInputs.ToList();
        _pendingInputs.Clear();
        return result;
    }

    /// <summary>
    /// Clears all pending inputs without processing them.
    /// </summary>
    public void ClearPendingInputs()
    {
        _pendingInputs.Clear();
    }

    private void ProcessActionMap(InputActionMap map)
    {
        foreach (var action in map.Actions)
        {
            ProcessAction(action);
        }
    }

    private void ProcessAction(InputAction action)
    {
        bool wasProcessed = false;

        foreach (var binding in action.Bindings)
        {
            if (binding.IsComposite)
            {
                if (ProcessCompositeBinding(action, binding))
                {
                    wasProcessed = true;
                    break; // First active binding wins
                }
            }
            else
            {
                if (ProcessSimpleBinding(action, binding))
                {
                    wasProcessed = true;
                    break; // First active binding wins
                }
            }
        }

        // If no binding was active but action was previously performed, it's been released
        if (!wasProcessed && action.Phase == InputActionPhase.Performed)
        {
            action.Trigger(new InputValue(false), InputActionPhase.Canceled, _currentTime);
        }
    }

    private bool ProcessSimpleBinding(InputAction action, InputBinding binding)
    {
        var device = _devices.FirstOrDefault(d => d.DeviceType == binding.Path.DeviceType);
        if (device == null) return false;

        bool isActive = device.IsControlActive(binding.Path);

        if (isActive && action.Phase == InputActionPhase.Waiting)
        {
            // Button pressed (Started)
            var value = device.ReadControlValue(binding.Path);
            _pendingInputs.Enqueue(new InputContext(action.Name, value, InputActionPhase.Performed, _currentTime));
            action.Trigger(value, InputActionPhase.Started, _currentTime);
            action.Trigger(value, InputActionPhase.Performed, _currentTime);
            return true; // Binding was processed
        }
        else if (!isActive && action.Phase == InputActionPhase.Performed)
        {
            // Button released (Canceled)
            action.Trigger(new InputValue(false), InputActionPhase.Canceled, _currentTime);
            return true; // Binding was processed
        }

        return false; // Binding not active
    }

    private bool ProcessCompositeBinding(InputAction action, InputBinding binding)
    {
        var composite = binding.Composite!;

        if (composite.Type == CompositeType.TwoDVector)
        {
            return ProcessTwoDVectorComposite(action, composite);
        }
        else if (composite.Type == CompositeType.OneDAxis)
        {
            return ProcessOneDAxisComposite(action, composite);
        }
        else if (composite.Type == CompositeType.ButtonWithOneModifier)
        {
            return ProcessButtonWithModifierComposite(action, composite);
        }

        return false;
    }

    private bool ProcessTwoDVectorComposite(InputAction action, BindingComposite composite)
    {
        // Read WASD inputs
        var up = ReadCompositePartValue(composite, "up");
        var down = ReadCompositePartValue(composite, "down");
        var left = ReadCompositePartValue(composite, "left");
        var right = ReadCompositePartValue(composite, "right");

        float x = (right ? 1f : 0f) - (left ? 1f : 0f);
        float y = (up ? 1f : 0f) - (down ? 1f : 0f);

        var vector = new Vector2(x, y);
        var value = new InputValue(vector);

        if (vector.X != 0 || vector.Y != 0)
        {
            if (action.Phase == InputActionPhase.Waiting || action.Phase == InputActionPhase.Canceled)
            {
                action.Trigger(value, InputActionPhase.Started, _currentTime);
            }
            action.Trigger(value, InputActionPhase.Performed, _currentTime);
            return true; // ADD THIS
        }
        else if (action.Phase == InputActionPhase.Performed)
        {
            action.Trigger(new InputValue(Vector2.Zero), InputActionPhase.Canceled, _currentTime);
            return true; // ADD THIS
        }

        return false; // ADD THIS
    }

    private bool ProcessOneDAxisComposite(InputAction action, BindingComposite composite)
    {
        var negative = ReadCompositePartValue(composite, "negative");
        var positive = ReadCompositePartValue(composite, "positive");

        float axisValue = (positive ? 1f : 0f) - (negative ? 1f : 0f);
        var value = new InputValue(axisValue);

        if (axisValue != 0)
        {
            if (action.Phase == InputActionPhase.Waiting || action.Phase == InputActionPhase.Canceled)
            {
                action.Trigger(value, InputActionPhase.Started, _currentTime);
            }
            action.Trigger(value, InputActionPhase.Performed, _currentTime);
            return true; // ADD THIS
        }
        else if (action.Phase == InputActionPhase.Performed)
        {
            action.Trigger(new InputValue(0f), InputActionPhase.Canceled, _currentTime);
            return true; // ADD THIS
        }

        return false; // ADD THIS
    }

    private bool ProcessButtonWithModifierComposite(InputAction action, BindingComposite composite)
    {
        var button = ReadCompositePartValue(composite, "button");
        var modifier = ReadCompositePartValue(composite, "modifier");

        // Button with modifier is only "pressed" when both are active
        bool isActive = button && modifier;
        var value = new InputValue(isActive);

        if (isActive && action.Phase == InputActionPhase.Waiting)
        {
            action.Trigger(value, InputActionPhase.Started, _currentTime);
            action.Trigger(value, InputActionPhase.Performed, _currentTime);
            return true; // ADD THIS
        }
        else if (!isActive && action.Phase == InputActionPhase.Performed)
        {
            action.Trigger(new InputValue(false), InputActionPhase.Canceled, _currentTime);
            return true; // ADD THIS
        }

        return false; // ADD THIS
    }

    private bool ReadCompositePartValue(BindingComposite composite, string partName)
    {
        if (!composite.Bindings.TryGetValue(partName, out var path))
            return false;

        var device = _devices.FirstOrDefault(d => d.DeviceType == path.DeviceType);
        if (device == null) return false;

        return device.IsControlActive(path);
    }
}
