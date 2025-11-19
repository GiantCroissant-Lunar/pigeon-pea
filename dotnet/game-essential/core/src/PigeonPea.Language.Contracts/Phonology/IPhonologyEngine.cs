namespace PigeonPea.Language.Contracts.Phonology;

public interface IPhonologyEngine
{
    bool ValidatePhonemeInventory(PhonemeInventory inventory);
    bool ValidateSyllableTemplate(SyllableTemplate template, PhonemeInventory inventory);
    string GenerateSyllable(SyllableTemplate template, Random random);
    bool IsValidWord(string word, PhonologyRules rules);
}

public record PhonemeInventory
{
    public IReadOnlyList<string> Vowels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Consonants { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Diphthongs { get; init; } = Array.Empty<string>();
}

public record SyllableTemplate
{
    public string Pattern { get; init; } = string.Empty; // e.g., "CVC", "CCVC", "CV"
    public IReadOnlyList<string> AllowedOnsets { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AllowedCodas { get; init; } = Array.Empty<string>();
    public int Weight { get; init; } = 1;
}

public record PhonologyRules
{
    public PhonemeInventory Inventory { get; init; } = new();
    public IReadOnlyList<SyllableTemplate> SyllableTemplates { get; init; } = Array.Empty<SyllableTemplate>();
    public ClusterRules Clusters { get; init; } = new();
}

public record ClusterRules
{
    public IReadOnlyList<string> InitialClusters { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MedialClusters { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FinalClusters { get; init; } = Array.Empty<string>();
}
