using PigeonPea.Language.Contracts;

namespace PigeonPea.Language.Core;

/// <summary>
/// Configuration options for name generation
/// </summary>
public class NameGenerationConfiguration
{
    /// <summary>
    /// Default generation mode
    /// </summary>
    public GenerationMode DefaultMode { get; set; } = GenerationMode.RuleBased;

    /// <summary>
    /// Custom language ID to FMG template mappings
    /// </summary>
    public Dictionary<string, string> CustomTemplateMappings { get; set; } = new();

    /// <summary>
    /// Enable caching of converted templates
    /// </summary>
    public bool EnableTemplateCache { get; set; } = true;

    /// <summary>
    /// Path to custom Markov chain corpus files
    /// </summary>
    public string? MarkovCorpusPath { get; set; }

    /// <summary>
    /// Maximum number of cached templates
    /// </summary>
    public int MaxCacheSize { get; set; } = 100;

    /// <summary>
    /// Enable detailed logging for name generation
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Default seed for random generation (null for random)
    /// </summary>
    public int? DefaultSeed { get; set; } = null;

    /// <summary>
    /// Maximum number of attempts to generate a unique name
    /// </summary>
    public int MaxGenerationAttempts { get; set; } = 100;

    /// <summary>
    /// Enable phoneme frequency weighting
    /// </summary>
    public bool EnablePhonemeWeighting { get; set; } = true;

    /// <summary>
    /// Custom phoneme weights for specific languages
    /// </summary>
    public Dictionary<string, Dictionary<char, double>> CustomPhonemeWeights { get; set; } = new();
}
