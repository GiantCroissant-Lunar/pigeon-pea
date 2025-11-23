using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts;
using PigeonPea.Language.Contracts.Grammar;
using PigeonPea.Language.Contracts.Models;
using PigeonPea.Language.Contracts.Phonology;
using FantasyNameGenerator;
using FantasyNameGenerator.NameTypes;
using FantasyNameGenerator.Phonology;
using FantasyNameGenerator.Phonotactics;
using FantasyNameGenerator.Morphology;
using FantasyNameGenerator.Grammar;
using Plate.SCG.General.AutoToString.Attributes;

namespace PigeonPea.Language.Core;

/// <summary>
/// Adapter for converting PigeonPea language definitions to FMG name generator templates
/// </summary>
[AutoToString]
public partial class LanguageToFMGAdapter : INameGeneratorAdapter
{
    private readonly ILogger<LanguageToFMGAdapter> _logger;
    [AddToString]
    private readonly Dictionary<string, string> _languageToTemplateMap;
    private readonly Dictionary<string, CulturePhonology> _templateCache;
    private readonly Dictionary<string, PhonotacticRules> _phonotacticsCache;
    private readonly Dictionary<string, MorphologyRules> _morphologyCache;
    private readonly Random _random;
    private readonly LanguageTemplateFactory _templateFactory;

