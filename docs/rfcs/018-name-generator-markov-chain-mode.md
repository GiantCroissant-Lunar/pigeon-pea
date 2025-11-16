---
doc_id: 'RFC-2025-00018'
title: 'Name Generator Markov Chain Mode'
doc_type: 'rfc'
status: 'draft'
canonical: true
created: '2025-11-15'
tags: ['name-generation', 'markov-chain', 'statistical', 'machine-learning', 'linguistic']
summary: 'Add Markov chain statistical name generation mode alongside existing rule-based system for more natural-sounding names learned from real examples'
supersedes: []
related: ['SPEC-2025-00014', 'RFC-2025-00016']
---

# RFC 018: Name Generator Markov Chain Mode

## Status
- **State:** Draft
- **Priority:** ⭐⭐⭐⭐ High
- **Estimated Effort:** 1 week
- **Dependencies:** Spec 014 (Name Generation System)
- **Target:** FantasyNameGenerator v2.1

## Problem Statement

The current FantasyNameGenerator uses a **rule-based phonological approach**:
- Phoneme inventories
- Phonotactic constraints
- Morphological rules
- Grammar templates

**Strengths:** Predictable, linguistically sound, configurable
**Weaknesses:** Can sound artificial, less natural variation

**Azgaar's Original** uses **Markov chains**:
- Learns from real name examples
- Statistical generation
- More natural-sounding output
- 30+ name bases (English, Japanese, Arabic, etc.)

**Strengths:** Natural-sounding, authentic patterns
**Weaknesses:** Requires training data, less predictable

### The Gap

Users want **both approaches**:
1. **Rule-based** for fantasy languages, constructed languages, precise control
2. **Markov-based** for authentic real-world names, historical accuracy

## Proposed Solution

Add **Markov Chain mode** as an **alternative generation strategy** alongside the existing rule-based system:

```csharp
public enum GenerationMode
{
    RuleBased,      // Current: Phoneme-based
    MarkovChain,    // NEW: Statistical learning
    Hybrid          // NEW: Combine both
}

public class NameGenerator
{
    public string Generate(NameType type, GenerationMode mode = GenerationMode.RuleBased)
    {
        return mode switch
        {
            GenerationMode.RuleBased => GenerateRuleBased(type),
            GenerationMode.MarkovChain => GenerateMarkovChain(type),
            GenerationMode.Hybrid => GenerateHybrid(type),
            _ => throw new ArgumentException(nameof(mode))
        };
    }
}
```

## Architecture

### Component Overview

```
┌──────────────────────────────────────────────────────────┐
│ NameGenerator (High-Level API)                          │
├──────────────────────────────────────────────────────────┤
│ ┌─────────────────┐  ┌──────────────────┐  ┌──────────┐ │
│ │ RuleBasedEngine │  │ MarkovChainEngine│  │HybridMode│ │
│ └─────────────────┘  └──────────────────┘  └──────────┘ │
├──────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────────────────┐ │
│ │ MarkovChainBuilder                                   │ │
│ │ - Train from corpus                                  │ │
│ │ - Build n-gram chains (order 2-4)                    │ │
│ │ - Calculate probabilities                            │ │
│ │ - Serialize/deserialize chains                       │ │
│ └──────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────────────────┐ │
│ │ NameCorpus                                           │ │
│ │ - Load name examples from files                      │ │
│ │ - Parse and validate names                           │ │
│ │ - Support multiple corpus types                      │ │
│ └──────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
```

### Data Structures

#### Markov Chain
```csharp
public class MarkovChain
{
    /// <summary>
    /// N-gram order (2 = bigram, 3 = trigram, etc.)
    /// </summary>
    public int Order { get; set; } = 2;

    /// <summary>
    /// Chain transitions: prefix → (next char, probability)
    /// </summary>
    public Dictionary<string, List<WeightedChar>> Transitions { get; set; } = new();

    /// <summary>
    /// Start prefixes (for beginning of word)
    /// </summary>
    public List<string> StartPrefixes { get; set; } = new();

    /// <summary>
    /// End characters (for detecting word end)
    /// </summary>
    public HashSet<char> EndCharacters { get; set; } = new();
}

public record WeightedChar(char Char, double Weight);
```

