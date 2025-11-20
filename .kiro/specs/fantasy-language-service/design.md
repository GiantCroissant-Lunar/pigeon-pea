# Design Document: Fantasy Language Service

## Overview

The Fantasy Language Service is a comprehensive system for creating, managing, and using constructed fantasy languages in the PigeonPea game engine. The service enables game developers to define linguistically consistent languages with distinct phonological, morphological, and syntactic characteristics. It supports bidirectional translation between English and fantasy languages, procedural name generation, and language evolution through sound change rules.

The system is designed to integrate with the existing plugin architecture (RFC-013) and follows the tier-based service pattern. Language definitions are data-driven through JSON/YAML configuration files, allowing for easy creation and modification without code changes.

### Key Features

- **Multi-language support**: Define unlimited fantasy languages with unique characteristics
- **Phonotactic engine**: Generate words that sound authentic to each language
- **Bidirectional translation**: Convert between English and fantasy languages
- **Morphological system**: Support for roots, affixes, and word formation rules
- **Grammar engine**: Apply syntactic rules for sentence generation
- **Sound change rules**: Create language evolution and dialects
- **Name generation**: Procedurally generate names for NPCs, locations, and items
- **Plugin architecture**: Extensible through custom language generators
- **Configuration-driven**: Define languages through JSON/YAML files

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Game Application                          │
│  (Requests translations, name generation, etc.)              │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│              Language Service Facade                         │
│  (ILanguageService - Tier 1 Contract)                        │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│           Language Service Implementation                    │
│  (Coordinates all subsystems)                                │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Phonology   │  │  Lexicon     │  │  Grammar     │      │
│  │  Engine      │  │  Manager     │  │  Engine      │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ Translation  │  │    Name      │  │ Sound Change │      │
│  │   Engine     │  │  Generator   │  │   Engine     │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│            Language Definition Repository                    │
│  (Loads/saves language configs from JSON/YAML)               │
└─────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

**Language Service Facade**
- Primary interface for all language operations
- Routes requests to appropriate subsystems
- Manages language lifecycle (load, unload, reload)

**Phonology Engine**
- Validates phoneme inventories
- Generates syllables based on templates
- Enforces phonotactic constraints
- Produces phonologically valid word forms

**Lexicon Manager**
- Stores word mappings between languages
- Supports forward and reverse lookups
- Manages morphological information
- Handles multi-meaning words

**Grammar Engine**
- Applies word order transformations
- Handles morphological inflections
- Manages compound word formation
- Validates grammatical correctness

**Translation Engine**
- Tokenizes input text
- Performs lexicon lookups
- Applies grammar transformations
- Handles unknown words

**Name Generator**
- Generates random names using phonotactics
- Applies morphological rules
- Supports seeded generation for determinism
- Produces names of configurable length

**Sound Change Engine**
- Applies phonological transformation rules
- Derives daughter languages from parent languages
- Handles contextual sound changes
- Maintains phonological validity

**Language Definition Repository**
- Loads language definitions from configuration files
- Validates configuration structure
- Serializes languages to JSON/YAML
- Supports hot-reloading

## Components and Interfaces

### Core Contracts (Tier 1)

```csharp
namespace PigeonPea.Language.Contracts;

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
    public string Subject { get; init; }
    public string Verb { get; init; }
    public string Object { get; init; }
    public Dictionary<string, string> Modifiers { get; init; } = new();
}
```

### Phonology Contracts

```csharp
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
    public IReadOnlyList<string> Vowels { get; init; }
    public IReadOnlyList<string> Consonants { get; init; }
    public IReadOnlyList<string> Diphthongs { get; init; }
}

public record SyllableTemplate
{
    public string Pattern { get; init; } // e.g., "CVC", "CCVC", "CV"
    public IReadOnlyList<string> AllowedOnsets { get; init; }
    public IReadOnlyList<string> AllowedCodas { get; init; }
}

public record PhonologyRules
{
    public PhonemeInventory Inventory { get; init; }
    public IReadOnlyList<SyllableTemplate> SyllableTemplates { get; init; }
    public ClusterRules Clusters { get; init; }
}

public record ClusterRules
{
    public IReadOnlyList<string> InitialClusters { get; init; }
    public IReadOnlyList<string> MedialClusters { get; init; }
    public IReadOnlyList<string> FinalClusters { get; init; }
}
```

