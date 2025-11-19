# PigeonPea Language Contracts - API Documentation

This document provides comprehensive API documentation for the Fantasy Language Service contracts.

## Overview

The Language Contracts define the core interfaces and data models for the Fantasy Language Service. These contracts enable plugin-based language implementations and ensure consistent behavior across different language generators.

## Core Interfaces

### ILanguageService

The main service interface for managing fantasy languages.

```csharp
public interface ILanguageService
{
    // Language Management
    Task<bool> LoadLanguageAsync(string languageId, string configPath);
    Task<bool> UnloadLanguageAsync(string languageId);
    IReadOnlyList<string> GetLoadedLanguages();
    
    // Translation
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
    
    // Name Generation
    string GenerateName(string languageId, NameGenerationOptions options);
    IEnumerable<string> GenerateNames(string languageId, int count, NameGenerationOptions options);
    
    // Text Generation
    string GenerateSentence(string languageId, SentenceTemplate template);
    string GenerateParagraph(string languageId, int sentenceCount);
}
```

#### Methods

**LoadLanguageAsync**
```csharp
Task<bool> LoadLanguageAsync(string languageId, string configPath)
```
Loads a language definition from a configuration file.

- **Parameters:**
  - `languageId`: Unique identifier for the language
  - `configPath`: Path to the JSON/YAML configuration file
- **Returns:** `true` if successful, `false` otherwise
- **Example:**
```csharp
var success = await languageService.LoadLanguageAsync("high-elvish", "configs/high-elvish.json");
```

**TranslateAsync**
```csharp
Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
```
Translates text between languages.

- **Parameters:**
  - `text`: Text to translate
  - `sourceLanguage`: Source language ID (use "english" for English)
  - `targetLanguage`: Target language ID
- **Returns:** Translated text
- **Example:**
```csharp
var translated = await languageService.TranslateAsync("hello world", "english", "high-elvish");
```

**GenerateName**
```csharp
string GenerateName(string languageId, NameGenerationOptions options)
```
Generates a single name in the specified language.

- **Parameters:**
  - `languageId`: Language to use for generation
  - `options`: Name generation options (syllable count, type, seed)
- **Returns:** Generated name
- **Example:**
```csharp
var name = languageService.GenerateName("dwarvish", new NameGenerationOptions
{
    MinSyllables = 2,
    MaxSyllables = 4,
    Type = NameType.Personal
});
```

### IPhonologyEngine

Handles phonological rules and syllable generation.

```csharp
public interface IPhonologyEngine
{
    bool ValidatePhonemeInventory(PhonemeInventory inventory);
    bool ValidateSyllableTemplate(SyllableTemplate template, PhonemeInventory inventory);
    string GenerateSyllable(SyllableTemplate template, PhonemeInventory inventory, Random random);
    bool IsValidWord(string word, PhonologyRules rules);
}
```

### ILexiconManager

Manages word mappings between languages.

```csharp
public interface ILexiconManager
{
    void AddEntry(string languageId, LexiconEntry entry);
    LexiconEntry? LookupByMeaning(string languageId, string meaning);
    LexiconEntry? LookupByWord(string languageId, string word);
    IEnumerable<LexiconEntry> GetAllEntries(string languageId);
    Task SaveLexiconAsync(string languageId, string path);
    Task LoadLexiconAsync(string languageId, string path);
}
```

### IGrammarEngine

Applies grammatical rules and transformations.

```csharp
public interface IGrammarEngine
{
    bool ValidateGrammar(GrammarRules rules);
    string[] ApplyWordOrder(string[] words, WordOrder order);
    string ApplyMorphology(string root, MorphologyRule rule);
    string FormCompound(string[] roots, CompoundRule rule);
}
```

### ISoundChangeEngine

Handles language evolution through sound changes.

```csharp
public interface ISoundChangeEngine
{
    string ApplySoundChange(string word, SoundChangeRule rule);
    IEnumerable<string> ApplySoundChanges(IEnumerable<string> words, IEnumerable<SoundChangeRule> rules);
    LanguageDefinition DeriveLanguage(LanguageDefinition parent, IEnumerable<SoundChangeRule> soundChanges);
}
```

## Data Models

### LanguageDefinition

Complete definition of a fantasy language.

```csharp
public class LanguageDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public PhonologyRules Phonology { get; set; }
    public GrammarRules Grammar { get; set; }
    public string? ParentLanguageId { get; set; }
    public List<SoundChangeRule> SoundChanges { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### PhonemeInventory

Defines the sounds available in a language.

```csharp
public class PhonemeInventory
{
    public List<string> Vowels { get; set; }
    public List<string> Consonants { get; set; }
    public List<string> Diphthongs { get; set; }
}
```

### SyllableTemplate

Pattern for syllable construction.

```csharp
public class SyllableTemplate
{
    public string Pattern { get; set; }        // e.g., "CVC", "CV", "CCVC"
    public int Weight { get; set; }            // Probability weight
    public List<string> AllowedOnsets { get; set; }
    public List<string> AllowedCodas { get; set; }
}
```

### GrammarRules

Grammatical rules for a language.

```csharp
public class GrammarRules
{
    public WordOrder WordOrder { get; set; }
    public List<MorphologyRule> MorphologyRules { get; set; }
    public List<CompoundRule> CompoundRules { get; set; }
}
```

### WordOrder Enum

```csharp
public enum WordOrder
{
    SVO,  // Subject-Verb-Object (English)
    SOV,  // Subject-Object-Verb (Japanese)
    VSO,  // Verb-Subject-Object (Welsh)
    VOS,  // Verb-Object-Subject (Malagasy)
    OVS,  // Object-Verb-Subject (Hixkaryana)
    OSV   // Object-Subject-Verb (rare)
}
```

### NameGenerationOptions

Options for name generation.

```csharp
public record NameGenerationOptions
{
    public int MinSyllables { get; init; } = 2;
    public int MaxSyllables { get; init; } = 4;
    public NameType Type { get; init; } = NameType.Personal;
    public int? Seed { get; init; }
}

