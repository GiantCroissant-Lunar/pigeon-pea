---
canonical: true
created: '2025-11-15'
doc_id: GUIDE-00001
doc_type: guide
related:
- RFC-00016
- RFC-00017
- RFC-00018
- SPEC-00014
status: active
summary: Step-by-step guide for implementing JSON configuration, CJK languages, and
  Markov chain mode for the FantasyNameGenerator
supersedes: []
tags:
- name-generation
- implementation
- guide
- json
- markov-chain
- cjk
title: Name Generator Enhancement Implementation Guide
---




# Name Generator Enhancement Implementation Guide

## Overview

This guide provides a **step-by-step roadmap** for implementing three major enhancements to the FantasyNameGenerator library:

1. **RFC-016: JSON Configuration System** - External language templates
2. **RFC-017: CJK Language Support** - Japanese, Chinese, Korean templates
3. **RFC-018: Markov Chain Mode** - Statistical name generation

These enhancements address the gaps identified in the current implementation and bring it to **100% feature parity** with the original JavaScript implementation while maintaining architectural superiority.

## Prerequisites

Before starting, ensure you have:

- ✅ Completed Spec 014 (Name Generation System)
- ✅ Existing 4-layer architecture working (Syllable, Phonology, Phonotactics, Morphology)
- ✅ Basic name generation functional
- ✅ Test suite in place

## Implementation Roadmap

### Recommended Order

**Phase 1:** JSON Configuration (RFC-016) - **Foundation**
**Phase 2:** CJK Languages (RFC-017) - **Builds on Phase 1**
**Phase 3:** Markov Chain (RFC-018) - **Alternative mode**

**Total Estimated Time:** 2-3 weeks

---

## Phase 1: JSON Configuration System (RFC-016)

**Duration:** 1 week
**Priority:** Critical (enables all other enhancements)

### Day 1: JSON Infrastructure

#### Tasks

1. Create `Configuration/` namespace
2. Implement `LanguageTemplate.cs` model
3. Implement `PhonologyConfig.cs`, `PhonotacticsConfig.cs`, etc.
4. Create JSON schema file (`Data/Languages/schema.json`)

#### Files to Create

```
src/FantasyNameGenerator/
├── Configuration/
│   ├── LanguageTemplate.cs          (JSON model)
│   ├── PhonologyConfig.cs
│   ├── PhonotacticsConfig.cs
│   ├── MorphologyConfig.cs
│   ├── GrammarConfig.cs
│   ├── AllophoneConfig.cs
│   └── OrthographyConfig.cs
└── Data/
    └── Languages/
        └── schema.json
```

#### Implementation Checklist

- [ ] Define `LanguageTemplate` class with all properties
- [ ] Create nested config classes (Phonology, Phonotactics, etc.)
- [ ] Add JSON attributes for serialization
- [ ] Write JSON schema for validation
- [ ] Add unit tests for models

#### Code Example

```csharp
namespace FantasyNameGenerator.Configuration;

public class LanguageTemplate
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("phonology")]
    public PhonologyConfig Phonology { get; set; } = new();

    [JsonPropertyName("phonotactics")]
    public PhonotacticsConfig Phonotactics { get; set; } = new();

    // ... more properties
}
```

### Day 2: JSON Loader & Validator

#### Tasks

1. Implement `LanguageLoader.cs`
2. Implement `LanguageValidator.cs`
3. Add support for embedded resources
4. Add support for file system loading

#### Files to Create

```
src/FantasyNameGenerator/Configuration/
├── LanguageLoader.cs
└── LanguageValidator.cs
```

#### Implementation Checklist

- [ ] `LoadFromFile(string path)` method
- [ ] `LoadFromResource(string resourceName)` method
- [ ] `LoadAllFromDirectory(string dir)` method
- [ ] JSON deserialization with error handling
- [ ] Schema validation
- [ ] Template validation (required fields, valid values)
- [ ] Unit tests for loading and validation

### Day 3: Converter & Integration

#### Tasks

1. Implement `LanguageTemplateConverter.cs`
2. Convert JSON models to existing C# models
3. Update `PhonologyTemplates.cs` to load from JSON
4. Update `PhonotacticTemplates.cs` to load from JSON

#### Files to Modify

```
src/FantasyNameGenerator/
├── Configuration/
│   └── LanguageTemplateConverter.cs  (NEW)
└── Templates/
    ├── PhonologyTemplates.cs         (UPDATE)
    └── PhonotacticTemplates.cs       (UPDATE)
```

#### Implementation Checklist

- [ ] `ToPhonology(LanguageTemplate)` converter
- [ ] `ToPhonotactics(LanguageTemplate)` converter
- [ ] `ToMorphology(LanguageTemplate)` converter
- [ ] Update `PhonologyTemplates` to use loader
- [ ] Maintain backward compatibility
- [ ] Add integration tests

