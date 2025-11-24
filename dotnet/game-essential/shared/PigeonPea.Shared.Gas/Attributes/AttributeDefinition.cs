namespace PigeonPea.Shared.Gas.Attributes;

/// <summary>
/// Defines an attribute with its base value.
/// </summary>
public sealed class AttributeDefinition
{
    public AttributeId Id { get; }
    public float BaseValue { get; set; }

    public AttributeDefinition(AttributeId id, float baseValue)
    {
        Id = id;
        BaseValue = baseValue;
    }

    public override string ToString() => $"{Id} = {BaseValue}";
}
