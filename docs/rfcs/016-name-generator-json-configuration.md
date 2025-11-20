---
canonical: true
created: '2025-11-15'
doc_id: RFC-00016
doc_type: rfc
related:
  - SPEC-00014
status: draft
summary: External JSON configuration for language templates to enable user-extensible
  name generation without code changes
supersedes: []
tags:
  - name-generation
  - json
  - configuration
  - extensibility
  - languages
title: Name Generator JSON Configuration System
---

# RFC 016: Name Generator JSON Configuration System

## Status

- **State:** Draft
- **Priority:** ⭐⭐⭐⭐ Critical
- **Estimated Effort:** 1 week
- **Dependencies:** Spec 014 (Name Generation System)
- **Target:** FantasyNameGenerator v2.0

## Problem Statement

The current FantasyNameGenerator has **hardcoded language templates** in C# code (`PhonologyTemplates.cs`, `PhonotacticTemplates.cs`). This creates several problems:

1. **Limited Extensibility**: Users cannot add new languages without modifying and recompiling code
2. **Spec Mismatch**: Spec 014 line 19 promises "JSON-Based Extensibility" but it's not implemented
3. **Developer Dependency**: Adding new language families requires C# knowledge
4. **Difficult Customization**: Users cannot tweak existing languages for their specific needs
5. **No Community Contribution**: Can't easily share language templates

### Current State

**6 Hardcoded Languages:**

- Germanic, Romance, Slavic (real-world inspired)
- Elvish, Dwarvish, Orcish (fantasy)

**Missing Language Families:**

- East Asian (Japanese, Chinese, Korean)
- Middle Eastern (Arabic, Persian, Turkish)
- African languages
- Celtic, Nordic, etc.

## Proposed Solution

Implement a **comprehensive JSON configuration system** that:

1. **Stores all language templates as JSON files** in `Data/Languages/`
2. **Loads templates at runtime** from embedded resources or file system
3. **Supports user-defined templates** from custom paths
4. **Validates JSON schema** to prevent errors
5. **Maintains backward compatibility** with existing code

## Architecture

### Directory Structure

```
src/FantasyNameGenerator/
├── Data/
│   └── Languages/
│       ├── schema.json                  ← JSON schema for validation
│       ├── RealWorld/
│       │   ├── germanic.json
│       │   ├── romance.json
│       │   ├── slavic.json
│       │   ├── celtic.json             ← NEW
│       │   ├── japanese.json           ← NEW
│       │   ├── chinese.json            ← NEW
│       │   ├── korean.json             ← NEW
│       │   ├── arabic.json             ← NEW
│       │   └── turkish.json            ← NEW
│       ├── Fantasy/
│       │   ├── elvish.json
│       │   ├── dwarvish.json
│       │   ├── orcish.json
│       │   ├── draconic.json           ← NEW
│       │   └── celestial.json          ← NEW
│       └── Custom/                      ← User-defined languages
│           └── README.md                ← Guide for users
├── Configuration/
│   ├── LanguageLoader.cs               ← JSON loading
│   ├── LanguageTemplate.cs             ← JSON model
│   └── LanguageValidator.cs            ← JSON validation
└── Templates/
    ├── PhonologyTemplates.cs           ← Convert to JSON loader
    └── PhonotacticTemplates.cs         ← Convert to JSON loader
```

### JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["name", "version", "phonology", "phonotactics"],
  "properties": {
    "name": {
      "type": "string",
      "description": "Language template name (lowercase)",
      "pattern": "^[a-z][a-z0-9_-]*$",
      "examples": ["germanic", "japanese", "elvish"]
    },
    "version": {
      "type": "string",
      "description": "Semantic version",
      "pattern": "^\\d+\\.\\d+\\.\\d+$",
      "default": "1.0.0"
    },
    "description": {
      "type": "string",
      "description": "Human-readable description"
    },
    "author": {
      "type": "string",
      "description": "Template creator"
    },
    "tags": {
      "type": "array",
      "items": { "type": "string" },
      "description": "Categorization tags",
      "examples": [
        ["real-world", "european"],
        ["fantasy", "tolkien-inspired"]
      ]
    },
    "phonology": {
      "type": "object",
      "required": ["consonants", "vowels"],
      "properties": {
        "consonants": {
          "type": "string",
          "description": "IPA consonant inventory",
          "pattern": "^[ptkbdgmnlrsfvʃʒθðhwjɲŋxɣʁχqɢʔ]+$",
          "examples": ["ptkbdgmnlrsfv"]
        },
        "vowels": {
          "type": "string",
          "description": "IPA vowel inventory",
          "pattern": "^[aeiouəɛɔæɑɪʊyøœɨʉ]+$",
          "examples": ["aeiou"]
        },
        "liquids": {
          "type": "string",
          "description": "Liquid consonants (l, r, w, y)",
          "default": "lr"
        },
        "nasals": {
          "type": "string",
          "description": "Nasal consonants (m, n, ŋ)",
          "default": "mn"
        },
        "fricatives": {
          "type": "string",
          "description": "Fricative consonants (f, v, s, z, etc.)",
          "default": "sfv"
        },
        "stops": {
          "type": "string",
          "description": "Stop consonants (p, t, k, b, d, g)",
          "default": "ptkbdg"
        },
        "sibilants": {
          "type": "string",
          "description": "Sibilant consonants (s, ʃ, ʒ)",
          "default": "sʃ"
        },
        "finals": {
          "type": "string",
          "description": "Syllable-final consonants",
          "default": "mn"
        },
        "allophones": {
          "type": "array",
          "description": "Contextual sound changes",
          "items": {
            "type": "object",
            "required": ["phoneme", "allophone", "context"],
            "properties": {
              "phoneme": { "type": "string", "pattern": "^.$" },
              "allophone": { "type": "string", "pattern": "^.$" },
              "context": { "type": "string", "description": "Regex pattern" }
            }
          }
        },
        "orthography": {
          "type": "object",
          "description": "IPA to written form mapping",
          "properties": {
            "consonants": {
              "type": "object",
              "patternProperties": {
                "^.$": { "type": "string" }
              },
              "examples": [{ "θ": "th", "ʃ": "sh", "ŋ": "ng" }]
            },
            "vowels": {
              "type": "object",
              "patternProperties": {
                "^.$": { "type": "string" }
              },
              "examples": [{ "ə": "e", "ɛ": "e" }]
            }
          }
        }
      }
    },
    "phonotactics": {
      "type": "object",
      "required": ["structures"],
      "properties": {
        "structures": {
          "type": "array",
          "description": "Allowed syllable structures",
          "items": {
            "type": "string",
            "pattern": "^[CVLSF?]+$",
            "examples": ["CV", "CVC", "CCVC", "CVC?"]
          },
          "minItems": 1
        },
        "forbiddenSequences": {
          "type": "array",
          "description": "Illegal phoneme combinations (regex)",
          "items": { "type": "string" },
          "examples": [["θθ", "ðð", "ww", "jj"]]
        },
        "allowedOnsets": {
          "type": "array",
          "description": "Legal syllable-initial clusters",
          "items": { "type": "string" },
          "examples": [["bl", "br", "pl", "pr", "tr", "dr"]]
        },
        "allowedCodas": {
          "type": "array",
          "description": "Legal syllable-final clusters",
          "items": { "type": "string" },
          "examples": [["st", "nd", "nt", "mp"]]
        },
        "maxConsonantCluster": {
          "type": "integer",
          "description": "Max consecutive consonants",
          "minimum": 1,
          "maximum": 5,
          "default": 2
        },
        "maxVowelCluster": {
          "type": "integer",
          "description": "Max consecutive vowels",
          "minimum": 1,
          "maximum": 3,
          "default": 2
        },
        "minSyllables": {
          "type": "integer",
          "description": "Min syllables per word",
          "minimum": 1,
          "default": 1
        },
        "maxSyllables": {
          "type": "integer",
          "description": "Max syllables per word",
          "minimum": 1,
          "maximum": 10,
          "default": 3
        },
        "enforceSonoritySequencing": {
          "type": "boolean",
          "description": "Enforce sonority hierarchy in clusters",
          "default": true
        }
      }
    },
    "morphology": {
      "type": "object",
      "description": "Word formation rules (optional)",
      "properties": {
        "prefixes": {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["form", "meaning"],
            "properties": {
              "form": { "type": "string" },
              "meaning": { "type": "string" },
              "frequency": { "type": "number", "minimum": 0, "maximum": 1, "default": 0.1 }
            }
          }
        },
        "suffixes": {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["form", "meaning"],
            "properties": {
              "form": { "type": "string" },
              "meaning": { "type": "string" },
              "frequency": { "type": "number", "minimum": 0, "maximum": 1, "default": 0.1 }
            }
          }
        },
        "compounding": {
          "type": "object",
          "properties": {
            "joiner": { "type": "string", "default": " " },
            "headFirst": { "type": "boolean", "default": true },
            "probability": { "type": "number", "minimum": 0, "maximum": 1, "default": 0.3 }
          }
        }
      }
    },
    "grammar": {
      "type": "object",
      "description": "Grammatical elements (optional)",
      "properties": {
        "wordOrder": {
          "type": "string",
          "enum": ["SVO", "SOV", "VSO", "VOS", "OSV", "OVS"],
          "description": "Subject-Verb-Object order",
          "default": "SVO"
        },
        "joiner": {
          "type": "string",
          "description": "Default word joiner",
          "default": " ",
          "examples": [" ", "-", ""]
        },
        "genitive": {
          "type": "string",
          "description": "Genitive marker ('of')",
          "examples": ["of", "de", "von", "no"]
        },
        "definiteArticle": {
          "type": "string",
          "description": "Definite article ('the')",
          "examples": ["the", "el", "la", "der", "die", "das"]
        }
      }
    },
    "exponent": {
      "type": "number",
      "description": "Phoneme selection bias (higher = prefer early phonemes)",
      "minimum": 0,
      "maximum": 5,
      "default": 1
    }
  }
}
```

## Implementation Plan

### Phase 1: JSON Infrastructure (Days 1-2)

#### Step 1.1: Create JSON Models

```csharp
namespace FantasyNameGenerator.Configuration;

