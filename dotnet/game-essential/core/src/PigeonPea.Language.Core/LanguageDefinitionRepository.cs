using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PigeonPea.Language.Core;

public class LanguageDefinitionRepository
{
    private readonly ILogger<LanguageDefinitionRepository> _logger;
    private readonly Dictionary<string, LanguageDefinition> _loadedLanguages = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public LanguageDefinitionRepository(ILogger<LanguageDefinitionRepository> logger)
    {
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public async Task<LanguageDefinition> LoadLanguageAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Language definition file not found: {path}", path);
        }

        try
        {
            _logger.LogInformation("Loading language definition from '{Path}'", path);

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);

            var language = JsonSerializer.Deserialize<LanguageDefinition>(json, _jsonOptions);

            if (language == null)
            {
                throw new InvalidOperationException($"Failed to deserialize language definition from '{path}'");
            }

            // Validate the language definition
            ValidateLanguageDefinition(language, path);

            // Cache the loaded language
            _loadedLanguages[language.Id] = language;

            _logger.LogInformation("Successfully loaded language '{LanguageId}' ({LanguageName}) from '{Path}'",
                language.Id, language.Name, path);

            return language;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON from '{Path}': {Message}", path, ex.Message);
            throw new InvalidOperationException($"Invalid JSON in language definition file '{path}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load language definition from '{Path}'", path);
            throw;
        }
    }

    public async Task SaveLanguageAsync(LanguageDefinition language, string path)
    {
        if (language == null)
        {
            throw new ArgumentNullException(nameof(language));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        try
        {
            _logger.LogInformation("Saving language '{LanguageId}' to '{Path}'", language.Id, path);

            var json = JsonSerializer.Serialize(language, _jsonOptions);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);

            _logger.LogInformation("Successfully saved language '{LanguageId}' to '{Path}'", language.Id, path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save language definition to '{Path}'", path);
            throw;
        }
    }

    public LanguageDefinition? GetLoadedLanguage(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            return null;
        }

        return _loadedLanguages.TryGetValue(languageId, out var language) ? language : null;
    }

    public IReadOnlyList<LanguageDefinition> GetAllLoadedLanguages()
    {
        return _loadedLanguages.Values.ToList();
    }

    public void UnloadLanguage(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            return;
        }

        if (_loadedLanguages.Remove(languageId))
        {
            _logger.LogInformation("Unloaded language '{LanguageId}'", languageId);
        }
    }

    public void UnloadAllLanguages()
    {
        var count = _loadedLanguages.Count;
        _loadedLanguages.Clear();
        _logger.LogInformation("Unloaded all {Count} languages", count);
    }

    private void ValidateLanguageDefinition(LanguageDefinition language, string path)
    {
        var errors = new List<string>();

        // Validate ID
        if (string.IsNullOrWhiteSpace(language.Id))
        {
            errors.Add("Language ID is required");
        }

        // Validate Name
        if (string.IsNullOrWhiteSpace(language.Name))
        {
            errors.Add("Language name is required");
        }

        // Validate Phonology
        if (language.Phonology == null)
        {
            errors.Add("Phonology rules are required");
        }
        else
        {
            if (language.Phonology.Inventory == null)
            {
                errors.Add("Phoneme inventory is required");
            }
            else
            {
                if (language.Phonology.Inventory.Vowels == null || language.Phonology.Inventory.Vowels.Count == 0)
                {
                    errors.Add("At least one vowel is required in phoneme inventory");
                }

                if (language.Phonology.Inventory.Consonants == null || language.Phonology.Inventory.Consonants.Count == 0)
                {
                    errors.Add("At least one consonant is required in phoneme inventory");
                }
            }

            if (language.Phonology.SyllableTemplates == null || language.Phonology.SyllableTemplates.Count == 0)
            {
                errors.Add("At least one syllable template is required");
            }
        }

        // Validate Grammar
        if (language.Grammar == null)
        {
            errors.Add("Grammar rules are required");
        }

        // Report validation errors
        if (errors.Count > 0)
        {
            var errorMessage = $"Validation errors in language definition '{path}':\n" +
                             string.Join("\n", errors.Select(e => $"  - {e}"));

            _logger.LogError("Validation failed for '{Path}': {Errors}", path, string.Join("; ", errors));
            throw new InvalidOperationException(errorMessage);
        }
    }
}