    public LanguageToFMGAdapter(ILogger<LanguageToFMGAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();
        _templateCache = new Dictionary<string, CulturePhonology>();
        _phonotacticsCache = new Dictionary<string, PhonotacticRules>();
        _morphologyCache = new Dictionary<string, MorphologyRules>();
        _templateFactory = new LanguageTemplateFactory();

        // Map common language IDs to FMG built-in templates
        _languageToTemplateMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Real-world languages
            ["english"] = "germanic",
            ["common-tongue"] = "germanic",
            ["westron"] = "germanic",

            // Fantasy languages
            ["elvish"] = "elvish",
            ["sindarin"] = "elvish",
            ["quenya"] = "elvish",
            ["dwarvish"] = "dwarvish",
            ["khuzdul"] = "dwarvish",
            ["orcish"] = "orcish",
            ["black-speech"] = "orcish",

            // Asian languages
            ["japanese"] = "japanese",
            ["nihongo"] = "japanese",
            ["chinese"] = "chinese",
            ["mandarin"] = "chinese",
            ["korean"] = "korean",
            ["hangul"] = "korean",

            // European languages
            ["german"] = "germanic",
            ["french"] = "romance",
            ["spanish"] = "romance",
            ["italian"] = "romance",
            ["russian"] = "slavic",
            ["polish"] = "slavic",

            // Other languages
            ["nordic"] = "nordic",
            ["viking"] = "nordic",
            ["celtic"] = "celtic",
            ["gaelic"] = "celtic",
            ["arabic"] = "arabic"
        };
    }

    public string GenerateName(
        LanguageDefinition language,
        PigeonPea.Language.Contracts.NameType nameType,
        GenerationMode mode = GenerationMode.RuleBased)
    {
        if (language == null)
            throw new ArgumentNullException(nameof(language));

        try
        {
            // Try to use built-in template first
            var normalizedId = language.Id.ToLowerInvariant();
            if (_languageToTemplateMap.TryGetValue(normalizedId, out var templateName))
            {
                return GenerateFromTemplate(templateName, nameType, mode);
            }

            // Convert custom language to FMG template
            var phonology = ConvertToFMGPhonology(language);
            var phonotactics = ConvertToFMGPhonotactics(language);
            var morphology = ConvertToFMGMorphology(language);

            // Create FMG name generator
            var fmgGenerator = new NameGenerator(phonology, phonotactics, morphology, _random);

            // Map name types
            var fmgNameType = MapNameType(nameType);

            // Generate name
            return fmgGenerator.Generate(fmgNameType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate name for language '{LanguageId}'", language.Id);
            throw;
        }
    }

    public string GenerateFromTemplate(
        string templateName,
        PigeonPea.Language.Contracts.NameType nameType,
        GenerationMode mode = GenerationMode.RuleBased)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));

        // TODO: Implement GenerationMode support (requires RFC-018)
        // Currently only RuleBased mode is supported
        // Markov and Hybrid modes will be added when RFC-018 is implemented
        if (mode != GenerationMode.RuleBased)
        {
            _logger.LogWarning(
                "Generation mode '{Mode}' not yet supported, falling back to RuleBased",
                mode);
        }

        try
        {
            // Load template from cache or create new one
            if (!_templateCache.TryGetValue(templateName, out var phonology))
            {
                phonology = LoadBuiltInTemplate(templateName);
                _templateCache[templateName] = phonology;
            }

            if (!_phonotacticsCache.TryGetValue(templateName, out var phonotactics))
            {
                phonotactics = LoadBuiltInPhonotactics(templateName);
                _phonotacticsCache[templateName] = phonotactics;
            }

            if (!_morphologyCache.TryGetValue(templateName, out var morphology))
            {
                morphology = LoadBuiltInMorphology(templateName);
                _morphologyCache[templateName] = morphology;
            }

            // Create FMG name generator
            var fmgGenerator = new NameGenerator(phonology, phonotactics, morphology, _random);

            // Map name types
            var fmgNameType = MapNameType(nameType);

            // Generate name
            return fmgGenerator.Generate(fmgNameType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate name from template '{TemplateName}'", templateName);
            throw;
        }
    }

    public bool HasBuiltInTemplate(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return false;

        return _languageToTemplateMap.ContainsValue(templateName.ToLowerInvariant()) ||
               _templateCache.ContainsKey(templateName.ToLowerInvariant());
    }

    public IReadOnlyList<string> GetBuiltInTemplates()
    {
        return _languageToTemplateMap.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private CulturePhonology ConvertToFMGPhonology(LanguageDefinition language)
    {
        var cacheKey = language.Id;
        if (_templateCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var phonology = new CulturePhonology
        {
            Name = language.Id,
            Inventory = new PhonemeInventory
            {
                Consonants = string.Join("", language.Phonology.Inventory.Consonants),
                Vowels = string.Join("", language.Phonology.Inventory.Vowels),
                Liquids = ExtractLiquids(language.Phonology.Inventory.Consonants),
                Nasals = ExtractNasals(language.Phonology.Inventory.Consonants),
                Fricatives = ExtractFricatives(language.Phonology.Inventory.Consonants),
                Stops = ExtractStops(language.Phonology.Inventory.Consonants),
                Sibilants = ExtractSibilants(language.Phonology.Inventory.Consonants),
                Finals = string.Join("", language.Phonology.Clusters?.FinalClusters ?? Array.Empty<string>())
            }
        };

        _templateCache[cacheKey] = phonology;
        return phonology;
    }

    private PhonotacticRules ConvertToFMGPhonotactics(LanguageDefinition language)
    {
        var cacheKey = language.Id;
        if (_phonotacticsCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var templates = language.Phonology.SyllableTemplates ?? Array.Empty<SyllableTemplate>();

        var phonotactics = new PhonotacticRules
        {
            // Note: FMG PhonotacticRules doesn't have Structures property
            // We'll use the default implementation from LanguageTemplateFactory
            AllowedOnsets = language.Phonology.Clusters?.InitialClusters?.ToArray() ?? Array.Empty<string>(),
            AllowedCodas = language.Phonology.Clusters?.FinalClusters?.ToArray() ?? Array.Empty<string>(),
            MaxConsonantCluster = language.Phonology.Clusters?.MaxConsonantCluster ?? 2,
            MaxVowelCluster = 2,
            MinSyllables = 1,
            MaxSyllables = 3,
            EnforceSonoritySequencing = true
        };

        _phonotacticsCache[cacheKey] = phonotactics;
        return phonotactics;
    }

    private MorphologyRules ConvertToFMGMorphology(LanguageDefinition language)
    {
        var cacheKey = language.Id;
        if (_morphologyCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var morphologyRules = language.Grammar.MorphologyRules ?? Array.Empty<MorphologyRule>();

        var prefixes = morphologyRules
            .Where(r => r.Type == MorphologyType.Prefix)
            .Select(r => new Morpheme
            {
                Form = ExtractPrefix(r.Pattern),
                Meaning = r.Name,
                Frequency = 0.1
            })
            .ToArray();

        var suffixes = morphologyRules
            .Where(r => r.Type == MorphologyType.Suffix)
            .Select(r => new Morpheme
            {
                Form = ExtractSuffix(r.Pattern),
                Meaning = r.Name,
                Frequency = 0.1
            })
            .ToArray();

        // Note: FMG MorphologyRules constructor requires a seed parameter
        var morphology = new MorphologyRules(_random.Next())
        {
            Prefixes = prefixes,
            Suffixes = suffixes
        };

        _morphologyCache[cacheKey] = morphology;
        return morphology;
    }

    private CulturePhonology LoadBuiltInTemplate(string templateName)
    {
        return _templateFactory.CreatePhonology(templateName);
    }

    private PhonotacticRules LoadBuiltInPhonotactics(string templateName)
    {
        return _templateFactory.CreatePhonotactics(templateName);
    }

    private MorphologyRules LoadBuiltInMorphology(string templateName)
    {
        return _templateFactory.CreateMorphology(templateName);
    }

    // Helper methods for extracting phoneme categories
    private string ExtractLiquids(string[] consonants)
    {
        var liquids = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "l", "r", "w", "y", "ɹ", "ɻ"
        };

        return string.Join("", consonants.Where(c => liquids.Contains(c)));
    }

    private string ExtractNasals(string[] consonants)
    {
        var nasals = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "m", "n", "ŋ", "ɲ", "ɴ", "ng"
        };

        return string.Join("", consonants.Where(c => nasals.Contains(c)));
    }

    private string ExtractFricatives(string[] consonants)
    {
        var fricatives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "f", "v", "s", "z", "ʃ", "ʒ", "θ", "ð", "h", "x", "ɣ", "sh", "th", "ch", "zh"
        };

        return string.Join("", consonants.Where(c => fricatives.Contains(c)));
    }

    private string ExtractStops(string[] consonants)
    {
        var stops = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "p", "t", "k", "b", "d", "g", "ʔ", "q", "ɢ"
        };

        return string.Join("", consonants.Where(c => stops.Contains(c)));
    }

    private string ExtractSibilants(string[] consonants)
    {
        var sibilants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "s", "z", "ʃ", "ʒ", "ʦ", "ʧ", "ʨ", "sh", "zh", "ts"
        };

        return string.Join("", consonants.Where(c => sibilants.Contains(c)));
    }

    private string ExtractPrefix(string pattern)
    {
        // Extract prefix from pattern like "ed{root}" → "ed"
        return pattern.Replace("{root}", "");
    }

    private string ExtractSuffix(string pattern)
    {
        // Extract suffix from pattern like "{root}ed" → "ed"
        return pattern.Replace("{root}", "");
    }

    private FantasyNameGenerator.NameTypes.NameType MapNameType(PigeonPea.Language.Contracts.NameType type)
    {
        return type switch
        {
            PigeonPea.Language.Contracts.NameType.Personal => FantasyNameGenerator.NameTypes.NameType.Person,
            PigeonPea.Language.Contracts.NameType.Place => FantasyNameGenerator.NameTypes.NameType.Region,
            PigeonPea.Language.Contracts.NameType.Item => FantasyNameGenerator.NameTypes.NameType.Person,
            PigeonPea.Language.Contracts.NameType.Clan => FantasyNameGenerator.NameTypes.NameType.Culture,
            PigeonPea.Language.Contracts.NameType.Title => FantasyNameGenerator.NameTypes.NameType.Person,
            _ => FantasyNameGenerator.NameTypes.NameType.Person
        };
    }
}
