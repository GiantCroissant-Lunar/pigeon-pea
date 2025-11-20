# Fantasy Language Service - Example Console Application

This is a demonstration console application for the PigeonPea Fantasy Language Service. It provides an interactive CLI to explore the language service capabilities.

## Features

- **Load Languages**: Load fantasy language definitions from JSON files
- **List Languages**: View all currently loaded languages
- **Translate Text**: Translate English text to fantasy languages
- **Generate Names**: Create authentic-sounding names in any loaded language

## Building and Running

```bash
# Build the example
dotnet build

# Run the example
dotnet run
```

## Available Commands

### help

Display all available commands and examples.

```
> help
```

### load <path>

Load a language definition from a JSON file.

```
> load Examples/high-elvish.json
> load Examples/dwarvish.json
> load Examples/draconic.json
```

### list

List all currently loaded languages.

```
> list
```

### translate <language-id> <text>

Translate English text to the specified fantasy language.

```
> translate high-elvish hello world
> translate dwarvish the mountain king
```

Note: Translation requires a lexicon file. If words are not in the lexicon, they will be generated based on the language's phonological rules.

### generate-name <language-id> [count]

Generate one or more names in the specified language.

```
> generate-name high-elvish
> generate-name dwarvish 10
```

### exit

Exit the application.

```
> exit
```

## Example Session

```
===========================================
  Fantasy Language Service - Demo CLI
===========================================

Type 'help' for available commands

> load Examples/high-elvish.json
Loading language from: D:\...\Examples\high-elvish.json
✓ Loaded language: High Elvish (high-elvish)
  Description: The ancient tongue of the Elven nobility...
  Vowels: a, e, i, o, u, á, é, í
  Consonants: l, r, n, m, s, h, t, th, d, v, f
  Word Order: SVO

> generate-name high-elvish 5
Generating 5 names in high-elvish:

  1. Aelóriel
  2. Thaloren
  3. Mirathel
  4. Elendil
  5. Lothíriel

> translate high-elvish hello friend
Translating: "hello friend"
Target: high-elvish
Result: Aelór mellon

> exit
Goodbye!
```

## Language Definition Files

The example includes three sample language definitions in the `Examples` directory:

- **high-elvish.json**: Soft, flowing language inspired by Tolkien's Elvish
- **dwarvish.json**: Harsh, guttural language with consonant clusters
- **draconic.json**: Ancient, booming language of dragons

## Architecture

The example demonstrates the full Fantasy Language Service architecture:

1. **LanguageDefinitionRepository**: Loads and manages language definitions
2. **PhonologyEngine**: Handles phoneme inventories and syllable generation
3. **LexiconManager**: Manages word mappings between languages
4. **GrammarEngine**: Applies grammatical rules and word order
5. **SoundChangeEngine**: Handles language evolution and sound changes
6. **LanguageService**: Coordinates all subsystems

## Next Steps

- Create your own language definitions
- Add custom lexicon files
- Experiment with different phonological rules
- Try language derivation with sound changes

For more information, see the main project documentation.