#### Name Corpus
```csharp
public class NameCorpus
{
    /// <summary>
    /// Corpus name/identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Source language/culture
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Example names for training
    /// </summary>
    public List<string> Examples { get; set; } = new();

    /// <summary>
    /// Minimum name length
    /// </summary>
    public int MinLength { get; set; } = 3;

    /// <summary>
    /// Maximum name length
    /// </summary>
    public int MaxLength { get; set; } = 12;

    /// <summary>
    /// Forbidden patterns (regex)
    /// </summary>
    public List<string> ForbiddenPatterns { get; set; } = new();
}
```

## Implementation Plan

### Phase 1: Core Markov Chain (Days 1-2)

#### Step 1.1: Markov Chain Builder
```csharp
public class MarkovChainBuilder
{
    private readonly int _order;

    public MarkovChainBuilder(int order = 2)
    {
        _order = order;
    }

    public MarkovChain BuildFromCorpus(NameCorpus corpus)
    {
        var chain = new MarkovChain { Order = _order };

        foreach (var name in corpus.Examples)
        {
            var normalized = NormalizeName(name);
            TrainOnName(chain, normalized);
        }

        CalculateProbabilities(chain);
        return chain;
    }

    private void TrainOnName(MarkovChain chain, string name)
    {
        // Add start-of-word marker
        var padded = new string('^', _order) + name.ToLower() + '$';

        for (int i = 0; i < padded.Length - _order; i++)
        {
            var prefix = padded.Substring(i, _order);
            var next = padded[i + _order];

            // Track start prefixes
            if (prefix.StartsWith("^"))
            {
                chain.StartPrefixes.Add(prefix);
            }

            // Track transitions
            if (!chain.Transitions.ContainsKey(prefix))
            {
                chain.Transitions[prefix] = new List<WeightedChar>();
            }

            var transitions = chain.Transitions[prefix];
            var existing = transitions.FirstOrDefault(t => t.Char == next);

            if (existing != null)
            {
                transitions.Remove(existing);
                transitions.Add(existing with { Weight = existing.Weight + 1 });
            }
            else
            {
                transitions.Add(new WeightedChar(next, 1));
            }

            // Track end characters
            if (next == '$')
            {
                chain.EndCharacters.Add(padded[i + _order - 1]);
            }
        }
    }

    private void CalculateProbabilities(MarkovChain chain)
    {
        foreach (var prefix in chain.Transitions.Keys.ToList())
        {
            var transitions = chain.Transitions[prefix];
            var total = transitions.Sum(t => t.Weight);

            chain.Transitions[prefix] = transitions
                .Select(t => t with { Weight = t.Weight / total })
                .OrderByDescending(t => t.Weight)
                .ToList();
        }
    }

    private string NormalizeName(string name)
    {
        // Remove accents, convert to lowercase
        return name
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c))
            .ToString()
            .ToLowerInvariant();
    }
}
```

#### Step 1.2: Markov Chain Generator
```csharp
public class MarkovChainEngine
{
    private readonly MarkovChain _chain;
    private readonly Random _random;

    public MarkovChainEngine(MarkovChain chain, Random random)
    {
        _chain = chain;
        _random = random;
    }

    public string Generate(int minLength = 3, int maxLength = 12, int maxAttempts = 1000)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var name = GenerateAttempt();

            if (name.Length >= minLength && name.Length <= maxLength)
            {
                return Capitalize(name);
            }
        }

        // Fallback: truncate or pad
        var fallback = GenerateAttempt();
        return Capitalize(fallback.Substring(0, Math.Min(fallback.Length, maxLength)));
    }

    private string GenerateAttempt()
    {
        var result = new StringBuilder();

        // Start with random start prefix
        var prefix = _chain.StartPrefixes[_random.Next(_chain.StartPrefixes.Count)];

        // Remove start markers and add initial characters
        var initial = prefix.TrimStart('^');
        result.Append(initial);

        // Build the name
        while (result.Length < 20) // Safety limit
        {
            if (!_chain.Transitions.TryGetValue(prefix, out var transitions))
                break;

            // Select next character based on probability
            var next = SelectWeightedRandom(transitions);

            if (next == '$')
                break; // End of word

            result.Append(next);

            // Update prefix (sliding window)
            prefix = prefix.Substring(1) + next;
        }

        return result.ToString();
    }

    private char SelectWeightedRandom(List<WeightedChar> transitions)
    {
        var roll = _random.NextDouble();
        var cumulative = 0.0;

        foreach (var transition in transitions)
        {
            cumulative += transition.Weight;
            if (roll <= cumulative)
                return transition.Char;
        }

        // Fallback
        return transitions.Last().Char;
    }

    private string Capitalize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return char.ToUpper(name[0]) + name.Substring(1);
    }
}
```

