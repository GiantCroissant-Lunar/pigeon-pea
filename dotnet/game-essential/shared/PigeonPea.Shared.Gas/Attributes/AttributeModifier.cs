namespace PigeonPea.Shared.Gas.Attributes;

/// <summary>
/// Represents a modification to an attribute value.
/// </summary>
public sealed class AttributeModifier
{
    public AttributeId AttributeId { get; }
    public ModifierOperation Operation { get; }
    public float Magnitude { get; }
    public string? SourceTag { get; }

    public AttributeModifier(
        AttributeId attributeId,
        ModifierOperation operation,
        float magnitude,
        string? sourceTag = null)
    {
        AttributeId = attributeId;
        Operation = operation;
        Magnitude = magnitude;
        SourceTag = sourceTag;
    }

    public override string ToString() =>
        $"{Operation} {Magnitude:F2} to {AttributeId}" +
        (SourceTag != null ? $" (from {SourceTag})" : "");
}
