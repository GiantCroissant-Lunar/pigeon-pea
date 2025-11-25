using System.Collections.Generic;
using System.Threading.Tasks;

namespace PigeonPea.Platform.Contracts.Language;

/// <summary>
/// Primary interface for language operations
/// </summary>
public interface ILanguageService
{
    // Language management
    Task<bool> LoadLanguageAsync(string languageId, string configPath);
    Task<bool> UnloadLanguageAsync(string languageId);
    IReadOnlyList<string> GetLoadedLanguages();

    // Translation
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);

    // Name generation
    string GenerateName(string languageId, NameGenerationOptions options);
    IEnumerable<string> GenerateNames(string languageId, int count, NameGenerationOptions options);

    // NEW: Enhanced name generation with adapter
    string GenerateNameAdvanced(
        string languageId,
        NameType nameType,
        GenerationMode mode = GenerationMode.RuleBased);

    string GenerateNameFromTemplate(
        string templateName,
        NameType nameType,
        GenerationMode mode = GenerationMode.RuleBased);

    IReadOnlyList<string> GetAvailableNameGenerationTemplates();

    // Text generation
    string GenerateSentence(string languageId, SentenceTemplate template);
    string GenerateParagraph(string languageId, int sentenceCount);
}

/// <summary>
/// Options for name generation
/// </summary>
public record NameGenerationOptions
{
    public int MinSyllables { get; init; } = 2;
    public int MaxSyllables { get; init; } = 4;
    public NameType Type { get; init; } = NameType.Personal;
    public int? Seed { get; init; }
}

public enum NameType
{
    Personal,
    Place,
    Item,
    Clan,
    Title
}

/// <summary>
/// Template for sentence generation
/// </summary>
public record SentenceTemplate
{
    public string Subject { get; init; } = string.Empty;
    public string Verb { get; init; } = string.Empty;
    public string Object { get; init; } = string.Empty;
    public Dictionary<string, string> Modifiers { get; init; } = new();
}