### Phase 2: Name Corpus System (Day 3)

#### Step 2.1: Corpus File Format

**JSON Format:**
```json
{
  "name": "english-places",
  "language": "English",
  "description": "English place names (towns, cities, regions)",
  "minLength": 4,
  "maxLength": 15,
  "forbiddenPatterns": ["xxx", "kkk"],
  "examples": [
    "London",
    "Manchester",
    "Birmingham",
    "Leeds",
    "Liverpool",
    "Bristol",
    "Sheffield",
    "Cambridge",
    "Oxford",
    "York",
    "Chester",
    "Winchester",
    "Stratford",
    "Nottingham",
    "Brighton"
  ]
}
```

**Text Format (Simple):**
```
# English Place Names
London
Manchester
Birmingham
Leeds
Liverpool
Bristol
...
```

#### Step 2.2: Corpus Loader
```csharp
public class CorpusLoader
{
    public NameCorpus LoadFromJson(string path)
    {
        var json = File.ReadAllText(path);
        var corpus = JsonSerializer.Deserialize<NameCorpus>(json);

        if (corpus == null)
            throw new InvalidDataException($"Failed to load corpus from {path}");

        ValidateCorpus(corpus);
        return corpus;
    }

    public NameCorpus LoadFromTextFile(string path, string name = "custom")
    {
        var lines = File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => !line.TrimStart().StartsWith("#"))
            .ToList();

        return new NameCorpus
        {
            Name = name,
            Examples = lines
        };
    }

    private void ValidateCorpus(NameCorpus corpus)
    {
        if (corpus.Examples.Count < 10)
            throw new ValidationException("Corpus must have at least 10 examples for training");

        if (corpus.MinLength < 1)
            throw new ValidationException("MinLength must be >= 1");

        if (corpus.MaxLength < corpus.MinLength)
            throw new ValidationException("MaxLength must be >= MinLength");
    }
}
```

### Phase 3: Built-in Corpus Data (Day 4)

Create corpus files for Azgaar's 30+ name bases:

**Data/Corpus/RealWorld/**
- `english.json` - English place names
- `german.json` - German place names
- `french.json` - French place names
- `spanish.json` - Spanish place names
- `italian.json` - Italian place names
- `russian.json` - Russian place names
- `japanese.json` - Japanese place names
- `chinese.json` - Chinese place names
- `arabic.json` - Arabic place names
- `turkish.json` - Turkish place names
- `greek.json` - Greek place names
- `norse.json` - Old Norse place names
- `celtic.json` - Celtic place names
- etc.

**Data/Corpus/Fantasy/**
- `tolkien-elvish.json` - Sindarin/Quenya inspired
- `tolkien-dwarvish.json` - Khuzdul inspired
- `lovecraft.json` - Lovecraftian names
- `dune.json` - Fremen-inspired
- etc.

### Phase 4: Integration & Hybrid Mode (Day 5)

#### Step 4.1: Generation Mode Selection
```csharp
public class NameGenerator
{
    private readonly RuleBasedEngine _ruleBased;
    private readonly Dictionary<string, MarkovChainEngine> _markovEngines = new();

    public string Generate(
        NameType type,
        string templateOrCorpus,
        GenerationMode mode = GenerationMode.RuleBased)
    {
        return mode switch
        {
            GenerationMode.RuleBased => _ruleBased.Generate(type, templateOrCorpus),
            GenerationMode.MarkovChain => GenerateMarkov(type, templateOrCorpus),
            GenerationMode.Hybrid => GenerateHybrid(type, templateOrCorpus),
            _ => throw new ArgumentException(nameof(mode))
        };
    }

    private string GenerateMarkov(NameType type, string corpusName)
    {
        if (!_markovEngines.TryGetValue(corpusName, out var engine))
        {
            throw new ArgumentException($"Unknown corpus: {corpusName}");
        }

        return engine.Generate();
    }

    private string GenerateHybrid(NameType type, string template)
    {
        // Use rule-based for structure, Markov for syllables
        var structure = _ruleBased.GenerateStructure(type, template);
        var syllables = GenerateMarkovSyllables(template, structure.SyllableCount);

        return CombineSyllables(syllables, structure);
    }

    public void LoadMarkovCorpus(string corpusName, string path)
    {
        var loader = new CorpusLoader();
        var corpus = loader.LoadFromJson(path);

        var builder = new MarkovChainBuilder(order: 2);
        var chain = builder.BuildFromCorpus(corpus);

        _markovEngines[corpusName] = new MarkovChainEngine(chain, _random);
    }
}
```

#### Step 4.2: Hybrid Mode Algorithm
```csharp
private string GenerateHybrid(NameType type, string template)
{
    // Get phonological template for constraints
    var phonology = PhonologyTemplates.GetPhonology(template);
    var phonotactics = PhonotacticTemplates.GetPhonotactics(template);

    // Get Markov chain for the same culture
    var markovEngine = _markovEngines.GetValueOrDefault(template);

    if (markovEngine == null)
    {
        // Fallback to rule-based if no Markov chain
        return _ruleBased.Generate(type, template);
    }

    // Generate with Markov
    var name = markovEngine.Generate();

    // Apply phonotactic constraints
    if (!phonotactics.IsValidName(name))
    {
        // Retry or fall back to rule-based
        return _ruleBased.Generate(type, template);
    }

    return name;
}
```

### Phase 5: Serialization & Caching (Day 6)

#### Pre-trained Chain Serialization
```csharp
public class MarkovChainSerializer
{
    public void SaveToFile(MarkovChain chain, string path)
    {
        var json = JsonSerializer.Serialize(chain, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }

    public MarkovChain LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var chain = JsonSerializer.Deserialize<MarkovChain>(json);

        if (chain == null)
            throw new InvalidDataException($"Failed to load chain from {path}");

        return chain;
    }
}
```

**Pre-trained Chains:**
- Ship pre-trained chains as embedded resources
- Avoid training on every startup
- Users can add custom chains

### Phase 6: Testing & Validation (Day 7)

#### Tests
```csharp
[Fact]
public void MarkovChain_TrainAndGenerate_ProducesValidNames()
{
    var corpus = new NameCorpus
    {
        Name = "test",
        Examples = new List<string>
        {
            "London", "Manchester", "Birmingham", "Liverpool", "Oxford"
        }
    };

    var builder = new MarkovChainBuilder(order: 2);
    var chain = builder.BuildFromCorpus(corpus);

    var engine = new MarkovChainEngine(chain, new Random(42));
    var names = new HashSet<string>();

    for (int i = 0; i < 100; i++)
    {
        var name = engine.Generate(minLength: 4, maxLength: 12);
        names.Add(name);

        Assert.NotEmpty(name);
        Assert.InRange(name.Length, 4, 12);
    }

    // Should generate variety
    Assert.True(names.Count > 50);
}

[Fact]
public void MarkovChain_Order3_ProducesMoreAccurateNames()
{
    var corpus = LoadRealWorldCorpus("english");

    var chain2 = new MarkovChainBuilder(2).BuildFromCorpus(corpus);
    var chain3 = new MarkovChainBuilder(3).BuildFromCorpus(corpus);

    // Order 3 should produce more realistic patterns
    var names2 = GenerateMany(chain2, 100);
    var names3 = GenerateMany(chain3, 100);

    var similarity2 = CalculateSimilarity(names2, corpus.Examples);
    var similarity3 = CalculateSimilarity(names3, corpus.Examples);

    Assert.True(similarity3 > similarity2);
}
```

## Configuration

```csharp
public class MarkovChainOptions
{
    /// <summary>
    /// N-gram order (2-4 recommended)
    /// </summary>
    public int Order { get; set; } = 2;

    /// <summary>
    /// Minimum training examples required
    /// </summary>
    public int MinExamples { get; set; } = 10;

    /// <summary>
    /// Default min name length
    /// </summary>
    public int MinLength { get; set; } = 3;

    /// <summary>
    /// Default max name length
    /// </summary>
    public int MaxLength { get; set; } = 12;

    /// <summary>
    /// Max generation attempts before fallback
    /// </summary>
    public int MaxAttempts { get; set; } = 1000;

    /// <summary>
    /// Path to custom corpus files
    /// </summary>
    public string? CustomCorpusPath { get; set; }

    /// <summary>
    /// Cache pre-trained chains
    /// </summary>
    public bool CacheChains { get; set; } = true;
}
```

## Usage Examples

### Basic Markov Generation
```csharp
var generator = new NameGenerator();

// Load corpus
generator.LoadMarkovCorpus("english", "Data/Corpus/RealWorld/english.json");

// Generate names
var name1 = generator.Generate(NameType.Burg, "english", GenerationMode.MarkovChain);
var name2 = generator.Generate(NameType.State, "english", GenerationMode.MarkovChain);

Console.WriteLine(name1); // "Manford" (sounds English)
Console.WriteLine(name2); // "Liverton" (sounds English)
```

### Hybrid Mode
```csharp
// Use phonological constraints with Markov generation
var name = generator.Generate(
    NameType.Burg,
    "germanic",
    GenerationMode.Hybrid
);

Console.WriteLine(name); // Follows Germanic phonotactics but uses Markov patterns
```

### Custom Corpus
```csharp
// Load custom names
var customCorpus = new NameCorpus
{
    Name = "my-world",
    Examples = new List<string> { "Azeroth", "Kalimdor", "Northrend", /* ... */ }
};

