using System;

namespace PigeonPea.Goap.WorldState;

/// <summary>
/// Comparison operations for preconditions and goal matching.
/// </summary>
public enum CompareOp
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual
}

public static class CompareOpExtensions
{
    public static bool Evaluate(this CompareOp op, WorldStateValue left, WorldStateValue right)
    {
        if (left.Type != right.Type)
            return false;

        return left.Type switch
        {
            WorldStateValueType.Bool => EvaluateBool(op, left.AsBool(), right.AsBool()),
            WorldStateValueType.Int => EvaluateInt(op, left.AsInt(), right.AsInt()),
            WorldStateValueType.Float => EvaluateFloat(op, left.AsFloat(), right.AsFloat()),
            WorldStateValueType.String => EvaluateString(op, left.AsString(), right.AsString()),
            _ => false
        };
    }

    private static bool EvaluateBool(CompareOp op, bool left, bool right)
    {
        return op switch
        {
            CompareOp.Equal => left == right,
            CompareOp.NotEqual => left != right,
            _ => false // Other ops not valid for bool
        };
    }

    private static bool EvaluateInt(CompareOp op, int left, int right)
    {
        return op switch
        {
            CompareOp.Equal => left == right,
            CompareOp.NotEqual => left != right,
            CompareOp.GreaterThan => left > right,
            CompareOp.GreaterThanOrEqual => left >= right,
            CompareOp.LessThan => left < right,
            CompareOp.LessThanOrEqual => left <= right,
            _ => false
        };
    }

    private static bool EvaluateFloat(CompareOp op, float left, float right)
    {
        return op switch
        {
            CompareOp.Equal => Math.Abs(left - right) < 0.0001f,
            CompareOp.NotEqual => Math.Abs(left - right) >= 0.0001f,
            CompareOp.GreaterThan => left > right,
            CompareOp.GreaterThanOrEqual => left >= right,
            CompareOp.LessThan => left < right,
            CompareOp.LessThanOrEqual => left <= right,
            _ => false
        };
    }

    private static bool EvaluateString(CompareOp op, string left, string right)
    {
        return op switch
        {
            CompareOp.Equal => left == right,
            CompareOp.NotEqual => left != right,
            _ => false // Other ops not valid for string
        };
    }
}
