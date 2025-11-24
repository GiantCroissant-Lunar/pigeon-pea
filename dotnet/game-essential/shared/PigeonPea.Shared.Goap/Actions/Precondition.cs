using PigeonPea.Shared.Goap.WorldState;

namespace PigeonPea.Shared.Goap.Actions;

/// <summary>
/// Condition that must be true for an action to execute.
/// </summary>
public sealed class Precondition
{
    public WorldStateKey Key { get; }
    public CompareOp Operation { get; }
    public WorldStateValue Value { get; }

    public Precondition(WorldStateKey key, WorldStateValue value, CompareOp operation = CompareOp.Equal)
    {
        Key = key;
        Value = value;
        Operation = operation;
    }

    public Precondition(string key, bool value) : this(new WorldStateKey(key), new WorldStateValue(value)) { }
    public Precondition(string key, int value, CompareOp operation = CompareOp.Equal) : this(new WorldStateKey(key), new WorldStateValue(value), operation) { }
    public Precondition(string key, float value, CompareOp operation = CompareOp.Equal) : this(new WorldStateKey(key), new WorldStateValue(value), operation) { }
    public Precondition(string key, string value) : this(new WorldStateKey(key), new WorldStateValue(value)) { }

    /// <summary>
    /// Checks if this precondition is satisfied by the given world state.
    /// </summary>
    public bool IsSatisfied(WorldState.WorldState state)
    {
        var value = state.Get(Key);
        if (value == null)
            return false;

        return Operation.Evaluate(value.Value, Value);
    }

    public override string ToString() => $"{Key} {Operation} {Value}";
}