var builder = new MarkovChainBuilder(order: 3);
var chain = builder.BuildFromCorpus(customCorpus);

generator.LoadMarkovChain("my-world", chain);

var name = generator.Generate(NameType.Burg, "my-world", GenerationMode.MarkovChain);
```

## Comparison: Rule-Based vs Markov

| Aspect | Rule-Based | Markov Chain | Winner |
|--------|------------|--------------|--------|
| **Authenticity** | Synthetic | Learned from real names | Markov |
| **Predictability** | High | Medium | Rule-Based |
| **Configurability** | High | Low | Rule-Based |
| **Training Required** | No | Yes | Rule-Based |
| **Fantasy Languages** | Excellent | Poor | Rule-Based |
| **Real-World Names** | Good | Excellent | Markov |
| **Variety** | Good | Excellent | Markov |
| **Performance** | Fast | Medium | Rule-Based |

**Recommendation:** Use **both**:
- **Rule-based** for fantasy settings, constructed languages
- **Markov** for historical settings, real-world cultures
- **Hybrid** for best of both worlds

## Benefits

1. **Authentic Names**: Learn from real-world examples
2. **Flexibility**: Multiple generation strategies
3. **User Control**: Load custom training data
4. **Backward Compatible**: Existing rule-based system unchanged
5. **Educational**: Users can experiment with different approaches

## Future Enhancements

- **Neural Networks**: Use RNNs/LSTMs for even better names
- **Character-level Transformers**: GPT-style generation
- **Phoneme-level Markov**: Combine with phonology
- **Style Transfer**: Mix multiple corpus styles
- **Name Mutation**: Evolve names over generations

## Success Criteria

- [ ] Markov chain builder implemented
- [ ] Markov chain generator working
- [ ] At least 10 built-in corpus files
- [ ] Hybrid mode functional
- [ ] All tests passing
- [ ] Performance acceptable (<100ms per name)
- [ ] Documentation complete
- [ ] Backward compatibility maintained

## References

- **Azgaar's Implementation**: `ref-projects/Fantasy-Map-Generator/modules/names-generator.js`
- **Markov Chains**: https://en.wikipedia.org/wiki/Markov_chain
- **N-gram Models**: https://en.wikipedia.org/wiki/N-gram
- **Original Paper**: Shannon, Claude E. (1948). "A Mathematical Theory of Communication"

## Dependencies

- Spec 014: Name Generation System (base)
- RFC 016: JSON Configuration (for corpus files)
