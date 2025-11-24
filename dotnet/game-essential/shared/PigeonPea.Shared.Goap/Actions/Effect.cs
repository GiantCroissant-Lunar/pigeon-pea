using System;
using PigeonPea.Shared.Goap.WorldState;

namespace PigeonPea.Shared.Goap.Actions;

/// <summary>
/// Change to world state produced by an action.
/// </summary>
public sealed class Effect
{
    public WorldStateKey Key { get; }
    public EffectOp Operation { get; }
    public WorldStateValue Value { get; }

    public Effect(WorldStateKey key, WorldStateValue value, EffectOp operation = EffectOp.Set)
    {
        Key = key;
        Value = value;
        Operation = operation;
    }

    public Effect(string key, bool value) : this(new WorldStateKey(key), new WorldStateValue(value)) { }
    public Effect(string key, int value, EffectOp operation = EffectOp.Set) : this(new WorldStateKey(key), new WorldStateValue(value), operation) { }
    public Effect(string key, float value, EffectOp operation = EffectOp.Set) : this(new WorldStateKey(key), new WorldStateValue(value), operation) { }
    public Effect(string key, string value) : this(new WorldStateKey(key), new WorldStateValue(value)) { }

    /// <summary>
    /// Applies this effect to the given world state, returning a new state.
    /// </summary>
    public WorldState.WorldState Apply(WorldState.WorldState state)
    {
        var currentValue = state.Get(Key);

        return Operation switch
        {
            EffectOp.Set => state.Set(Key, Value),
            EffectOp.Add => ApplyNumericOp(state, currentValue, (a, b) => a + b),
            EffectOp.Subtract => ApplyNumericOp(state, currentValue, (a, b) => a - b),
            EffectOp.Multiply => ApplyNumericOp(state, currentValue, (a, b) => a * b),
            EffectOp.Remove => state.Remove(Key),
            _ => state
        };
    }

    private WorldState.WorldState ApplyNumericOp(WorldState.WorldState state, WorldStateValue? currentValue, Func<float, float, float> op)
    {
        if (currentValue == null || Value.Type != WorldStateValueType.Int && Value.Type != WorldStateValueType.Float)
            return state;

        float current = currentValue.Value.Type == WorldStateValueType.Int ? currentValue.Value.AsInt() : currentValue.Value.AsFloat();
        float operand = Value.Type == WorldStateValueType.Int ? Value.AsInt() : Value.AsFloat();
        float result = op(current, operand);

        return currentValue.Value.Type == WorldStateValueType.Int
            ? state.Set(Key, new WorldStateValue((int)result))
            : state.Set(Key, new WorldStateValue(result));
    }

    public override string ToString() => $"{Key} {Operation} {Value}";
}

public enum EffectOp
{
    Set,      // Replace value
    Add,      // Add to numeric value
    Subtract, // Subtract from numeric value
    Multiply, // Multiply numeric value
    Remove    // Remove key from state
}