public class LanguageTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();

    public PhonologyConfig Phonology { get; set; } = new();
    public PhonotacticsConfig Phonotactics { get; set; } = new();
    public MorphologyConfig? Morphology { get; set; }
    public GrammarConfig? Grammar { get; set; }

    public double Exponent { get; set; } = 1.0;
}

public class PhonologyConfig
{
    public string Consonants { get; set; } = string.Empty;
    public string Vowels { get; set; } = string.Empty;
    public string Liquids { get; set; } = "lr";
    public string Nasals { get; set; } = "mn";
    public string Fricatives { get; set; } = "sfv";
    public string Stops { get; set; } = "ptkbdg";
    public string Sibilants { get; set; } = "sʃ";
    public string Finals { get; set; } = "mn";

    public AllophoneConfig[] Allophones { get; set; } = Array.Empty<AllophoneConfig>();
    public OrthographyConfig? Orthography { get; set; }
}

public class PhonotacticsConfig
{
    public string[] Structures { get; set; } = Array.Empty<string>();
    public string[] ForbiddenSequences { get; set; } = Array.Empty<string>();
    public string[] AllowedOnsets { get; set; } = Array.Empty<string>();
    public string[] AllowedCodas { get; set; } = Array.Empty<string>();

    public int MaxConsonantCluster { get; set; } = 2;
    public int MaxVowelCluster { get; set; } = 2;
    public int MinSyllables { get; set; } = 1;
    public int MaxSyllables { get; set; } = 3;

    public bool EnforceSonoritySequencing { get; set; } = true;
}
```

#### Step 1.2: Create JSON Loader

```csharp
namespace FantasyNameGenerator.Configuration;

