using System.Collections.Generic;
using PigeonPea.Platform.Contracts.Language.Models;

namespace PigeonPea.Platform.Contracts.Language.SoundChange;

public interface ISoundChangeEngine
{
    string ApplySoundChange(string word, SoundChangeRule rule);
    IEnumerable<string> ApplySoundChanges(IEnumerable<string> words, IEnumerable<SoundChangeRule> rules);
    LanguageDefinition DeriveLanguage(LanguageDefinition parent, IEnumerable<SoundChangeRule> rules);
}

public record SoundChangeRule
{
    public string Name { get; init; } = string.Empty;
    public string SourcePhoneme { get; init; } = string.Empty;
    public string TargetPhoneme { get; init; } = string.Empty;
    public string? Context { get; init; } // e.g., "_a" means "before 'a'"
    public int Order { get; init; }
}
