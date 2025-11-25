namespace PigeonPea.Language.Contracts.Grammar;

public interface IGrammarEngine
{
    string[] ApplyWordOrder(string[] words, WordOrder order);
    string ApplyMorphology(string root, MorphologyRule rule);
    string FormCompound(string[] roots, CompoundRule rule);
    bool ValidateGrammar(GrammarRules rules);
}

public record GrammarRules
{
    public WordOrder WordOrder { get; init; }
    public IReadOnlyList<MorphologyRule> MorphologyRules { get; init; } = Array.Empty<MorphologyRule>();
    public IReadOnlyList<CompoundRule> CompoundRules { get; init; } = Array.Empty<CompoundRule>();
}

public enum WordOrder
{
    SVO, // Subject-Verb-Object (English)
    SOV, // Subject-Object-Verb (Japanese)
    VSO, // Verb-Subject-Object (Welsh)
    VOS, // Verb-Object-Subject (Malagasy)
    OVS, // Object-Verb-Subject (Hixkaryana)
    OSV  // Object-Subject-Verb (rare)
}

public record MorphologyRule
{
    public string Name { get; init; } = string.Empty;
    public MorphologyType Type { get; init; }
    public string Pattern { get; init; } = string.Empty; // e.g., "{root}iel" for feminine suffix
    public string? Condition { get; init; }
}

public enum MorphologyType
{
    Prefix,
    Suffix,
    Infix,
    Circumfix,
    Pluralization,
    CaseMarking,
    VerbConjugation
}

public record CompoundRule
{
    public string Pattern { get; init; } = string.Empty; // e.g., "{root1}{root2}"
    public string? Connector { get; init; }
    public CompoundType Type { get; init; }
}

public enum CompoundType
{
    Noun_Noun,
    Adjective_Noun,
    Verb_Noun,
    Noun_Verb
}
