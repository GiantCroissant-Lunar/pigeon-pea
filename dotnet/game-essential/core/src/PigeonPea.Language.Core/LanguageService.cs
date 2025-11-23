using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts;
using PigeonPea.Language.Contracts.Models;
using Yokan.SCG.DI.ConstructorInjection.Attributes;

namespace PigeonPea.Language.Core;

[ConstructorInjection]
public partial class LanguageService : ILanguageService
{
    [ResolveInConstructor]
    private readonly LanguageDefinitionRepository _repository;
    [ResolveInConstructor]
    private readonly PhonologyEngine _phonologyEngine;
    [ResolveInConstructor]
    private readonly LexiconManager _lexiconManager;
    [ResolveInConstructor]
    private readonly GrammarEngine _grammarEngine;
    [ResolveInConstructor]
    private readonly SoundChangeEngine _soundChangeEngine;
    [ResolveInConstructor]
    private readonly INameGeneratorAdapter _nameAdapter;
    [ResolveInConstructor]
    private readonly ILoggerFactory _loggerFactory;
    [ResolveInConstructor]
    private readonly ILogger<LanguageService> _logger;

    // Cache of name generators per language
    private readonly Dictionary<string, NameGenerator> _nameGenerators = new();

    // Cache of translation engines per language pair
    private readonly Dictionary<string, TranslationEngine> _translationEngines = new();

