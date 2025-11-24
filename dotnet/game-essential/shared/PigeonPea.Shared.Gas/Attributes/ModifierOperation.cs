namespace PigeonPea.Shared.Gas.Attributes;

/// <summary>
/// Defines how a modifier affects an attribute value.
/// Formula: Current = (Base + ΣAdd) × ΠMultiply
/// Override replaces the entire calculation.
/// </summary>
public enum ModifierOperation
{
    /// <summary>Additive modifier: Base + modifier</summary>
    Add,

    /// <summary>Multiplicative modifier: Base × modifier</summary>
    Multiply,

    /// <summary>Override modifier: Replaces base value entirely</summary>
    Override
}