### Lexicon Contracts

```csharp
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
    public string Word { get; init; }
    public string Meaning { get; init; }
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
```

### Grammar Contracts

```csharp
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
    public IReadOnlyList<MorphologyRule> MorphologyRules { get; init; }
    public IReadOnlyList<CompoundRule> CompoundRules { get; init; }
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
    public string Name { get; init; }
    public MorphologyType Type { get; init; }
    public string Pattern { get; init; } // e.g., "{root}iel" for feminine suffix
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
    public string Pattern { get; init; } // e.g., "{root1}{root2}"
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
```

### Sound Change Contracts

```csharp
namespace PigeonPea.Language.Contracts.SoundChange;

public interface ISoundChangeEngine
{
    string ApplySoundChange(string word, SoundChangeRule rule);
    IEnumerable<string> ApplySoundChanges(IEnumerable<string> words, IEnumerable<SoundChangeRule> rules);
    LanguageDefinition DeriveLanguage(LanguageDefinition parent, IEnumerable<SoundChangeRule> rules);
}

public record SoundChangeRule
{
    public string Name { get; init; }
    public string SourcePhoneme { get; init; }
    public string TargetPhoneme { get; init; }
    public string? Context { get; init; } // e.g., "_a" means "before 'a'"
    public int Order { get; init; }
}
```

## Data Models

### Language Definition

```csharp
namespace PigeonPea.Language.Contracts.Models;

public record LanguageDefinition
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public PhonologyRules Phonology { get; init; }
    public GrammarRules Grammar { get; init; }
    public string? ParentLanguageId { get; init; }
    public IReadOnlyList<SoundChangeRule> SoundChanges { get; init; } = Array.Empty<SoundChangeRule>();
    public Dictionary<string, object> Metadata { get; init; } = new();
}
```

### Configuration File Format

```yaml
# Example: elvish.yaml
id: "high-elvish"
name: "High Elvish"
description: "The ancient tongue of the Elven nobility"

phonology:
  vowels: ["a", "e", "i", "o", "u", "á", "é", "í"]
  consonants: ["l", "r", "n", "m", "s", "h", "t", "th", "d", "v", "f"]
  diphthongs: ["ae", "ai", "ei"]
  
  syllable_templates:
    - pattern: "CV"
      weight: 40
    - pattern: "CVC"
      weight: 30
    - pattern: "V"
      weight: 20
    - pattern: "CCV"
      weight: 10
      allowed_onsets: ["th", "sh", "el", "ar"]
  
  clusters:
    initial: ["l", "r", "th", "sh", "el", "ar"]
    medial: ["lv", "rl", "nd", "rv", "ll"]
    final: ["n", "l", "r"]

grammar:
  word_order: "SVO"
  
  morphology_rules:
    - name: "feminine_suffix"
      type: "Suffix"
      pattern: "{root}iel"
      condition: "feminine"
    
    - name: "plural"
      type: "Suffix"
      pattern: "{root}lir"
      condition: "plural"
    
    - name: "genitive"
      type: "Suffix"
      pattern: "{root}ien"
      condition: "genitive"
  
  compound_rules:
    - pattern: "{root1}{root2}"
      type: "Noun_Noun"
    
    - pattern: "{root1}a{root2}"
      type: "Adjective_Noun"
      connector: "a"

lexicon_path: "./lexicons/high-elvish.json"

metadata:
  author: "Game Developer"
  version: "1.0"
  created: "2025-11-19"
```

### Lexicon File Format

