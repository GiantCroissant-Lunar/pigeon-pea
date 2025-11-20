# Fantasy Language Creation Guide

This guide will walk you through creating your own fantasy language for the PigeonPea game engine.

## Table of Contents

1. [Introduction](#introduction)
2. [Phonology Design](#phonology-design)
3. [Grammar Design](#grammar-design)
4. [Lexicon Creation](#lexicon-creation)
5. [Sound Changes and Evolution](#sound-changes-and-evolution)
6. [Testing Your Language](#testing-your-language)
7. [Best Practices](#best-practices)

## Introduction

Creating a believable fantasy language involves three main components:

1. **Phonology**: The sounds and sound patterns of your language
2. **Grammar**: The rules for combining words into sentences
3. **Lexicon**: The vocabulary mapping meanings to words

This guide will help you design each component to create a cohesive, authentic-sounding language.

## Phonology Design

Phonology defines what your language sounds like. Start by choosing your phoneme inventory.

### Step 1: Choose Your Vowels

Select 3-7 vowels for your language. Common choices:

**Minimal (3-5 vowels):**

```json
"vowels": ["a", "i", "u"]
```

**Standard (5-7 vowels):**

```json
"vowels": ["a", "e", "i", "o", "u"]
```

**Extended (with length or quality distinctions):**

```json
"vowels": ["a", "e", "i", "o", "u", "á", "é", "í", "ó", "ú"]
```

### Step 2: Choose Your Consonants

Select 10-25 consonants. Consider the aesthetic you want:

**Soft/Flowing (Elvish-style):**

```json
"consonants": ["l", "r", "n", "m", "s", "h", "t", "th", "d", "v", "f"]
```

**Harsh/Guttural (Dwarvish-style):**

```json
"consonants": ["k", "g", "kh", "gh", "r", "rr", "z", "zh", "d", "t", "b", "p"]
```

**Sibilant/Hissing (Draconic-style):**

```json
"consonants": ["s", "z", "sh", "zh", "th", "dh", "k", "g", "r", "h"]
```

### Step 3: Define Syllable Templates

Syllable templates control the structure of words. Common patterns:

```json
"syllableTemplates": [
  {
    "pattern": "CV",      // Consonant-Vowel (simple, flowing)
    "weight": 40
  },
  {
    "pattern": "CVC",     // Consonant-Vowel-Consonant (balanced)
    "weight": 30
  },
  {
    "pattern": "V",       // Vowel only (rare, adds variety)
    "weight": 10
  },
  {
    "pattern": "CCV",     // Consonant cluster start
    "weight": 15,
    "allowedOnsets": ["th", "sh", "kr", "tr"]
  },
  {
    "pattern": "CVCC",    // Consonant cluster end
    "weight": 5,
    "allowedCodas": ["st", "nd", "nt"]
  }
]
```

**Weight** determines how often each pattern appears. Higher weights = more common.

### Step 4: Define Consonant Clusters

Specify which consonant combinations are allowed:

```json
"clusters": {
  "initialClusters": ["th", "sh", "kr", "tr", "pl", "br"],
  "medialClusters": ["nt", "nd", "st", "ld", "rv"],
  "finalClusters": ["st", "nt", "nd", "rn"]
}
```

### Phonology Examples

**Elvish (Flowing, Vowel-Rich):**

- Many vowels, soft consonants
- Simple syllable structure (CV, CVC)
- Liquid consonants (l, r) common
- Few consonant clusters

**Dwarvish (Harsh, Consonant-Heavy):**

- Fewer vowels, many consonants
- Complex syllable structure (CCVC, CVCC)
- Guttural sounds (kh, gh)
- Many consonant clusters

**Draconic (Ancient, Booming):**

- Long vowels, sibilants
- Varied syllable structure
- Emphasis on resonant sounds
- Compound-friendly

## Grammar Design

Grammar defines how words combine into sentences.

### Step 1: Choose Word Order

Select one of six basic word orders:

```json
"wordOrder": "svo"  // Subject-Verb-Object (like English)
```

Options:

- **SVO**: Subject-Verb-Object (English, Chinese) - "The cat eats fish"
- **SOV**: Subject-Object-Verb (Japanese, Turkish) - "The cat fish eats"
- **VSO**: Verb-Subject-Object (Welsh, Irish) - "Eats the cat fish"
- **VOS**: Verb-Object-Subject (Malagasy) - "Eats fish the cat"
- **OVS**: Object-Verb-Subject (Hixkaryana) - "Fish eats the cat"
- **OSV**: Object-Subject-Verb (rare) - "Fish the cat eats"

### Step 2: Define Morphology Rules

Morphology rules modify words for grammar (plural, tense, case, etc.):

```json
"morphologyRules": [
  {
    "name": "plural",
    "type": "suffix",
    "pattern": "{root}s",
    "condition": "plural"
  },
  {
    "name": "past_tense",
    "type": "suffix",
    "pattern": "{root}ed",
    "condition": "past"
  },
  {
    "name": "genitive",
    "type": "suffix",
    "pattern": "{root}en",
    "condition": "genitive"
  },
  {
    "name": "agent_noun",
    "type": "suffix",
    "pattern": "{root}ar",
    "condition": "agent"
  }
]
```

**Types:**

- `suffix`: Add to end (most common)
- `prefix`: Add to beginning
- `infix`: Insert in middle
- `circumfix`: Add to both ends

### Step 3: Define Compound Rules

Rules for combining words:

```json
"compoundRules": [
  {
    "pattern": "{root1}{root2}",
    "connector": null,
    "type": "noun_noun"
  },
  {
    "pattern": "{root1}a{root2}",
    "connector": "a",
    "type": "adjective_noun"
  }
]
```

## Lexicon Creation

The lexicon maps English words to your fantasy language.

### Creating a Lexicon File

Create a JSON file (e.g., `my-language-lexicon.json`):

```json
{
  "entries": [
    {
      "word": "aelor",
      "meaning": "hello",
      "partOfSpeech": "interjection",
      "root": "ael",
      "etymology": "from 'ael' (light) + 'or' (greeting)"
    },
    {
      "word": "mellon",
      "meaning": "friend",
      "partOfSpeech": "noun",
      "root": "mell",
      "etymology": "from 'mell' (companion)"
    },
    {
      "word": "thalor",
      "meaning": "mountain",
      "partOfSpeech": "noun",
      "root": "thal",
      "etymology": "from 'thal' (stone) + 'or' (great)"
    }
  ]
}
```

### Lexicon Best Practices

1. **Start Small**: Begin with 50-100 common words
2. **Use Roots**: Create word families from common roots
3. **Be Consistent**: Similar meanings should have similar sounds
4. **Add Etymology**: Track word origins for consistency

### Common Word Categories

Essential vocabulary to include:

- **Greetings**: hello, goodbye, thank you
- **Pronouns**: I, you, he, she, it, we, they
- **Common Nouns**: person, place, thing, time, way
- **Common Verbs**: be, have, do, say, go, get, make, know
- **Adjectives**: good, bad, big, small, new, old
- **Numbers**: one, two, three, many, few
- **Nature**: sun, moon, star, water, fire, earth, wind
- **Fantasy Terms**: magic, dragon, sword, quest, hero

## Sound Changes and Evolution

Create language variants through sound changes.

### Defining Sound Changes

```json
"soundChanges": [
  {
    "name": "p_to_f",
    "source": "p",
    "target": "f",
    "context": null
  },
  {
    "name": "t_to_th_before_a",
    "source": "t",
    "target": "th",
    "context": "_a"
  },
  {
    "name": "vowel_lengthening",
    "source": "a",
    "target": "á",
    "context": "_#"
  }
]
```

**Context Notation:**

- `_a`: Before 'a'
- `a_`: After 'a'
- `_#`: At end of word
- `#_`: At start of word
- `null`: Anywhere

### Creating Language Families

1. Create a parent language (e.g., "Ancient Elvish")
2. Define sound changes
3. Derive daughter languages (e.g., "High Elvish", "Wood Elvish")

```json
{
  "id": "high-elvish",
  "name": "High Elvish",
  "parentLanguageId": "ancient-elvish",
  "soundChanges": [
    { "name": "p_to_f", "source": "p", "target": "f" },
    { "name": "k_to_h", "source": "k", "target": "h" }
  ]
}
```

## Testing Your Language

### Using the Example Application

```bash
cd examples/PigeonPea.Language.Example
dotnet run

> load path/to/your-language.json
> generate-name your-language 10
> translate your-language hello world
```

### Validation Checklist

- [ ] All phonemes in syllable templates exist in inventory
- [ ] Syllable template weights sum to reasonable distribution
- [ ] Word order is specified
- [ ] At least one syllable template defined
- [ ] Lexicon file path is correct (if specified)
- [ ] Sound changes reference valid phonemes

### Quality Checks

1. **Generate 20+ names**: Do they sound consistent?
2. **Check syllable distribution**: Are patterns varied but coherent?
3. **Test translations**: Do words follow phonological rules?
4. **Verify morphology**: Do affixes apply correctly?

## Best Practices

### Do's

✅ **Start Simple**: Begin with basic phonology and expand
✅ **Be Consistent**: Maintain phonological and grammatical patterns
✅ **Use Real Languages**: Study natural languages for inspiration
✅ **Test Frequently**: Generate names and words to verify sound
✅ **Document Decisions**: Keep notes on design choices
✅ **Create Word Families**: Build vocabulary from common roots

### Don'ts

❌ **Don't Overcomplicate**: Too many rules make languages hard to use
❌ **Don't Mix Aesthetics**: Keep phonological style consistent
❌ **Don't Ignore Phonotactics**: Ensure sound combinations are pronounceable
❌ **Don't Forget Context**: Consider how the language fits your world
❌ **Don't Copy Directly**: Use real languages as inspiration, not templates

### Aesthetic Guidelines

**For Elegant/Flowing Languages (Elvish-style):**

- Favor vowels and liquid consonants (l, r, m, n)
- Use simple syllable structures (CV, CVC)
- Avoid harsh consonant clusters
- Include long vowels or diphthongs

**For Harsh/Guttural Languages (Dwarvish-style):**

- Favor stops and fricatives (k, g, kh, gh)
- Use complex syllable structures (CCVC, CVCC)
- Include consonant clusters
- Limit vowel variety

**For Ancient/Mystical Languages (Draconic-style):**

- Mix sibilants with resonants
- Use varied syllable structures
- Include unusual phonemes
- Favor compound words

## Example: Creating "Sylvan"

Let's create a forest-dwelling language step by step.

### 1. Phonology

```json
{
  "id": "sylvan",
  "name": "Sylvan",
  "description": "Language of the forest folk",
  "phonology": {
    "inventory": {
      "vowels": ["a", "e", "i", "o", "u", "y"],
      "consonants": ["s", "l", "v", "n", "m", "r", "th", "f", "w"],
      "diphthongs": ["ai", "ei", "ou"]
    },
    "syllableTemplates": [
      { "pattern": "CV", "weight": 45 },
      { "pattern": "CVC", "weight": 30 },
      { "pattern": "V", "weight": 15 },
      { "pattern": "CCV", "weight": 10, "allowedOnsets": ["sl", "sv", "fl"] }
    ],
    "clusters": {
      "initialClusters": ["sl", "sv", "fl", "vr"],
      "medialClusters": ["lv", "rv", "nv"],
      "finalClusters": ["n", "l", "r"]
    }
  }
}
```

### 2. Grammar

```json
{
  "grammar": {
    "wordOrder": "sov",
    "morphologyRules": [
      {
        "name": "plural",
        "type": "suffix",
        "pattern": "{root}in",
        "condition": "plural"
      },
      {
        "name": "diminutive",
        "type": "suffix",
        "pattern": "{root}il",
        "condition": "small"
      }
    ],
    "compoundRules": [
      {
        "pattern": "{root1}a{root2}",
        "connector": "a",
        "type": "noun_noun"
      }
    ]
  }
}
```

### 3. Sample Lexicon

```json
{
  "entries": [
    { "word": "silva", "meaning": "forest", "partOfSpeech": "noun", "root": "silv" },
    { "word": "lumen", "meaning": "light", "partOfSpeech": "noun", "root": "lum" },
    { "word": "verna", "meaning": "spring", "partOfSpeech": "noun", "root": "vern" },
    { "word": "flor", "meaning": "flower", "partOfSpeech": "noun", "root": "flor" }
  ]
}
```

### 4. Generated Names

Using this language, we might generate:

- Silvaren (forest-dweller)
- Lumevil (little light)
- Vernalor (spring-bringer)
- Floralin (flowers - plural)

## Resources

### Linguistic Concepts

- **IPA (International Phonetic Alphabet)**: Standard for representing sounds
- **Phonotactics**: Rules for sound combinations
- **Morphology**: Word structure and formation
- **Syntax**: Sentence structure

### Inspiration Sources

- **Tolkien's Languages**: Quenya, Sindarin (Elvish)
- **Natural Languages**: Study real-world language families
- **Conlang Communities**: r/conlangs, Language Creation Society

### Tools

- **IPA Chart**: https://www.ipachart.com/
- **Phonology Generators**: Various online tools
- **Etymology Dictionaries**: For root word inspiration

## Next Steps

1. Create your language definition JSON file
2. Test with the example application
3. Iterate on phonology based on generated names
4. Build your lexicon gradually
5. Share your language with the community!

Happy language creating! 🌟
