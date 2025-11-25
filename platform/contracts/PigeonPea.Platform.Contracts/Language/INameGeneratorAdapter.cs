using System.Collections.Generic;
using PigeonPea.Platform.Contracts.Language.Models;

namespace PigeonPea.Platform.Contracts.Language;

/// <summary>
/// Adapter for converting PigeonPea language definitions to name generator templates
/// </summary>
public interface INameGeneratorAdapter
{
    /// <summary>
    /// Generate a name using the specified language
    /// </summary>
    string GenerateName(
        LanguageDefinition language,
        NameType nameType,
        GenerationMode mode = GenerationMode.RuleBased);

    /// <summary>
    /// Generate a name using a built-in template name
    /// </summary>
    string GenerateFromTemplate(
        string templateName,
        NameType nameType,
        GenerationMode mode = GenerationMode.RuleBased);

    /// <summary>
    /// Check if a built-in template exists
    /// </summary>
    bool HasBuiltInTemplate(string templateName);

    /// <summary>
    /// Get all available built-in template names
    /// </summary>
    IReadOnlyList<string> GetBuiltInTemplates();
}

/// <summary>
/// Generation modes for name creation
/// </summary>
public enum GenerationMode
{
    RuleBased,      // Phoneme-based generation
    MarkovChain,    // Statistical learning from corpus
    Hybrid          // Combination of both
}