public class LanguageLoader
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public LanguageTemplate LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var template = JsonSerializer.Deserialize<LanguageTemplate>(json, _jsonOptions);

        if (template == null)
            throw new InvalidDataException($"Failed to load language template from {path}");

        ValidateTemplate(template);
        return template;
    }

    public LanguageTemplate LoadFromResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
            throw new FileNotFoundException($"Resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var template = JsonSerializer.Deserialize<LanguageTemplate>(json, _jsonOptions);

        if (template == null)
            throw new InvalidDataException($"Failed to load language template from resource {resourceName}");

        ValidateTemplate(template);
        return template;
    }

    public Dictionary<string, LanguageTemplate> LoadAllFromDirectory(string directory)
    {
        var templates = new Dictionary<string, LanguageTemplate>();

        foreach (var file in Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var template = LoadFromFile(file);
                templates[template.Name] = template;
            }
            catch (Exception ex)
            {
                // Log warning but continue
                Console.WriteLine($"Warning: Failed to load {file}: {ex.Message}");
            }
        }

        return templates;
    }

    private void ValidateTemplate(LanguageTemplate template)
    {
        if (string.IsNullOrWhiteSpace(template.Name))
            throw new ValidationException("Template must have a name");

        if (string.IsNullOrWhiteSpace(template.Phonology.Consonants))
            throw new ValidationException("Template must define consonants");

        if (string.IsNullOrWhiteSpace(template.Phonology.Vowels))
            throw new ValidationException("Template must define vowels");

        if (template.Phonotactics.Structures.Length == 0)
            throw new ValidationException("Template must define at least one syllable structure");

        // More validation...
    }
}
```

#### Step 1.3: Create Converter

```csharp
namespace FantasyNameGenerator.Configuration;

public static class LanguageTemplateConverter
{
    public static CulturePhonology ToPhonology(LanguageTemplate template)
    {
        var inventory = new PhonemeInventory
        {
            Consonants = template.Phonology.Consonants,
            Vowels = template.Phonology.Vowels,
            Liquids = template.Phonology.Liquids,
            Nasals = template.Phonology.Nasals,
            Fricatives = template.Phonology.Fricatives,
            Stops = template.Phonology.Stops,
            Sibilants = template.Phonology.Sibilants,
            Finals = template.Phonology.Finals
        };

        var allophones = template.Phonology.Allophones
            .Select(a => new AllophoneRule
            {
                Phoneme = a.Phoneme[0],
                Allophone = a.Allophone[0],
                Context = a.Context
            })
            .ToList();

        return new CulturePhonology(template.Name, inventory, allophones);
    }

