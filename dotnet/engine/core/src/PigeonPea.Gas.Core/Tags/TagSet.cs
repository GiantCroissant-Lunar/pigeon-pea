using System.Collections.Generic;
using System.Linq;

namespace PigeonPea.Gas.Tags;

/// <summary>
/// Collection of gameplay tags with hierarchical matching support.
/// </summary>
public sealed class TagSet
{
    private readonly HashSet<GameplayTag> _tags = new();

    public IReadOnlySet<GameplayTag> Tags => _tags;
    public int Count => _tags.Count;

    /// <summary>
    /// Adds a tag to the set.
    /// </summary>
    public bool AddTag(GameplayTag tag)
    {
        return _tags.Add(tag);
    }

    /// <summary>
    /// Removes a tag from the set.
    /// </summary>
    public bool RemoveTag(GameplayTag tag)
    {
        return _tags.Remove(tag);
    }

    /// <summary>
    /// Checks if the set contains a tag with the specified match type.
    /// </summary>
    public bool HasTag(GameplayTag tag, TagMatchType matchType = TagMatchType.ExactOrAncestor)
    {
        return matchType switch
        {
            TagMatchType.Exact => _tags.Contains(tag),
            // ExactOrAncestor: match if the exact tag is present, or any descendant of it is present
            TagMatchType.ExactOrAncestor => _tags.Contains(tag) || _tags.Any(t => t.IsDescendantOf(tag)),
            // ExactOrDescendant: match if the exact tag is present, or any ancestor of it is present
            TagMatchType.ExactOrDescendant => _tags.Contains(tag) || _tags.Any(t => tag.IsDescendantOf(t)),
            _ => false
        };
    }

    /// <summary>
    /// Checks if the set contains all of the specified tags.
    /// </summary>
    public bool HasAllTags(IEnumerable<GameplayTag> tags, TagMatchType matchType = TagMatchType.ExactOrAncestor)
    {
        return tags.All(tag => HasTag(tag, matchType));
    }

    /// <summary>
    /// Checks if the set contains any of the specified tags.
    /// </summary>
    public bool HasAnyTag(IEnumerable<GameplayTag> tags, TagMatchType matchType = TagMatchType.ExactOrAncestor)
    {
        return tags.Any(tag => HasTag(tag, matchType));
    }

    /// <summary>
    /// Checks if the set contains none of the specified tags.
    /// </summary>
    public bool HasNoTags(IEnumerable<GameplayTag> tags, TagMatchType matchType = TagMatchType.ExactOrAncestor)
    {
        return !HasAnyTag(tags, matchType);
    }

    /// <summary>
    /// Clears all tags from the set.
    /// </summary>
    public void Clear()
    {
        _tags.Clear();
    }

    public override string ToString() => string.Join(", ", _tags.Select(t => t.Value));
}