```json
{
  "language_id": "high-elvish",
  "entries": [
    {
      "word": "aelór",
      "meaning": "fire",
      "root": "ael",
      "affixes": ["ór"],
      "part_of_speech": "Noun"
    },
    {
      "word": "miriel",
      "meaning": "jewel",
      "root": "mir",
      "affixes": ["iel"],
      "part_of_speech": "Noun",
      "alternate_meanings": ["beloved", "treasure"]
    },
    {
      "word": "thalor",
      "meaning": "mountain",
      "root": "thal",
      "affixes": ["or"],
      "part_of_speech": "Noun"
    }
  ]
}
```


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Phoneme inventory validation accepts all valid inventories
*For any* phoneme inventory with valid vowels and consonants, the system should accept it without errors.
**Validates: Requirements 1.1**

### Property 2: Syllable template validation correctly identifies valid patterns
*For any* syllable template, the validation should accept it if and only if it follows standard patterns (V, CV, VC, CVC, CCV, CCVC, etc.).
**Validates: Requirements 1.2**

### Property 3: Cluster storage round-trip preserves data
*For any* set of consonant cluster definitions, storing and then retrieving should return identical cluster combinations.
**Validates: Requirements 1.3**

### Property 4: Language isolation maintains independence
*For any* two language definitions, modifying the phonological rules of one should not affect the phonological rules of the other.
**Validates: Requirements 1.4**

### Property 5: Phoneme reference validation catches invalid references
*For any* language definition, if a syllable template references a phoneme not in the inventory, the validation should reject it.
**Validates: Requirements 1.5**

### Property 6: Lexicon entry round-trip preserves mappings
*For any* lexicon entry, adding it to the lexicon and then looking it up by meaning should return an equivalent entry.
**Validates: Requirements 2.1**

### Property 7: Morphological data structure preservation
*For any* lexicon entry with root and affixes, storing and retrieving should preserve the root and affixes as separate fields.
**Validates: Requirements 2.2**

### Property 8: Lexicon query returns all matching entries
*For any* set of lexicon entries and a meaning query, the query should return exactly those entries whose meaning matches the query.
**Validates: Requirements 2.3**

### Property 9: Multiple meanings storage completeness
*For any* word with multiple meanings, all meanings should be retrievable from the lexicon.
**Validates: Requirements 2.4**

### Property 10: Lexicon serialization round-trip
*For any* lexicon, exporting to JSON/YAML and then importing should preserve all entries with identical data.
**Validates: Requirements 2.5, 8.1, 8.3**

### Property 11: Word order transformation correctness
*For any* word sequence and target word order (SVO, SOV, VSO, VOS, OVS, OSV), applying the transformation should produce the correct ordering.
**Validates: Requirements 3.1, 3.4**

### Property 12: Morphological rules storage completeness
*For any* set of morphological rules (pluralization, case marking, verb conjugation), all rules should be stored and retrievable.
**Validates: Requirements 3.2**

### Property 13: Compound word formation follows rules
*For any* compound rule and set of roots, the formed compound should match the pattern specified in the rule.
**Validates: Requirements 3.3**

### Property 14: Morpheme reference validation
*For any* grammar rule that references a morpheme, the system should reject the rule if the morpheme doesn't exist in the language definition.
**Validates: Requirements 3.5**

### Property 15: Generated names follow phonotactic constraints
*For any* language definition and name generation request, all generated names should be phonotactically valid according to the language's rules.
**Validates: Requirements 4.1**

### Property 16: Name length respects syllable constraints
*For any* name generation request with specified min/max syllables, the generated name should have a syllable count within that range.
**Validates: Requirements 4.2**

### Property 17: Morphological name generation follows rules
*For any* name generated using morphological rules, the combination of roots and affixes should follow the language's morphology rules.
**Validates: Requirements 4.3**

### Property 18: Name generation produces diverse yet consistent output
*For any* language definition, generating multiple names should produce different names that all follow the same phonological rules.
**Validates: Requirements 4.4**

### Property 19: Seeded name generation is deterministic
*For any* seed value and name generation parameters, generating names multiple times with the same seed should produce identical results.
**Validates: Requirements 4.5**

### Property 20: English tokenization correctness
*For any* English sentence, tokenization should split it into words at whitespace and punctuation boundaries.
**Validates: Requirements 5.1**

