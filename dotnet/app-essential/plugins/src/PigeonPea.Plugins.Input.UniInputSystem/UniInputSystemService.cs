using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusInput;
using NexusInput.Actions;
using NexusInput.Controls;
using NexusInput.Events;
using NexusInput.Json;
using PigeonPea.Contracts.Input.Services;

namespace PigeonPea.Plugins.Input.UniInputSystem;

public sealed class UniInputSystemService : IService, IDisposable
{
    private readonly ILogger? _logger;
    private readonly InputSystem _inputSystem;
    private readonly InputActionAsset _asset;
    private readonly Dictionary<string, bool> _buttonStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, float> _axisStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly object _sync = new();
    private double _lastUpdateSeconds;
    private bool _disposed;

    public UniInputSystemService(ILogger? logger)
    {
        _logger = logger;

        _inputSystem = new InputSystem();

        var keyboard = new ConsoleKeyboardDevice();
        _inputSystem.RegisterDevice(keyboard);

        var json = LoadDefaultPlayerControls();
        _asset = InputActionAssetJson.FromJson(json);
        _inputSystem.RegisterAsset(_asset);

        foreach (var map in _asset.ActionMaps)
        {
            RegisterCallbacks(map);
            map.Enable();
        }
    }

    public bool IsActionPressed(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId)) throw new ArgumentException("Action ID must not be null or empty.", nameof(actionId));

        EnsureUpdated();

        lock (_sync)
        {
            return _buttonStates.TryGetValue(actionId, out var value) && value;
        }
    }

    public float GetAxis(string axisId)
    {
        if (string.IsNullOrWhiteSpace(axisId)) throw new ArgumentException("Axis ID must not be null or empty.", nameof(axisId));

        EnsureUpdated();

        lock (_sync)
        {
            return _axisStates.TryGetValue(axisId, out var value) ? value : 0f;
        }
    }

    private void EnsureUpdated()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            var now = _stopwatch.Elapsed.TotalSeconds;
            var delta = now - _lastUpdateSeconds;
            if (delta <= 0)
            {
                delta = 0.016;
            }

            _inputSystem.Update(delta);
            _lastUpdateSeconds = now;
        }
    }

    private void RegisterCallbacks(InputActionMap map)
    {
        foreach (var action in map.Actions)
        {
            if (string.Equals(action.Name, "Move", StringComparison.OrdinalIgnoreCase))
            {
                action.OnPerformed(context =>
                {
                    var v = context.ReadValue<NexusInput.Controls.Vector2>();
                    lock (_sync)
                    {
                        _axisStates["MoveX"] = v.X;
                        _axisStates["MoveY"] = v.Y;
                    }
                });

                action.OnCanceled(_ =>
                {
                    lock (_sync)
                    {
                        _axisStates["MoveX"] = 0f;
                        _axisStates["MoveY"] = 0f;
                    }
                });
            }
            else if (string.Equals(action.Name, "Navigate", StringComparison.OrdinalIgnoreCase))
            {
                action.OnPerformed(context =>
                {
                    var v = context.ReadValue<NexusInput.Controls.Vector2>();
                    lock (_sync)
                    {
                        _axisStates["NavigateX"] = v.X;
                        _axisStates["NavigateY"] = v.Y;
                    }
                });

                action.OnCanceled(_ =>
                {
                    lock (_sync)
                    {
                        _axisStates["NavigateX"] = 0f;
                        _axisStates["NavigateY"] = 0f;
                    }
                });
            }
            else
            {
                action.OnPerformed(context =>
                {
                    if (action.Type == InputActionType.Button || string.Equals(action.ExpectedControlType, "Button", StringComparison.OrdinalIgnoreCase))
                    {
                        var pressed = context.ReadValueAsButton();
                        lock (_sync)
                        {
                            _buttonStates[action.Name] = pressed;
                        }
                    }
                });

                action.OnCanceled(_ =>
                {
                    lock (_sync)
                    {
                        _buttonStates[action.Name] = false;
                    }
                });
            }
        }
    }

    private static string LoadDefaultPlayerControls()
    {
        var assembly = typeof(UniInputSystemService).Assembly;
        const string resourceName = "PigeonPea.Plugins.Input.UniInputSystem.Assets.DefaultPlayerControls.inputactions";

        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        using var reader = new System.IO.StreamReader(stream);
        return reader.ReadToEnd();
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
        }
    }
}
