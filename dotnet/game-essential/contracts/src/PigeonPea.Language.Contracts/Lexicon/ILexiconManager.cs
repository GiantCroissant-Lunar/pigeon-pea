namespace PigeonPea.Language.Contracts.Lexicon;

public interface ILexiconManager
{
    void AddEntry(string languageId, LexiconEntry entry);
    LexiconEntry? LookupByMeaning(string languageId, string meaning);
    string? LookupByWord(string languageId, string word);
    IEnumerable<LexiconEntry> GetAllEntries(string languageId);
    Task SaveLexiconAsync(string languageId, string path);
    Task LoadLexiconAsync(string languageId, string path);
}

public record LexiconEntry
{
    public string Word { get; init; } = string.Empty;
    public string Meaning { get; init; } = string.Empty;
    public string? Root { get; init; }
    public IReadOnlyList<string> Affixes { get; init; } = Array.Empty<string>();
    public PartOfSpeech PartOfSpeech { get; init; }
    public IReadOnlyList<string> AlternateMeanings { get; init; } = Array.Empty<string>();
}

public enum PartOfSpeech
{
    Noun,
    Verb,
    Adjective,
    Adverb,
    Preposition,
    Conjunction,
    Pronoun,
    Determiner
}