#### Code Example

```csharp
public static class LanguageTemplateConverter
{
    public static CulturePhonology ToPhonology(LanguageTemplate template)
    {
        var inventory = new PhonemeInventory
        {
            Consonants = template.Phonology.Consonants,
            Vowels = template.Phonology.Vowels,
            // ... map all fields
        };

        return new CulturePhonology(template.Name, inventory);
    }
}
```

### Days 4-5: Convert Existing Templates to JSON

#### Tasks

1. Create JSON files for all 6 existing languages
2. Embed as resources in project
3. Test loading and generation

#### Files to Create

```
src/FantasyNameGenerator/Data/Languages/
├── RealWorld/
│   ├── germanic.json
│   ├── romance.json
│   └── slavic.json
└── Fantasy/
    ├── elvish.json
    ├── dwarvish.json
    └── orcish.json
```

#### Implementation Checklist

- [ ] Create `germanic.json` from existing template
- [ ] Create `romance.json` from existing template
- [ ] Create `slavic.json` from existing template
- [ ] Create `elvish.json` from existing template
- [ ] Create `dwarvish.json` from existing template
- [ ] Create `orcish.json` from existing template
- [ ] Set as embedded resources in `.csproj`
- [ ] Verify all templates load correctly
- [ ] Verify generated names match old output

#### Template Conversion Process

1. Review existing `PhonologyTemplates.Germanic` code
2. Extract all phoneme inventories
3. Extract phonotactic rules
4. Extract morphology rules
5. Create JSON file following schema
6. Validate against schema
7. Test generation

### Days 6-7: User Custom Templates & Documentation

#### Tasks

1. Add custom template loading support
2. Write user documentation
3. Create example custom template
4. Write troubleshooting guide

#### Files to Create

```
src/FantasyNameGenerator/Data/Languages/Custom/README.md
docs/guides/creating-custom-language-templates.md
```

#### Implementation Checklist

- [ ] `NameGeneratorOptions.CustomLanguagesPath`
- [ ] `NameGenerator.LoadCustomTemplates(path)`
- [ ] Precedence: custom > built-in
- [ ] User documentation with examples
- [ ] Example custom template
- [ ] Troubleshooting common errors
- [ ] API documentation

---

## Phase 2: CJK Language Support (RFC-017)

**Duration:** 3-4 days
**Priority:** High
**Depends on:** Phase 1 (JSON Configuration)

### Day 1: Japanese Template

#### Tasks

1. Research Japanese phonology
2. Create `japanese.json` template
3. Implement allophonic rules
4. Test generation

#### Implementation Checklist

- [ ] Define phoneme inventory (consonants, vowels)
- [ ] Define syllable structures (CV, V, CVN)
- [ ] Implement allophonic rules (ti→chi, si→shi, etc.)
- [ ] Define morphology (yama, kawa, mura, etc.)
- [ ] Create JSON file
- [ ] Add to embedded resources
- [ ] Test with 1000+ generations
- [ ] Validate against real Japanese names

#### Key Features

```json
{
  "name": "japanese",
  "phonology": {
    "consonants": "ptkbdgmnszhrwj",
    "vowels": "aiueo",
    "allophones": [
      { "phoneme": "t", "allophone": "ʧ", "context": "i" },
      { "phoneme": "s", "allophone": "ʃ", "context": "i" }
    ]
  },
  "phonotactics": {
    "structures": ["CV", "V", "CVN"],
    "forbiddenSequences": ["ti", "di", "tu", "du", "si"]
  }
}
```

### Day 2: Chinese Template

#### Tasks

1. Research Mandarin phonology
2. Create `chinese.json` template
3. Implement Pinyin-style romanization
4. Test generation

#### Implementation Checklist

- [ ] Define phoneme inventory (initials, finals)
- [ ] Define syllable structures
- [ ] Define orthography (Pinyin romanization)
- [ ] Define morphology (shān, hé, chéng, etc.)
- [ ] Create JSON file
- [ ] Test generation
- [ ] Validate against real Chinese names

### Day 3: Korean Template

#### Tasks

1. Research Korean phonology
2. Create `korean.json` template
3. Implement Revised Romanization
4. Test generation

#### Implementation Checklist

- [ ] Define phoneme inventory
- [ ] Define syllable structures (CVC)
- [ ] Define orthography (Revised Romanization)
- [ ] Define morphology (san, gang, do, etc.)
- [ ] Create JSON file
- [ ] Test generation
- [ ] Validate against real Korean names

### Day 4: Integration & Testing

#### Tasks

1. Add CJK templates to `PhonologyTemplates`
2. Add CJK culture types
3. Integration testing
4. Documentation

#### Implementation Checklist