public enum NameType
{
    Personal,  // Character names
    Place,     // Location names
    Item,      // Object names
    Clan,      // Family/clan names
    Title      // Titles and honorifics
}
```

### SoundChangeRule

Rule for phonological evolution.

```csharp
public class SoundChangeRule
{
    public string Name { get; set; }
    public string Source { get; set; }      // Phoneme to change
    public string Target { get; set; }      // Result phoneme
    public string? Context { get; set; }    // Optional context (e.g., "_a" = before 'a')
}
```

## Common Usage Patterns

### Loading and Using a Language

```csharp
// Create service with dependencies
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var repository = new LanguageDefinitionRepository(loggerFactory.CreateLogger<LanguageDefinitionRepository>());
var phonologyEngine = new PhonologyEngine(loggerFactory.CreateLogger<PhonologyEngine>());
var lexiconManager = new LexiconManager(loggerFactory.CreateLogger<LexiconManager>());
var grammarEngine = new GrammarEngine(loggerFactory.CreateLogger<GrammarEngine>());
var soundChangeEngine = new SoundChangeEngine(loggerFactory.CreateLogger<SoundChangeEngine>());

var languageService = new LanguageService(
    repository,
    phonologyEngine,
    lexiconManager,
    grammarEngine,
    soundChangeEngine,
    loggerFactory);

// Load a language
await languageService.LoadLanguageAsync("high-elvish", "configs/high-elvish.json");

// Generate names
var names = languageService.GenerateNames("high-elvish", 5, new NameGenerationOptions
{
    MinSyllables = 2,
    MaxSyllables = 4,
    Type = NameType.Personal
});

// Translate text
var translated = await languageService.TranslateAsync("hello friend", "english", "high-elvish");
```

### Creating a Custom Language

```csharp
var language = new LanguageDefinition
{
    Id = "my-language",
    Name = "My Fantasy Language",
    Description = "A custom constructed language",
    Phonology = new PhonologyRules
    {
        Inventory = new PhonemeInventory
        {
            Vowels = new List<string> { "a", "e", "i", "o", "u" },
            Consonants = new List<string> { "p", "t", "k", "s", "m", "n" }
        },
        SyllableTemplates = new List<SyllableTemplate>
        {
            new() { Pattern = "CV", Weight = 50 },
            new() { Pattern = "CVC", Weight = 30 },
            new() { Pattern = "V", Weight = 20 }
        }
    },
    Grammar = new GrammarRules
    {
        WordOrder = WordOrder.SOV,
        MorphologyRules = new List<MorphologyRule>
        {
            new() { Name = "plural", Type = "suffix", Pattern = "{root}s" }
        }
    }
};
```

### Language Derivation with Sound Changes

```csharp
// Define sound changes
var soundChanges = new List<SoundChangeRule>
{
    new() { Name = "p_to_f", Source = "p", Target = "f", Context = null },
    new() { Name = "t_to_th", Source = "t", Target = "th", Context = "_a" }
};

// Derive a daughter language
var daughterLanguage = soundChangeEngine.DeriveLanguage(parentLanguage, soundChanges);
```

## Configuration File Format

Languages are defined in JSON format:

```json
{
  "id": "high-elvish",
  "name": "High Elvish",
  "description": "The ancient tongue of the Elven nobility",
  "phonology": {
    "inventory": {
      "vowels": ["a", "e", "i", "o", "u"],
      "consonants": ["l", "r", "n", "m", "s", "h", "t"],
      "diphthongs": ["ae", "ai", "ei"]
    },
    "syllableTemplates": [
      {
        "pattern": "CV",
        "weight": 40,
        "allowedOnsets": [],
        "allowedCodas": []
      }
    ]
  },
  "grammar": {
    "wordOrder": "svo",
    "morphologyRules": [
      {
        "name": "plural",
        "type": "suffix",
        "pattern": "{root}lir",
        "condition": "plural"
      }
    ]
  },
  "metadata": {
    "lexicon_path": "high-elvish-lexicon.json"
  }
}
```

## Error Handling

All async methods may throw:
- `ArgumentException`: Invalid parameters
- `InvalidOperationException`: Language not loaded or invalid state
- `FileNotFoundException`: Configuration file not found
- `JsonException`: Invalid JSON format

Always wrap calls in try-catch blocks:

```csharp
try
{
    await languageService.LoadLanguageAsync("my-lang", "config.json");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Config file not found: {ex.Message}");
}
catch (JsonException ex)
{
    Console.WriteLine($"Invalid JSON: {ex.Message}");
}
```

## Performance Considerations

- **Caching**: Language definitions are cached in memory after loading
- **Name Generation**: Use seeded Random for deterministic results
- **Batch Operations**: Use `GenerateNames()` instead of multiple `GenerateName()` calls
- **Lexicon Size**: Larger lexicons improve translation quality but increase memory usage

## Thread Safety

The service is designed to be thread-safe for read operations. However, avoid concurrent modifications to the same language definition.

## See Also

- [Language Creation Guide](../docs/language-creation-guide.md)
- [Plugin Development Guide](../docs/plugin-development-guide.md)
- [Example Application](../examples/PigeonPea.Language.Example/README.md)