### Property 21: Translation performs lexicon lookup for all tokens
*For any* tokenized sentence, each token should result in a lexicon query.
**Validates: Requirements 5.2**

### Property 22: Grammar transformation applies word order correctly
*For any* sentence and target language word order, the transformation should reorder words according to the target syntax.
**Validates: Requirements 5.3**

### Property 23: Unknown word fallback handling
*For any* word not in the lexicon, the translation should either generate a phonologically appropriate word or mark it as untranslatable.
**Validates: Requirements 5.4**

### Property 24: Fantasy language tokenization respects morphology
*For any* fantasy language text, tokenization should split words at morphological boundaries according to the language's morphology rules.
**Validates: Requirements 6.1**

### Property 25: Reverse lexicon lookup correctness
*For any* fantasy language word in the lexicon, reverse lookup should return the correct English meaning.
**Validates: Requirements 6.2**

### Property 26: Reverse grammar transformation to English SVO
*For any* fantasy language sentence, applying reverse grammar rules should produce English word order (SVO).
**Validates: Requirements 6.3**

### Property 27: Unknown fantasy word preservation
*For any* fantasy language word not in the lexicon, the translation should preserve the original word in the output.
**Validates: Requirements 6.4**

### Property 28: Sound change rule acceptance
*For any* valid sound change rule (source phoneme, target phoneme, optional context), the system should accept it.
**Validates: Requirements 7.1**

### Property 29: Sound change application order correctness
*For any* word and ordered list of sound change rules, applying the rules should transform the word in the specified order.
**Validates: Requirements 7.2**

### Property 30: Contextual sound change application
*For any* word and contextual sound change rule, the rule should only apply when the specified context is present in the word.
**Validates: Requirements 7.3**

### Property 31: Daughter language derivation completeness
*For any* parent language and set of sound change rules, deriving a daughter language should apply all rules to all lexicon entries.
**Validates: Requirements 7.4**

### Property 32: Configuration file parsing validation
*For any* malformed configuration file, the system should report validation errors with specific line numbers.
**Validates: Requirements 8.2**

### Property 33: Language inheritance and derivation
*For any* parent language and child language with sound changes, the child should inherit the parent's features and apply the specified transformations.
**Validates: Requirements 8.4**

### Property 34: Plugin discovery completeness
*For any* set of plugin directories, all valid language plugins in those directories should be discovered.
**Validates: Requirements 9.1**

### Property 35: Plugin contract validation
*For any* plugin, the system should verify that it implements the required ILanguageService contracts before loading.
**Validates: Requirements 9.2**

### Property 36: Plugin registration completeness
*For any* set of valid plugins, all should be registered in the service registry.
**Validates: Requirements 9.3**

### Property 37: Service resolution correctness
*For any* language identifier, requesting a language service should resolve to the correct plugin for that language.
**Validates: Requirements 9.4**

### Property 38: Plugin load failure resilience
*For any* set of plugins where one fails to load, the system should log the error and successfully load all other valid plugins.
**Validates: Requirements 9.5**

### Property 39: Paragraph generation follows grammar rules
*For any* language definition and paragraph generation request, all sentences in the generated paragraph should be grammatically valid according to the language's rules.
**Validates: Requirements 10.1**

### Property 40: Sentence structure diversity
*For any* generated paragraph, the sentences should exhibit varied structures rather than all following the same pattern.
**Validates: Requirements 10.2**

### Property 41: Template filling uses valid vocabulary
*For any* sentence template, all placeholders should be filled with words from the lexicon that match the required part of speech.
**Validates: Requirements 10.3**

### Property 42: Paragraph length approximation
*For any* requested sentence count, the generated paragraph should contain approximately that many sentences (within ±1 sentence tolerance).
**Validates: Requirements 10.5**

## Error Handling

### Error Categories

**Configuration Errors**
- Invalid phoneme inventory (empty vowels/consonants)
- Malformed syllable templates
- Invalid sound change rules
- Missing required fields in configuration files

**Runtime Errors**
- Language not loaded when requested
- Lexicon lookup failures
- Translation failures due to missing lexicon entries
- Plugin loading failures

