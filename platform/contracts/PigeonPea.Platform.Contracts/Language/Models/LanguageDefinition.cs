using System;
using System.Collections.Generic;
using PigeonPea.Platform.Contracts.Language.Grammar;
using PigeonPea.Platform.Contracts.Language.Phonology;
using PigeonPea.Platform.Contracts.Language.SoundChange;

namespace PigeonPea.Platform.Contracts.Language.Models;

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
