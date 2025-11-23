namespace PigeonPea.Gas.Tags;

/// <summary>
/// Defines how tags are matched in queries.
/// </summary>
public enum TagMatchType
{
    /// <summary>Exact match only</summary>
    Exact,

    /// <summary>Match if tag or any ancestor is present (default for most GAS operations)</summary>
    ExactOrAncestor,

    /// <summary>Match if tag or any descendant is present</summary>
    ExactOrDescendant
}