    public static PhonotacticRules ToPhonotactics(LanguageTemplate template)
    {
        return new PhonotacticRules
        {
            AllowedStructures = template.Phonotactics.Structures.ToList(),
            ForbiddenSequences = template.Phonotactics.ForbiddenSequences.ToList(),
            AllowedOnsets = template.Phonotactics.AllowedOnsets.ToList(),
            AllowedCodas = template.Phonotactics.AllowedCodas.ToList(),
            MaxConsonantCluster = template.Phonotactics.MaxConsonantCluster,
            MaxVowelCluster = template.Phonotactics.MaxVowelCluster,
            EnforceSonoritySequencing = template.Phonotactics.EnforceSonoritySequencing
        };
    }
}
```

### Phase 2: Convert Existing Templates to JSON (Day 3)

#### Example: germanic.json

```json
{
  "name": "germanic",
  "version": "1.0.0",
  "description": "Germanic language family (English, German, Norse inspired)",
  "author": "FantasyMapGenerator",
  "tags": ["real-world", "european", "germanic"],

  "phonology": {
    "consonants": "ptkbdgmnlrsfvʃʒθðhw",
    "vowels": "aeiouəɛɔæɪʊ",
    "liquids": "lrw",
    "nasals": "mn",
    "fricatives": "sfvʃʒθð",
    "stops": "ptkbdg",
    "sibilants": "sʃʒ",
    "finals": "mnst",
    "orthography": {
      "consonants": {
        "θ": "th",
        "ð": "th",
        "ʃ": "sh",
        "ʒ": "zh",
        "ŋ": "ng"
      },
      "vowels": {
        "ə": "e",
        "ɛ": "e",
        "ɔ": "o",
        "æ": "ae",
        "ɪ": "i",
        "ʊ": "u"
      }
    }
  },

  "phonotactics": {
    "structures": ["CV", "CVC", "CCVC", "CVC?"],
    "forbiddenSequences": ["θθ", "ðð", "ŋŋ", "ww"],
    "allowedOnsets": [
      "bl",
      "br",
      "dr",
      "fl",
      "fr",
      "gl",
      "gr",
      "kl",
      "kr",
      "pl",
      "pr",
      "sl",
      "sp",
      "st",
      "sw",
      "tr",
      "tw",
      "ʃr"
    ],
    "allowedCodas": ["ft", "ks", "kt", "ld", "lt", "mp", "nd", "nt", "nk", "pt", "sk", "sp", "st"],
    "maxConsonantCluster": 3,
    "maxVowelCluster": 2,
    "minSyllables": 1,
    "maxSyllables": 3,
    "enforceSonoritySequencing": true
  },

  "morphology": {
    "suffixes": [
      { "form": "burg", "meaning": "fortress", "frequency": 0.3 },
      { "form": "heim", "meaning": "home", "frequency": 0.2 },
      { "form": "land", "meaning": "land", "frequency": 0.2 },
      { "form": "mark", "meaning": "border", "frequency": 0.1 }
    ],
    "compounding": {
      "joiner": "",
      "headFirst": false,
      "probability": 0.5
    }
  },

  "grammar": {
    "wordOrder": "SVO",
    "joiner": " ",
    "genitive": "of"
  },

  "exponent": 1.5
}
```

### Phase 3: Add New Language Templates (Days 4-5)

Create JSON files for missing language families:

- `japanese.json` (CJK)
- `chinese.json` (CJK)
- `korean.json` (CJK)
- `arabic.json` (Middle Eastern)
- `celtic.json` (European)
- `draconic.json` (Fantasy)

### Phase 4: Update Loading System (Days 6-7)

#### Step 4.1: Update PhonologyTemplates

```csharp
// OLD: Hardcoded
public static class PhonologyTemplates
{
    public static CulturePhonology Germanic => new(...);
    // ...
}

// NEW: JSON-based
public static class PhonologyTemplates
{
    private static readonly LanguageLoader _loader = new();
    private static readonly Dictionary<string, LanguageTemplate> _templates;

    static PhonologyTemplates()
    {
        _templates = LoadBuiltInTemplates();
    }

    private static Dictionary<string, LanguageTemplate> LoadBuiltInTemplates()
    {
        var templates = new Dictionary<string, LanguageTemplate>();

        // Load from embedded resources
        var resources = new[]
        {
            "FantasyNameGenerator.Data.Languages.RealWorld.germanic.json",
            "FantasyNameGenerator.Data.Languages.RealWorld.romance.json",
            // ... etc
        };

        foreach (var resource in resources)
        {
            var template = _loader.LoadFromResource(resource);
            templates[template.Name] = template;
        }

        return templates;
    }

    public static CulturePhonology GetPhonology(string templateName)
    {
        if (!_templates.TryGetValue(templateName, out var template))
            throw new ArgumentException($"Unknown template: {templateName}");

        return LanguageTemplateConverter.ToPhonology(template);
    }

    // Backward compatibility
    public static CulturePhonology Germanic => GetPhonology("germanic");
    public static CulturePhonology Romance => GetPhonology("romance");
    public static CulturePhonology Slavic => GetPhonology("slavic");
    public static CulturePhonology Elvish => GetPhonology("elvish");
    public static CulturePhonology Dwarvish => GetPhonology("dwarvish");
    public static CulturePhonology Orcish => GetPhonology("orcish");

