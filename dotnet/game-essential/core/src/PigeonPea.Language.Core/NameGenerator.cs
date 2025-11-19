using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts;
using PigeonPea.Language.Contracts.Grammar;
using PigeonPea.Language.Contracts.Models;
using PigeonPea.Language.Contracts.Phonology;

namespace PigeonPea.Language.Core;

public class NameGenerator
{
    private readonly LanguageDefinition _language;
    private readonly PhonologyEngine _phonologyEngine;
    private readonly GrammarEngine _grammarEngine;
    private readonly ILogger<NameGenerator> _logger;

    public NameGenerator(
        LanguageDefinition language,
        PhonologyEngine phonologyEngine,
        GrammarEngine grammarEngine,
        ILogger<NameGenerator> logger)
    {
        _language = language ?? throw new ArgumentNullException(nameof(language));
        _phonologyEngine = phonologyEngine ?? throw new ArgumentNullException(nameof(phonologyEngine));
        _grammarEngine = grammarEngine ?? throw new ArgumentNullException(nameof(grammarEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GenerateName(NameGenerationOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        // Create Random instance with seed if provided
        var random = options.Seed.HasValue
            ? new Random(options.Seed.Value)
            : new Random();

        // Determine number of syllables
        var syllableCount = random.Next(options.MinSyllables, options.MaxSyllables + 1);

        // Generate name based on type
        var name = options.Type switch
        {
            NameType.Personal => GeneratePersonalName(syllableCount, random),
            NameType.Place => GeneratePlaceName(syllableCount, random),
            NameType.Item => GenerateItemName(syllableCount, random),
            NameType.Clan => GenerateClanName(syllableCount, random),
            NameType.Title => GenerateTitleName(syllableCount, random),
            _ => GenerateBasicName(syllableCount, random)
        };

        _logger.LogDebug("Generated {Type} name with {SyllableCount} syllables: {Name}",
            options.Type, syllableCount, name);

        return name;
    }

    public IEnumerable<string> GenerateNames(int count, NameGenerationOptions options)
    {
        if (count <= 0)
        {
            throw new ArgumentException("Count must be greater than zero", nameof(count));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var names = new List<string>();
        var usedNames = new HashSet<string>();

        // If seed is provided, use it as base and increment for each name
        var baseSeed = options.Seed ?? Environment.TickCount;

        for (int i = 0; i < count; i++)
        {
            var nameOptions = options with { Seed = baseSeed + i };
            var name = GenerateName(nameOptions);

            // Ensure uniqueness
            int attempts = 0;
            while (usedNames.Contains(name) && attempts < 100)
            {
                nameOptions = nameOptions with { Seed = baseSeed + count + attempts };
                name = GenerateName(nameOptions);
                attempts++;
            }

            usedNames.Add(name);
            names.Add(name);
        }

        _logger.LogDebug("Generated {Count} unique {Type} names", count, options.Type);

        return names;
    }

    private string GeneratePersonalName(int syllableCount, Random random)
    {
        // Personal names often use morphological suffixes
        var baseName = GenerateBasicName(syllableCount, random);

        // Apply morphological rules if available (e.g., feminine suffix -iel)
        if (_language.Grammar.MorphologyRules.Any())
        {
            var personalRules = _language.Grammar.MorphologyRules
                .Where(r => r.Type == MorphologyType.Suffix)
                .ToList();

            if (personalRules.Count > 0 && random.NextDouble() > 0.5)
            {
                var rule = personalRules[random.Next(personalRules.Count)];
                baseName = _grammarEngine.ApplyMorphology(baseName, rule);
            }
        }

        // Capitalize first letter
        return CapitalizeFirst(baseName);
    }

    private string GeneratePlaceName(int syllableCount, Random random)
    {
        // Place names often use compound words
        if (_language.Grammar.CompoundRules.Any() && syllableCount >= 3)
        {
            var halfSyllables = syllableCount / 2;
            var root1 = GenerateBasicName(halfSyllables, random);
            var root2 = GenerateBasicName(syllableCount - halfSyllables, random);

            var compoundRule = _language.Grammar.CompoundRules[random.Next(_language.Grammar.CompoundRules.Count)];
            var compound = _grammarEngine.FormCompound(new[] { root1, root2 }, compoundRule);

            return CapitalizeFirst(compound);
        }

        // Otherwise generate a longer basic name
        var name = GenerateBasicName(syllableCount, random);
        return CapitalizeFirst(name);
    }

    private string GenerateItemName(int syllableCount, Random random)
    {
        // Item names are often shorter and simpler
        var name = GenerateBasicName(Math.Max(1, syllableCount - 1), random);
        return CapitalizeFirst(name);
    }

    private string GenerateClanName(int syllableCount, Random random)
    {
        // Clan names often use compound words or morphological patterns
        var baseName = GenerateBasicName(syllableCount, random);

        // Apply genitive or plural morphology if available
        if (_language.Grammar.MorphologyRules.Any())
        {
            var clanRules = _language.Grammar.MorphologyRules
                .Where(r => r.Type == MorphologyType.Pluralization || r.Type == MorphologyType.CaseMarking)
                .ToList();

            if (clanRules.Count > 0 && random.NextDouble() > 0.3)
            {
                var rule = clanRules[random.Next(clanRules.Count)];
                baseName = _grammarEngine.ApplyMorphology(baseName, rule);
            }
        }

        return CapitalizeFirst(baseName);
    }

    private string GenerateTitleName(int syllableCount, Random random)
    {
        // Titles are often formal and use specific morphological patterns
        var baseName = GenerateBasicName(syllableCount, random);

        // Apply agent form or other formal morphology if available
        if (_language.Grammar.MorphologyRules.Any())
        {
            var titleRules = _language.Grammar.MorphologyRules
                .Where(r => r.Type == MorphologyType.Suffix || r.Type == MorphologyType.Prefix)
                .ToList();

            if (titleRules.Count > 0)
            {
                var rule = titleRules[random.Next(titleRules.Count)];
                baseName = _grammarEngine.ApplyMorphology(baseName, rule);
            }
        }

        return CapitalizeFirst(baseName);
    }

    private string GenerateBasicName(int syllableCount, Random random)
    {
        if (_language.Phonology.SyllableTemplates.Count == 0)
        {
            throw new InvalidOperationException("Language has no syllable templates defined");
        }

        var syllables = new List<string>();

        for (int i = 0; i < syllableCount; i++)
        {
            // Select a syllable template based on weights
            var template = SelectWeightedTemplate(random);

            // Generate syllable using the phonology engine
            var syllable = GenerateSyllableWithInventory(template, random);

            syllables.Add(syllable);
        }

        return string.Join("", syllables);
    }

    private SyllableTemplate SelectWeightedTemplate(Random random)
    {
        var templates = _language.Phonology.SyllableTemplates;

        // Calculate total weight
        var totalWeight = templates.Sum(t => t.Weight);

        // Select random value
        var value = random.Next(totalWeight);

        // Find template
        var currentWeight = 0;
        foreach (var template in templates)
        {
            currentWeight += template.Weight;
            if (value < currentWeight)
            {
                return template;
            }
        }

        // Fallback to first template
        return templates[0];
    }

    private string GenerateSyllableWithInventory(SyllableTemplate template, Random random)
    {
        var syllable = new System.Text.StringBuilder();
        var pattern = template.Pattern;
        var inventory = _language.Phonology.Inventory;

        int i = 0;
        while (i < pattern.Length)
        {
            char symbol = pattern[i];

            if (symbol == 'C')
            {
                // Check if this is part of a consonant cluster at the beginning
                if (i == 0 && i + 1 < pattern.Length && pattern[i + 1] == 'C')
                {
                    // Onset cluster
                    if (template.AllowedOnsets != null && template.AllowedOnsets.Count > 0)
                    {
                        var onset = template.AllowedOnsets[random.Next(template.AllowedOnsets.Count)];
                        syllable.Append(onset);
                        // Skip the consonants we've handled
                        while (i < pattern.Length && pattern[i] == 'C')
                        {
                            i++;
                        }
                        continue;
                    }
                }
                // Check if this is part of a consonant cluster at the end
                else if (i > 0 && i == pattern.LastIndexOf('C'))
                {
                    // Check if there are multiple C's at the end
                    int cCount = 0;
                    for (int j = i; j < pattern.Length && pattern[j] == 'C'; j++)
                    {
                        cCount++;
                    }

                    if (cCount > 1 && template.AllowedCodas != null && template.AllowedCodas.Count > 0)
                    {
                        var coda = template.AllowedCodas[random.Next(template.AllowedCodas.Count)];
                        syllable.Append(coda);
                        i += cCount;
                        continue;
                    }
                }

                // Single consonant - select from inventory
                if (inventory.Consonants.Count > 0)
                {
                    var consonant = inventory.Consonants[random.Next(inventory.Consonants.Count)];
                    syllable.Append(consonant);
                }

                i++;
            }
            else if (symbol == 'V')
            {
                // Vowel - select from inventory
                if (inventory.Vowels.Count > 0)
                {
                    var vowel = inventory.Vowels[random.Next(inventory.Vowels.Count)];
                    syllable.Append(vowel);
                }

                i++;
            }
            else
            {
                // Unknown symbol, skip it
                i++;
            }
        }

        return syllable.ToString();
    }

    private static string CapitalizeFirst(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (text.Length == 1)
        {
            return text.ToUpperInvariant();
        }

        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