- [ ] Update `PhonologyTemplates` with static properties
- [ ] Add `CultureType.Japanese`, `.Chinese`, `.Korean`
- [ ] Update `NameGeneratorFactory` mappings
- [ ] Generate 1000+ names per language
- [ ] Compare with real place names
- [ ] Statistical analysis (syllable frequency, etc.)
- [ ] User documentation
- [ ] Example outputs

---

## Phase 3: Markov Chain Mode (RFC-018)

**Duration:** 1 week
**Priority:** High
**Depends on:** Phase 1 (for corpus JSON files)

### Day 1: Core Markov Chain

#### Tasks

1. Implement `MarkovChain` data structure
2. Implement `MarkovChainBuilder`
3. Unit tests for chain building

#### Files to Create

```
src/FantasyNameGenerator/
├── Markov/
│   ├── MarkovChain.cs
│   ├── MarkovChainBuilder.cs
│   ├── MarkovChainEngine.cs
│   └── WeightedChar.cs
```

#### Implementation Checklist

- [ ] `MarkovChain` class (transitions, start prefixes, etc.)
- [ ] `MarkovChainBuilder.BuildFromCorpus()`
- [ ] N-gram extraction (order 2-4)
- [ ] Probability calculation
- [ ] Unit tests for chain building
- [ ] Test with small corpus

#### Code Example

```csharp
public class MarkovChain
{
    public int Order { get; set; } = 2;
    public Dictionary<string, List<WeightedChar>> Transitions { get; set; } = new();
    public List<string> StartPrefixes { get; set; } = new();
}
```

### Day 2: Markov Chain Generator

#### Tasks

1. Implement `MarkovChainEngine`
2. Name generation algorithm
3. Unit tests for generation

#### Implementation Checklist

- [ ] `MarkovChainEngine.Generate()` method
- [ ] Weighted random selection
- [ ] Length constraints
- [ ] Retry logic
- [ ] Capitalization
- [ ] Unit tests for generation
- [ ] Test with real data

### Day 3: Corpus System

#### Tasks

1. Implement `NameCorpus` model
2. Implement `CorpusLoader`
3. Create corpus JSON schema
4. Create 3-5 initial corpus files

#### Files to Create

```
src/FantasyNameGenerator/
├── Corpus/
│   ├── NameCorpus.cs
│   └── CorpusLoader.cs
└── Data/
    └── Corpus/
        ├── RealWorld/
        │   ├── english.json
        │   ├── german.json
        │   └── japanese.json
        └── Fantasy/
            └── tolkien-elvish.json
```

#### Implementation Checklist

- [ ] `NameCorpus` class
- [ ] Corpus JSON schema
- [ ] `CorpusLoader.LoadFromJson()`
- [ ] `CorpusLoader.LoadFromTextFile()`
- [ ] Create corpus files with 50-100 examples each
- [ ] Validation logic
- [ ] Unit tests

### Day 4: Integration with NameGenerator

#### Tasks

1. Add `GenerationMode` enum
2. Update `NameGenerator` API
3. Markov chain caching
4. Integration tests

#### Implementation Checklist

- [ ] `GenerationMode` enum (RuleBased, MarkovChain, Hybrid)
- [ ] Update `NameGenerator.Generate()` signature
- [ ] `LoadMarkovCorpus()` method
- [ ] Chain caching/serialization
- [ ] Integration tests
- [ ] Performance tests

### Day 5: Hybrid Mode

#### Tasks

1. Implement hybrid generation
2. Combine rule-based constraints with Markov
3. Testing

#### Implementation Checklist

- [ ] `GenerateHybrid()` method
- [ ] Apply phonotactic validation to Markov output
- [ ] Fallback logic
- [ ] A/B testing (rule vs markov vs hybrid)
- [ ] Quality comparison

### Days 6-7: Corpus Building & Documentation

#### Tasks

1. Create 10-15 corpus files
2. Pre-train Markov chains
3. Serialize chains as embedded resources
4. Documentation

#### Implementation Checklist

- [ ] Create corpus files for major languages
- [ ] Pre-train chains (order 2 and 3)
- [ ] Serialize to JSON
- [ ] Embed in project
- [ ] Benchmarking
- [ ] User documentation
- [ ] API documentation
- [ ] Usage examples

---

## Testing Strategy

### Unit Tests (Per Phase)

**Phase 1:**

- [ ] JSON deserialization
- [ ] Schema validation
- [ ] Template validation
- [ ] Converter functionality
- [ ] Resource loading

**Phase 2:**

- [ ] CJK phoneme generation
- [ ] Allophonic rules
- [ ] Syllable structures
- [ ] Name quality (no forbidden sequences)

**Phase 3:**

- [ ] Markov chain building
- [ ] N-gram extraction
- [ ] Probability calculation
- [ ] Name generation
- [ ] Length constraints

### Integration Tests