    public async Task<bool> LoadLanguageAsync(string languageId, string configPath)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException("Language ID cannot be null or empty", nameof(languageId));
        }

        if (string.IsNullOrWhiteSpace(configPath))
        {
            throw new ArgumentException("Config path cannot be null or empty", nameof(configPath));
        }

        try
        {
            _logger.LogInformation("Loading language '{LanguageId}' from '{ConfigPath}'", languageId, configPath);

            // Load language definition
            var language = await _repository.LoadLanguageAsync(configPath).ConfigureAwait(false);

            // Verify the language ID matches
            if (language.Id != languageId)
            {
                _logger.LogWarning(
                    "Language ID mismatch: requested '{RequestedId}' but file contains '{FileId}'",
                    languageId, language.Id);
            }

            // Load lexicon if path is specified in metadata
            if (language.Metadata.TryGetValue("lexicon_path", out var lexiconPathObj) &&
                lexiconPathObj is string lexiconPath)
            {
                try
                {
                    // Resolve relative path
                    var configDir = Path.GetDirectoryName(configPath);
                    var fullLexiconPath = string.IsNullOrEmpty(configDir)
                        ? lexiconPath
                        : Path.Combine(configDir, lexiconPath);

                    await _lexiconManager.LoadLexiconAsync(language.Id, fullLexiconPath).ConfigureAwait(false);
                    _logger.LogInformation("Loaded lexicon for '{LanguageId}' from '{LexiconPath}'",
                        language.Id, fullLexiconPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load lexicon for '{LanguageId}', continuing without lexicon",
                        language.Id);
                }
            }

            // Create name generator for this language
            var nameGenerator = new NameGenerator(
                language,
                _phonologyEngine,
                _grammarEngine,
                _loggerFactory.CreateLogger<NameGenerator>());

            _nameGenerators[language.Id] = nameGenerator;

            // Create translation engine
            var translationEngine = new TranslationEngine(
                _lexiconManager,
                _grammarEngine,
                nameGenerator,
                _loggerFactory.CreateLogger<TranslationEngine>());

            _translationEngines[language.Id] = translationEngine;

            _logger.LogInformation("Successfully loaded language '{LanguageId}'", language.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load language '{LanguageId}' from '{ConfigPath}'",
                languageId, configPath);
            return false;
        }
    }

    public Task<bool> UnloadLanguageAsync(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException("Language ID cannot be null or empty", nameof(languageId));
        }

        try
        {
            _logger.LogInformation("Unloading language '{LanguageId}'", languageId);

            // Remove from repository
            _repository.UnloadLanguage(languageId);

            // Remove cached generators
            _nameGenerators.Remove(languageId);
            _translationEngines.Remove(languageId);

            _logger.LogInformation("Successfully unloaded language '{LanguageId}'", languageId);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload language '{LanguageId}'", languageId);
            return Task.FromResult(false);
        }
    }

    public IReadOnlyList<string> GetLoadedLanguages()
    {
        return _repository.GetAllLoadedLanguages()
            .Select(l => l.Id)
            .ToList();
    }

    public Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(string.Empty);
        }

        if (string.IsNullOrWhiteSpace(sourceLanguage))
        {
            throw new ArgumentException("Source language cannot be null or empty", nameof(sourceLanguage));
        }

        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            throw new ArgumentException("Target language cannot be null or empty", nameof(targetLanguage));
        }

        try
        {
            // Get target language definition
            var targetLangDef = _repository.GetLoadedLanguage(targetLanguage);
            if (targetLangDef == null)
            {
                throw new InvalidOperationException($"Target language '{targetLanguage}' is not loaded");
            }

            // Get translation engine
            if (!_translationEngines.TryGetValue(targetLanguage, out var translationEngine))
            {
                throw new InvalidOperationException($"Translation engine not available for '{targetLanguage}'");
            }

            // For now, we assume source is English
            // In a full implementation, we would support fantasy-to-fantasy translation
            string result;
            if (sourceLanguage.Equals("english", StringComparison.OrdinalIgnoreCase) ||
                sourceLanguage.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                result = translationEngine.TranslateToFantasy(text, targetLanguage, targetLangDef);
            }
            else
            {
                // Fantasy to English
                var sourceLangDef = _repository.GetLoadedLanguage(sourceLanguage);
                if (sourceLangDef == null)
                {
                    throw new InvalidOperationException($"Source language '{sourceLanguage}' is not loaded");
                }

                result = translationEngine.TranslateToEnglish(text, sourceLanguage, sourceLangDef);
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed from '{Source}' to '{Target}'",
                sourceLanguage, targetLanguage);
            throw;
        }
    }

    public string GenerateName(string languageId, NameGenerationOptions options)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException("Language ID cannot be null or empty", nameof(languageId));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (!_nameGenerators.TryGetValue(languageId, out var generator))
        {
            throw new InvalidOperationException($"Language '{languageId}' is not loaded");
        }

        return generator.GenerateName(options);
    }

    public IEnumerable<string> GenerateNames(string languageId, int count, NameGenerationOptions options)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException("Language ID cannot be null or empty", nameof(languageId));
        }

        if (count <= 0)
        {
            throw new ArgumentException("Count must be greater than zero", nameof(count));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (!_nameGenerators.TryGetValue(languageId, out var generator))
        {
            throw new InvalidOperationException($"Language '{languageId}' is not loaded");
        }

        return generator.GenerateNames(count, options);
    }

    public string GenerateSentence(string languageId, SentenceTemplate template)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException("Language ID cannot be null or empty", nameof(languageId));
        }

        if (template == null)
        {
            throw new ArgumentNullException(nameof(template));
        }

        // Get language definition
        var language = _repository.GetLoadedLanguage(languageId);
        if (language == null)
        {
            throw new InvalidOperationException($"Language '{languageId}' is not loaded");
        }

        // Get translation engine
        if (!_translationEngines.TryGetValue(languageId, out var translationEngine))
        {
            throw new InvalidOperationException($"Translation engine not available for '{languageId}'");
        }

        // Build English sentence from template
        var englishSentence = $"{template.Subject} {template.Verb} {template.Object}";

        // Translate to target language
        return translationEngine.TranslateToFantasy(englishSentence, languageId, language);
    }

    public string GenerateParagraph(string languageId, int sentenceCount)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException("Language ID cannot be null or empty", nameof(languageId));
        }

        if (sentenceCount <= 0)
        {
            throw new ArgumentException("Sentence count must be greater than zero", nameof(sentenceCount));
        }

        // Get language definition
        var language = _repository.GetLoadedLanguage(languageId);
        if (language == null)
        {
            throw new InvalidOperationException($"Language '{languageId}' is not loaded");
        }

        // Get name generator for word generation
        if (!_nameGenerators.TryGetValue(languageId, out var nameGenerator))
        {
            throw new InvalidOperationException($"Name generator not available for '{languageId}'");
        }

        var sentences = new List<string>();
        var random = new Random();

        // Generate varied sentences
        for (int i = 0; i < sentenceCount; i++)
        {
            // Generate random words for subject, verb, object
            var subject = nameGenerator.GenerateName(new NameGenerationOptions
            {
                MinSyllables = 2,
                MaxSyllables = 3,
                Type = NameType.Personal,
                Seed = random.Next()
            });

            var verb = nameGenerator.GenerateName(new NameGenerationOptions
            {
                MinSyllables = 1,
                MaxSyllables = 2,
                Type = NameType.Item,
                Seed = random.Next()
            });

            var obj = nameGenerator.GenerateName(new NameGenerationOptions
            {
                MinSyllables = 2,
                MaxSyllables = 3,
                Type = NameType.Item,
                Seed = random.Next()
            });

            // Apply word order
            var words = _grammarEngine.ApplyWordOrder(
                new[] { subject, verb, obj },
                language.Grammar.WordOrder);

            sentences.Add(string.Join(" ", words));
        }

        return string.Join(". ", sentences) + ".";
    }

    // NEW: Enhanced name generation with adapter
    public string GenerateNameAdvanced(
        string languageId,
        NameType nameType,
        GenerationMode mode = GenerationMode.RuleBased)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException("Language ID cannot be null or empty", nameof(languageId));
        }

        try
        {
            // Get language definition
            var language = _repository.GetLoadedLanguage(languageId);
            if (language == null)
            {
                throw new InvalidOperationException($"Language '{languageId}' is not loaded");
            }

            // Use the adapter for generation
            return _nameAdapter.GenerateName(language, nameType, mode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate advanced name for language '{LanguageId}'", languageId);
            throw;
        }
    }

    public string GenerateNameFromTemplate(
        string templateName,
        NameType nameType,
        GenerationMode mode = GenerationMode.RuleBased)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));
        }

        try
        {
            // Use the adapter for template-based generation
            return _nameAdapter.GenerateFromTemplate(templateName, nameType, mode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate name from template '{TemplateName}'", templateName);
            throw;
        }
    }

    public IReadOnlyList<string> GetAvailableNameGenerationTemplates()
    {
        try
        {
            return _nameAdapter.GetBuiltInTemplates();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available name generation templates");
            return new List<string>();
        }
    }
}