**Validation Errors**
- Phoneme references in syllable templates that don't exist in inventory
- Grammar rules referencing non-existent morphemes
- Invalid word order specifications
- Circular language inheritance

### Error Handling Strategy

```csharp
public class LanguageServiceException : Exception
{
    public ErrorCategory Category { get; }
    public string LanguageId { get; }
    public Dictionary<string, object> Context { get; }
    
    public LanguageServiceException(
        string message, 
        ErrorCategory category, 
        string languageId = null,
        Exception innerException = null)
        : base(message, innerException)
    {
        Category = category;
        LanguageId = languageId;
        Context = new Dictionary<string, object>();
    }
}

public enum ErrorCategory
{
    Configuration,
    Runtime,
    Validation,
    PluginLoading
}
```

### Error Recovery

**Graceful Degradation**
- If a language fails to load, log error and continue with other languages
- If translation fails, return original text with error marker
- If name generation fails, fall back to simple random syllable generation
- If plugin loading fails, continue with built-in implementations

**Validation Before Execution**
- Validate all language definitions on load
- Validate lexicon entries before adding
- Validate grammar rules before applying
- Validate sound change rules before deriving languages

**Detailed Error Messages**
- Include language ID in all error messages
- Include line numbers for configuration errors
- Include context information (e.g., which phoneme was invalid)
- Suggest corrections when possible

## Testing Strategy

### Unit Testing

**Phonology Engine Tests**
```csharp
[Test]
public void PhonologyEngine_ValidatesPhonemeInventory_AcceptsValidInventory()
{
    var inventory = new PhonemeInventory
    {
        Vowels = new[] { "a", "e", "i", "o", "u" },
        Consonants = new[] { "p", "t", "k", "s", "m", "n" }
    };
    
    var engine = new PhonologyEngine();
    var result = engine.ValidatePhonemeInventory(inventory);
    
    Assert.IsTrue(result);
}

[Test]
public void PhonologyEngine_GenerateSyllable_FollowsTemplate()
{
    var template = new SyllableTemplate { Pattern = "CVC" };
    var inventory = CreateTestInventory();
    var engine = new PhonologyEngine();
    
    var syllable = engine.GenerateSyllable(template, new Random(42));
    
    Assert.AreEqual(3, syllable.Length);
    Assert.IsTrue(IsConsonant(syllable[0], inventory));
    Assert.IsTrue(IsVowel(syllable[1], inventory));
    Assert.IsTrue(IsConsonant(syllable[2], inventory));
}
```

**Lexicon Manager Tests**
```csharp
[Test]
public void LexiconManager_AddAndLookup_RoundTrip()
{
    var manager = new LexiconManager();
    var entry = new LexiconEntry
    {
        Word = "aelór",
        Meaning = "fire",
        Root = "ael",
        PartOfSpeech = PartOfSpeech.Noun
    };
    
    manager.AddEntry("elvish", entry);
    var retrieved = manager.LookupByMeaning("elvish", "fire");
    
    Assert.IsNotNull(retrieved);
    Assert.AreEqual(entry.Word, retrieved.Word);
    Assert.AreEqual(entry.Meaning, retrieved.Meaning);
}
```

**Grammar Engine Tests**
```csharp
[Test]
public void GrammarEngine_ApplyWordOrder_TransformsSVO_ToSOV()
{
    var engine = new GrammarEngine();
    var words = new[] { "I", "see", "you" }; // SVO
    
    var result = engine.ApplyWordOrder(words, WordOrder.SOV);
    
    Assert.AreEqual(new[] { "I", "you", "see" }, result);
}
```

**Translation Engine Tests**
```csharp
[Test]
public void TranslationEngine_Translate_AppliesLexiconAndGrammar()
{
    var engine = CreateTranslationEngine();
    var text = "The fire burns";
    
    var result = engine.Translate(text, "english", "elvish");
    
    Assert.IsNotNull(result);
    Assert.IsTrue(result.Contains("aelór")); // "fire" in elvish
}
```