- [ ] JSON → C# model → Name generation
- [ ] Custom template loading
- [ ] CJK name generation end-to-end
- [ ] Markov generation end-to-end
- [ ] Hybrid mode functionality

### Quality Tests

- [ ] Generated names are pronounceable
- [ ] No duplicate names in 1000 generations
- [ ] Cultural authenticity (compare with real names)
- [ ] Performance (<100ms per name)
- [ ] Backward compatibility (existing tests still pass)

### Statistical Tests

- [ ] Syllable frequency distribution
- [ ] Phoneme frequency distribution
- [ ] Name length distribution
- [ ] Uniqueness ratio
- [ ] Similarity to corpus (for Markov)

---

## Performance Considerations

### Optimization Targets

- JSON loading: <50ms per template
- Markov chain building: <500ms per corpus
- Name generation: <10ms (rule-based), <20ms (Markov)
- Memory: <10MB for all templates and chains

### Caching Strategy

1. **Template Caching**: Load JSON once, reuse forever
2. **Chain Caching**: Pre-train chains, serialize, embed
3. **Name Caching**: Cache recent names per language (optional)

---

## Migration & Compatibility

### Backward Compatibility

**CRITICAL:** All existing code must continue to work!

```csharp
// OLD CODE (still works)
var name = nameGenerator.Generate(NameType.Burg, "germanic");

// NEW CODE (same result)
var name = nameGenerator.Generate(
    NameType.Burg,
    "germanic",
    GenerationMode.RuleBased
);
```

### Deprecation Plan

1. **v2.0**: Add JSON system, mark hardcoded templates as `[Obsolete]`
2. **v2.1**: Add Markov mode
3. **v2.2**: Add CJK languages
4. **v3.0**: Remove hardcoded templates (breaking change)

---

## Documentation Deliverables

### User Documentation

1. **README.md updates**
   - JSON configuration section
   - CJK languages section
   - Markov mode section
   - Migration guide

2. **API Documentation**
   - XML docs on all public members
   - Usage examples
   - Configuration options

3. **Guides**
   - Creating custom language templates
   - Building custom corpus files
   - Choosing generation mode
   - Troubleshooting

### Developer Documentation

1. **Architecture Documentation**
   - Layer descriptions
   - Data flow diagrams
   - Class diagrams

2. **Implementation Notes**
   - Design decisions
   - Performance optimizations
   - Known limitations

---

## Success Criteria

### Phase 1 (JSON Configuration)

- [x] All 6 existing templates converted to JSON
- [x] Templates load from embedded resources
- [x] Custom templates load from file system
- [x] Backward compatibility maintained
- [x] All tests passing

### Phase 2 (CJK Languages)

- [x] 3 CJK templates created (Japanese, Chinese, Korean)
- [x] Names sound authentic
- [x] Phonological rules correct
- [x] Romanization working
- [x] All tests passing

### Phase 3 (Markov Chain)

- [x] Markov engine implemented
- [x] At least 10 corpus files created
- [x] Hybrid mode working
- [x] Performance acceptable
- [x] All tests passing

---

## Troubleshooting Guide

### Common Issues

**JSON not loading:**

- Check file path
- Verify JSON syntax (use online validator)
- Check schema validation errors

**Names don't sound right:**

- Review phoneme inventory
- Check phonotactic constraints
- Adjust syllable structures
- Increase corpus size (for Markov)

**Performance issues:**

- Enable chain caching
- Reduce Markov chain order
- Pre-train chains offline
- Profile hot paths

---

## Next Steps After Completion

### Future Enhancements

1. **More Languages**: Arabic, Celtic, Nordic, African
2. **Neural Networks**: RNN/LSTM generation
3. **Writing Systems**: Output in native scripts (Kanji, Hangul, etc.)
4. **Tonal Languages**: Support for tones in Chinese, Thai
5. **Historical Evolution**: Names change over time
6. **Web API**: Name generation as a service

---

## Resources

### Reference Implementations

- Azgaar's `names-generator.js`
- Choochoo's `LanguageGenerator.cs`

### Linguistic Resources

- IPA Chart: https://www.internationalphoneticassociation.org/
- Conlang Resources: https://www.zompist.com/kit.html
- Phonology Databases: https://phoible.org/

### Libraries

- System.Text.Json for JSON
- Regex for phonotactic validation
- LINQ for data processing

---

## Conclusion

This implementation guide provides a clear roadmap for enhancing the FantasyNameGenerator with:

- **JSON Configuration** for extensibility
- **CJK Languages** for cultural diversity
- **Markov Chain Mode** for authentic names

Following this guide, an AI agent or developer can implement all three enhancements in **2-3 weeks** with confidence that the result will be:

- ✅ Architecturally sound
- ✅ Well-tested
- ✅ Backward compatible
- ✅ Production-ready
- ✅ Feature-complete

Good luck with the implementation!