    // NEW languages
    public static CulturePhonology Japanese => GetPhonology("japanese");
    public static CulturePhonology Chinese => GetPhonology("chinese");
    public static CulturePhonology Korean => GetPhonology("korean");
    public static CulturePhonology Arabic => GetPhonology("arabic");
    public static CulturePhonology Celtic => GetPhonology("celtic");
}
```

#### Step 4.2: Add User Template Support

```csharp
public class NameGeneratorOptions
{
    /// <summary>
    /// Path to custom language templates directory
    /// </summary>
    public string? CustomLanguagesPath { get; set; }

    /// <summary>
    /// Load custom templates on initialization
    /// </summary>
    public bool LoadCustomTemplates { get; set; } = true;
}

public class NameGenerator
{
    private readonly Dictionary<string, LanguageTemplate> _customTemplates = new();

    public NameGenerator(NameGeneratorOptions? options = null)
    {
        if (options?.LoadCustomTemplates == true && options.CustomLanguagesPath != null)
        {
            LoadCustomTemplates(options.CustomLanguagesPath);
        }
    }

    public void LoadCustomTemplates(string directory)
    {
        var loader = new LanguageLoader();
        var templates = loader.LoadAllFromDirectory(directory);

        foreach (var kvp in templates)
        {
            _customTemplates[kvp.Key] = kvp.Value;
        }
    }

    public LanguageTemplate GetTemplate(string name)
    {
        // Check custom first, then built-in
        if (_customTemplates.TryGetValue(name, out var custom))
            return custom;

        return PhonologyTemplates.GetTemplate(name);
    }
}
```

## Migration Path

### Backward Compatibility

All existing code continues to work:

```csharp
// OLD CODE (still works)
var phonology = PhonologyTemplates.Germanic;
var phonotactics = PhonotacticTemplates.Germanic;

// NEW CODE (same result)
var template = LanguageLoader.LoadFromResource("germanic.json");
var phonology = LanguageTemplateConverter.ToPhonology(template);
```

### Deprecation Plan

1. **v2.0**: Add JSON system alongside hardcoded templates
2. **v2.1**: Mark hardcoded templates as `[Obsolete]`
3. **v3.0**: Remove hardcoded templates entirely

## User Documentation

### Creating Custom Language Template

Create `my-language.json`:

```json
{
  "name": "my-language",
  "version": "1.0.0",
  "description": "My custom language",

  "phonology": {
    "consonants": "ptkmnls",
    "vowels": "aiu"
  },

  "phonotactics": {
    "structures": ["CV", "CVC"]
  }
}
```

Load it:

```csharp
var options = new NameGeneratorOptions
{
    CustomLanguagesPath = "path/to/custom/languages"
};

var generator = new NameGenerator(options);
var name = generator.Generate(NameType.Burg, "my-language");
```

## Testing Strategy

1. **Schema Validation Tests**: Ensure all JSON files validate
2. **Loading Tests**: Test file and resource loading
3. **Conversion Tests**: Verify JSON → C# model conversion
4. **Backward Compatibility Tests**: Ensure existing code still works
5. **Custom Template Tests**: Test user-defined templates
6. **Error Handling Tests**: Invalid JSON, missing files, etc.

## Success Criteria

- [x] All existing templates converted to JSON
- [x] JSON schema documented and validated
- [x] Custom templates can be loaded from file system
- [x] Backward compatibility maintained
- [x] At least 10 language templates available
- [x] User documentation complete
- [x] All tests passing

## Benefits

1. **Extensibility**: Users can add languages without code changes
2. **Community Contribution**: Easy to share language templates
3. **Customization**: Users can tweak existing templates
4. **Versioning**: Templates can evolve independently
5. **Documentation**: JSON is self-documenting with schema
6. **Validation**: JSON schema catches errors early

## Future Enhancements

- **Online Repository**: Community-contributed templates
- **Hot Reload**: Update templates without restart
- **Template Editor**: GUI for creating templates
- **Template Validation**: Advanced linguistic validation
- **Template Analytics**: Track template usage and quality

## References

- Spec 014: Name Generation System
- JSON Schema: http://json-schema.org/
- Conlang Resources: https://www.zompist.com/kit.html