### Property-Based Testing

The system will use **FsCheck** (F# property testing library with C# support) for property-based testing.

**Property Test Configuration**
```csharp
// Configure FsCheck to run 100 iterations per property
[assembly: FsCheck.NUnit.Property(MaxTest = 100, QuietOnSuccess = true)]
```

**Property Test Examples**

```csharp
// Property 6: Lexicon entry round-trip preserves mappings
[FsCheck.NUnit.Property]
public Property LexiconEntry_RoundTrip_PreservesData()
{
    return Prop.ForAll(
        ArbitraryLexiconEntry(),
        entry =>
        {
            var manager = new LexiconManager();
            manager.AddEntry("test-lang", entry);
            var retrieved = manager.LookupByMeaning("test-lang", entry.Meaning);
            
            return retrieved != null &&
                   retrieved.Word == entry.Word &&
                   retrieved.Meaning == entry.Meaning &&
                   retrieved.Root == entry.Root;
        });
}

// Property 15: Generated names follow phonotactic constraints
[FsCheck.NUnit.Property]
public Property GeneratedNames_FollowPhonotacticRules()
{
    return Prop.ForAll(
        ArbitraryLanguageDefinition(),
        langDef =>
        {
            var generator = new NameGenerator(langDef);
            var name = generator.GenerateName(new NameGenerationOptions());
            var validator = new PhonologyEngine();
            
            return validator.IsValidWord(name, langDef.Phonology);
        });
}

// Property 19: Seeded name generation is deterministic
[FsCheck.NUnit.Property]
public Property SeededNameGeneration_IsDeterministic()
{
    return Prop.ForAll(
        Arb.Default.Int32(),
        seed =>
        {
            var langDef = CreateTestLanguage();
            var options = new NameGenerationOptions { Seed = seed };
            var generator = new NameGenerator(langDef);
            
            var name1 = generator.GenerateName(options);
            var name2 = generator.GenerateName(options);
            
            return name1 == name2;
        });
}

// Property 10: Lexicon serialization round-trip
[FsCheck.NUnit.Property]
public async Task<Property> LexiconSerialization_RoundTrip_PreservesData()
{
    return Prop.ForAll(
        ArbitraryLexiconEntries(),
        async entries =>
        {
            var manager = new LexiconManager();
            foreach (var entry in entries)
            {
                manager.AddEntry("test-lang", entry);
            }
            
            var tempFile = Path.GetTempFileName();
            await manager.SaveLexiconAsync("test-lang", tempFile);
            
            var manager2 = new LexiconManager();
            await manager2.LoadLexiconAsync("test-lang", tempFile);
            
            var retrieved = manager2.GetAllEntries("test-lang").ToList();
            
            File.Delete(tempFile);
            
            return entries.Count == retrieved.Count &&
                   entries.All(e => retrieved.Any(r => 
                       r.Word == e.Word && r.Meaning == e.Meaning));
        });
}
```

**Custom Generators for Property Tests**

```csharp
public static class LanguageArbitraries
{
    public static Arbitrary<LexiconEntry> ArbitraryLexiconEntry()
    {
        return Arb.From(
            from word in GenWord()
            from meaning in GenMeaning()
            from root in GenRoot()
            from pos in Arb.Generate<PartOfSpeech>()
            select new LexiconEntry
            {
                Word = word,
                Meaning = meaning,
                Root = root,
                PartOfSpeech = pos
            });
    }
    
    public static Arbitrary<LanguageDefinition> ArbitraryLanguageDefinition()
    {
        return Arb.From(
            from id in GenLanguageId()
            from phonology in GenPhonologyRules()
            from grammar in GenGrammarRules()
            select new LanguageDefinition
            {
                Id = id,
                Name = id,
                Phonology = phonology,
                Grammar = grammar
            });
    }
    
    private static Gen<string> GenWord() =>
        from length in Gen.Choose(3, 10)
        from chars in Gen.ArrayOf(length, Gen.Elements("aeioulrnmsth"))
        select new string(chars);
    
    private static Gen<string> GenMeaning() =>
        Gen.Elements("fire", "water", "earth", "wind", "light", "dark", 
                     "mountain", "forest", "river", "star");
    
    private static Gen<string> GenRoot() =>
        from length in Gen.Choose(2, 5)
        from chars in Gen.ArrayOf(length, Gen.Elements("aeioulrnm"))
        select new string(chars);
    
    private static Gen<string> GenLanguageId() =>
        Gen.Elements("elvish", "dwarvish", "draconic", "goblin");
    
    private static Gen<PhonologyRules> GenPhonologyRules() =>
        from vowels in Gen.NonEmptyListOf(Gen.Elements("a", "e", "i", "o", "u"))
        from consonants in Gen.NonEmptyListOf(Gen.Elements("p", "t", "k", "s", "m", "n", "l", "r"))
        select new PhonologyRules
        {
            Inventory = new PhonemeInventory
            {
                Vowels = vowels.ToArray(),
                Consonants = consonants.ToArray()
            },
            SyllableTemplates = new[] 
            { 
                new SyllableTemplate { Pattern = "CV" },
                new SyllableTemplate { Pattern = "CVC" }
            }
        };
    
    private static Gen<GrammarRules> GenGrammarRules() =>
        from wordOrder in Arb.Generate<WordOrder>()
        select new GrammarRules
        {
            WordOrder = wordOrder,
            MorphologyRules = Array.Empty<MorphologyRule>(),
            CompoundRules = Array.Empty<CompoundRule>()
        };
}
```

### Integration Testing

**Full Translation Pipeline**
```csharp
[Test]
public async Task FullPipeline_LoadLanguage_TranslateText()
{
    var service = new LanguageService();
    await service.LoadLanguageAsync("elvish", "configs/elvish.yaml");
    
    var result = await service.TranslateAsync(
        "The fire burns in the mountain",
        "english",
        "elvish");
    
    Assert.IsNotNull(result);
    Assert.IsTrue(result.Length > 0);
}

[Test]
public async Task FullPipeline_GenerateAndTranslateNames()
{
    var service = new LanguageService();
    await service.LoadLanguageAsync("elvish", "configs/elvish.yaml");
    
    var name = service.GenerateName("elvish", new NameGenerationOptions
    {
        MinSyllables = 2,
        MaxSyllables = 3,
        Type = NameType.Personal
    });
    
    Assert.IsNotNull(name);
    Assert.IsTrue(name.Length >= 4); // At least 2 syllables
}
```

**Plugin Integration**
```csharp
[Test]
public async Task PluginSystem_LoadsLanguagePlugins()
{
    var pluginLoader = new PluginLoader();
    var count = await pluginLoader.DiscoverAndLoadAsync(
        new[] { "plugins/languages" },
        "game");
    
    Assert.Greater(count, 0);
    
    var service = ServiceRegistry.Get<ILanguageService>();
    Assert.IsNotNull(service);
}
```

### Manual/Visual Testing

**Name Generation Samples**
- Generate 100 names for each language
- Verify they "sound right" for the language
- Check for phonological consistency
- Ensure diversity in output

**Translation Samples**
- Translate common phrases to each language
- Verify grammar rules are applied correctly
- Check that word order matches language specification
- Translate back to English and verify meaning preservation

**Configuration Validation**
- Test with intentionally malformed configs
- Verify error messages are helpful
- Check that line numbers are accurate
- Ensure validation catches all error types

## Implementation Notes

### Performance Considerations

**Caching**
- Cache loaded language definitions
- Cache lexicon lookups (LRU cache)
- Cache generated syllables for reuse
- Cache compiled sound change rules

**Lazy Loading**
- Load lexicons on-demand
- Defer plugin loading until first use
- Stream large lexicon files

**Optimization**
- Use Span<T> for string manipulation
- Pool StringBuilder instances
- Use ArrayPool for temporary buffers
- Compile regex patterns for sound changes

### Extensibility Points

**Custom Phonology Engines**
```csharp
public interface IPhonologyEngine
{
    // Allows custom phonological systems
    // e.g., tonal languages, click consonants
}
```

**Custom Grammar Engines**
```csharp
public interface IGrammarEngine
{
    // Allows custom syntactic systems
    // e.g., polysynthetic languages, ergative-absolutive
}
```

**Custom Name Generators**
```csharp
public interface INameGenerator
{
    // Allows custom generation strategies
    // e.g., Markov chains, neural networks
}
```

### Future Enhancements

**Markov Chain Generation**
- Train on sample texts to learn language patterns
- Generate more natural-sounding text
- Support style transfer between languages

**Neural Language Models**
- Use transformer models for translation
- Generate contextually appropriate text
- Learn from user corrections

**Phonetic Transcription**
- IPA (International Phonetic Alphabet) support
- Text-to-speech integration
- Pronunciation guides

**Advanced Morphology**
- Irregular verb conjugations
- Noun declensions
- Agreement rules (gender, number, case)

**Syntax Trees**
- Parse sentences into syntax trees
- Support complex sentence structures
- Enable grammatical analysis

**Language Families**
- Define proto-languages
- Automatically derive daughter languages
- Model historical language evolution

## Dependencies

### External Libraries

- **YamlDotNet**: YAML parsing for configuration files
- **System.Text.Json**: JSON serialization
- **FsCheck**: Property-based testing framework
- **Microsoft.Extensions.Logging**: Logging infrastructure
- **Microsoft.Extensions.Configuration**: Configuration management

### Internal Dependencies

- **PigeonPea.Contracts**: Core service contracts
- **PigeonPea.PluginSystem**: Plugin loading and management
- **PigeonPea.Game.Contracts**: Game-specific contracts

### Project Structure

```
dotnet/game-essential/core/src/
├─ PigeonPea.Language.Contracts/          # Tier 1: Interfaces
│  ├─ ILanguageService.cs
│  ├─ Phonology/
│  ├─ Lexicon/
│  ├─ Grammar/
│  └─ SoundChange/
│
├─ PigeonPea.Language.Core/               # Tier 3: Implementation
│  ├─ LanguageService.cs
│  ├─ PhonologyEngine.cs
│  ├─ LexiconManager.cs
│  ├─ GrammarEngine.cs
│  ├─ TranslationEngine.cs
│  ├─ NameGenerator.cs
│  ├─ SoundChangeEngine.cs
│  └─ LanguageDefinitionRepository.cs
│
└─ PigeonPea.Language.Tests/              # Tests
   ├─ Unit/
   ├─ Property/
   └─ Integration/

dotnet/game-essential/plugins/
└─ PigeonPea.Plugin.Language.Elvish/      # Example language plugin
   ├─ ElvishLanguagePlugin.cs
   ├─ configs/elvish.yaml
   └─ lexicons/elvish.json
```

## Migration and Deployment

### Phase 1: Core Infrastructure (Week 1)
- Create contract projects
- Implement phonology engine
- Implement lexicon manager
- Basic unit tests

### Phase 2: Grammar and Translation (Week 2)
- Implement grammar engine
- Implement translation engine
- Implement name generator
- Property-based tests

### Phase 3: Advanced Features (Week 3)
- Implement sound change engine
- Configuration file support
- Plugin integration
- Integration tests

### Phase 4: Sample Languages (Week 4)
- Create Elvish language definition
- Create Dwarvish language definition
- Create Draconic language definition
- Documentation and examples

### Rollout Strategy

**Development Environment**
- Deploy to dev environment first
- Test with sample languages
- Gather feedback from developers

**Staging Environment**
- Deploy with full test suite
- Performance testing
- Load testing with large lexicons

**Production Environment**
- Gradual rollout
- Monitor error rates
- Collect usage metrics

## Success Metrics

- All 42 correctness properties pass property-based tests
- 100% unit test coverage for core components
- Configuration files load without errors
- Translation accuracy > 95% for known words
- Name generation produces phonologically valid output 100% of the time
- Plugin system successfully loads all language plugins
- Hot-reload works without application restart
- Performance: < 10ms for name generation, < 100ms for sentence translation
