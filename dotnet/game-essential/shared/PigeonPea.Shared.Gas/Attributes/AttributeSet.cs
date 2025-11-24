namespace PigeonPea.Shared.Gas.Attributes;

/// <summary>
/// Manages a collection of attributes and their modifiers.
/// Calculates current values using formula: (Base + ΣAdd) × ΠMultiply
/// Override modifiers replace the entire calculation.
/// </summary>
public sealed class AttributeSet
{
    private readonly Dictionary<AttributeId, float> _baseValues = new();
    private readonly Dictionary<AttributeId, List<AttributeModifier>> _modifiers = new();

    public IReadOnlyDictionary<AttributeId, float> BaseValues => _baseValues;

    /// <summary>
    /// Sets or updates the base value for an attribute.
    /// </summary>
    public void SetBaseValue(AttributeId attributeId, float value)
    {
        _baseValues[attributeId] = value;
    }

    /// <summary>
    /// Gets the base value for an attribute (without modifiers).
    /// </summary>
    public float GetBaseValue(AttributeId attributeId)
    {
        return _baseValues.TryGetValue(attributeId, out var value) ? value : 0f;
    }

    /// <summary>
    /// Adds a modifier to an attribute.
    /// </summary>
    public void AddModifier(AttributeModifier modifier)
    {
        if (!_modifiers.ContainsKey(modifier.AttributeId))
            _modifiers[modifier.AttributeId] = new List<AttributeModifier>();

        _modifiers[modifier.AttributeId].Add(modifier);
    }

    /// <summary>
    /// Removes a specific modifier from an attribute.
    /// </summary>
    public bool RemoveModifier(AttributeModifier modifier)
    {
        if (!_modifiers.TryGetValue(modifier.AttributeId, out var modList))
            return false;

        return modList.Remove(modifier);
    }

    /// <summary>
    /// Removes all modifiers with a specific source tag.
    /// </summary>
    public int RemoveModifiersBySource(string sourceTag)
    {
        int removed = 0;
        foreach (var modList in _modifiers.Values)
        {
            removed += modList.RemoveAll(m => m.SourceTag == sourceTag);
        }
        return removed;
    }

    /// <summary>
    /// Gets the current value for an attribute (base + modifiers).
    /// Formula: (Base + ΣAdd) × ΠMultiply
    /// Override modifiers replace the entire calculation.
    /// </summary>
    public float GetCurrentValue(AttributeId attributeId)
    {
        float baseValue = GetBaseValue(attributeId);

        if (!_modifiers.TryGetValue(attributeId, out var modList) || modList.Count == 0)
            return baseValue;

        // Check for Override modifiers (latest one wins)
        var overrideModifier = modList.LastOrDefault(m => m.Operation == ModifierOperation.Override);
        if (overrideModifier != null)
            return overrideModifier.Magnitude;

        // Calculate: (Base + ΣAdd) × ΠMultiply
        float additive = modList
            .Where(m => m.Operation == ModifierOperation.Add)
            .Sum(m => m.Magnitude);

        float multiplicative = modList
            .Where(m => m.Operation == ModifierOperation.Multiply)
            .Aggregate(1f, (acc, m) => acc * m.Magnitude);

        return (baseValue + additive) * multiplicative;
    }

    /// <summary>
    /// Gets all modifiers for an attribute.
    /// </summary>
    public IReadOnlyList<AttributeModifier> GetModifiers(AttributeId attributeId)
    {
        return _modifiers.TryGetValue(attributeId, out var modList)
            ? modList.AsReadOnly()
            : Array.Empty<AttributeModifier>();
    }

    /// <summary>
    /// Clears all modifiers from all attributes.
    /// </summary>
    public void ClearAllModifiers()
    {
        _modifiers.Clear();
    }
}
