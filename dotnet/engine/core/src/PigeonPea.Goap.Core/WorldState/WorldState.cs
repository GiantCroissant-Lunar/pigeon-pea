using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PigeonPea.Goap.WorldState;

/// <summary>
/// Immutable snapshot of world state from an agent's perspective.
/// Used by GOAP planner to represent current state, desired state, and intermediate states.
/// </summary>
public sealed class WorldState
{
    private readonly ImmutableDictionary<WorldStateKey, WorldStateValue> _state;

    public IReadOnlyDictionary<WorldStateKey, WorldStateValue> State => _state;

    public WorldState()
    {
        _state = ImmutableDictionary<WorldStateKey, WorldStateValue>.Empty;
    }

    private WorldState(ImmutableDictionary<WorldStateKey, WorldStateValue> state)
    {
        _state = state;
    }

    /// <summary>
    /// Creates a new WorldState with the given key-value pair added or updated.
    /// </summary>
    public WorldState Set(WorldStateKey key, WorldStateValue value)
    {
        return new WorldState(_state.SetItem(key, value));
    }

    /// <summary>
    /// Creates a new WorldState with the given key-value pair added or updated.
    /// </summary>
    public WorldState Set(string key, bool value) => Set(new WorldStateKey(key), new WorldStateValue(value));
    public WorldState Set(string key, int value) => Set(new WorldStateKey(key), new WorldStateValue(value));
    public WorldState Set(string key, float value) => Set(new WorldStateKey(key), new WorldStateValue(value));
    public WorldState Set(string key, string value) => Set(new WorldStateKey(key), new WorldStateValue(value));

    /// <summary>
    /// Gets the value for a key, or default if not present.
    /// </summary>
    public WorldStateValue? Get(WorldStateKey key)
    {
        if (_state.TryGetValue(key, out var value))
        {
            return new WorldStateValue?(value);
        }

        return new WorldStateValue?();
    }

    /// <summary>
    /// Checks if a key exists in the state.
    /// </summary>
    public bool Has(WorldStateKey key) => _state.ContainsKey(key);

    /// <summary>
    /// Creates a new WorldState with the given key removed.
    /// </summary>
    public WorldState Remove(WorldStateKey key)
    {
        return new WorldState(_state.Remove(key));
    }

    /// <summary>
    /// Merges another WorldState into this one (other's values take precedence).
    /// </summary>
    public WorldState Merge(WorldState other)
    {
        var builder = _state.ToBuilder();
        foreach (var kvp in other._state)
        {
            builder[kvp.Key] = kvp.Value;
        }
        return new WorldState(builder.ToImmutable());
    }

    /// <summary>
    /// Checks if this state satisfies all key-value pairs in the target state.
    /// </summary>
    public bool Satisfies(WorldState target)
    {
        foreach (var kvp in target._state)
        {
            if (!_state.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Calculates the number of differing keys between this state and another.
    /// Used as heuristic in A* planning.
    /// </summary>
    public int DifferenceCount(WorldState other)
    {
        int count = 0;
        foreach (var kvp in other._state)
        {
            if (!_state.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                count++;
        }
        return count;
    }

    public override string ToString() =>
        string.Join(", ", _state.Select(kvp => $"{kvp.Key}={kvp.Value}"));
}
