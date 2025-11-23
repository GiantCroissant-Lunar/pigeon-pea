using System.Collections.Generic;
using System.Linq;

namespace PigeonPea.Gas.Tags;

/// <summary>
/// Complex query for tag matching with multiple conditions.
/// </summary>
public sealed class TagQuery
{
    public List<GameplayTag> RequireAllTags { get; set; } = new();
    public List<GameplayTag> RequireAnyTags { get; set; } = new();
    public List<GameplayTag> ForbidTags { get; set; } = new();
    public TagMatchType MatchType { get; set; } = TagMatchType.ExactOrAncestor;

    /// <summary>
    /// Evaluates the query against a tag set.
    /// Returns true if all conditions are satisfied.
    /// </summary>
    public bool Matches(TagSet tagSet)
    {
        // Must have all required tags
        if (RequireAllTags.Count > 0 && !tagSet.HasAllTags(RequireAllTags, MatchType))
            return false;

        // Must have at least one of the "any" tags (if specified)
        if (RequireAnyTags.Count > 0 && !tagSet.HasAnyTag(RequireAnyTags, MatchType))
            return false;

        // Must not have any forbidden tags
        if (ForbidTags.Count > 0 && tagSet.HasAnyTag(ForbidTags, MatchType))
            return false;

        return true;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (RequireAllTags.Count > 0)
            parts.Add($"RequireAll: {string.Join(", ", RequireAllTags)}");
        if (RequireAnyTags.Count > 0)
            parts.Add($"RequireAny: {string.Join(", ", RequireAnyTags)}");
        if (ForbidTags.Count > 0)
            parts.Add($"Forbid: {string.Join(", ", ForbidTags)}");
        return string.Join(" | ", parts);
    }
}
