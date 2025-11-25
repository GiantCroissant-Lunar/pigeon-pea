namespace PigeonPea.Language.Contracts.Models;

using PigeonPea.Language.Contracts.Grammar;
using PigeonPea.Language.Contracts.Phonology;
using PigeonPea.Language.Contracts.SoundChange;

public record LanguageDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public PhonologyRules Phonology { get; init; } = new();
    public GrammarRules Grammar { get; init; } = new();
    public string? ParentLanguageId { get; init; }
    public IReadOnlyList<SoundChangeRule> SoundChanges { get; init; } = Array.Empty<SoundChangeRule>();
    public Dictionary<string, object> Metadata { get; init; } = new();
}
