# Fantasy Language Service - Plugin Development Guide

This guide explains how to create custom language plugins for the PigeonPea Fantasy Language Service.

## Table of Contents

1. [Overview](#overview)
2. [Plugin Architecture](#plugin-architecture)
3. [Creating a Basic Plugin](#creating-a-basic-plugin)
4. [Implementing Custom Generators](#implementing-custom-generators)
5. [Plugin Registration](#plugin-registration)
6. [Testing Your Plugin](#testing-your-plugin)
7. [Best Practices](#best-practices)

## Overview

The Fantasy Language Service uses a plugin architecture that allows you to:

- Create custom language generators
- Implement specialized phonological systems
- Add domain-specific language features
- Extend translation capabilities
- Integrate external linguistic tools

## Plugin Architecture

The plugin system follows the RFC-013 tier-based architecture:

```
┌─────────────────────────────────────┐
│     Application Layer               │
│  (Game, Tools, Services)            │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│     Plugin Layer                    │
│  (Custom Language Implementations)  │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│     Contracts Layer                 │
│  (ILanguageService, etc.)           │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│     Core Implementation             │
│  (PhonologyEngine, etc.)            │
└─────────────────────────────────────┘
```

### Key Interfaces

Your plugin must implement:

1. **ILanguageService**: Main service interface
2. **IPhonologyEngine** (optional): Custom phonology
3. **IGrammarEngine** (optional): Custom grammar
4. **ILexiconManager** (optional): Custom lexicon storage

## Creating a Basic Plugin

### Step 1: Create Plugin Project

```bash
dotnet new classlib -n MyLanguagePlugin
cd MyLanguagePlugin
dotnet add reference ../PigeonPea.Language.Contracts/PigeonPea.Language.Contracts.csproj
```

### Step 2: Implement ILanguageService

```csharp
using PigeonPea.Language.Contracts;
using PigeonPea.Language.Contracts.Models;

namespace MyLanguagePlugin;

public class MyLanguageService : ILanguageService
{
    private readonly Dictionary<string, LanguageDefinition> _loadedLanguages = new();

    public async Task<bool> LoadLanguageAsync(string languageId, string configPath)
    {
        // Load and parse your language definition
        var json = await File.ReadAllTextAsync(configPath);
        var definition = JsonSerializer.Deserialize<LanguageDefinition>(json);

        if (definition == null)
            return false;

        _loadedLanguages[languageId] = definition;
        return true;
    }

    public Task<bool> UnloadLanguageAsync(string languageId)
    {
        return Task.FromResult(_loadedLanguages.Remove(languageId));
    }

    public IReadOnlyList<string> GetLoadedLanguages()
    {
        return _loadedLanguages.Keys.ToList();
    }

    public Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        // Implement your translation logic
        throw new NotImplementedException();
    }

    public string GenerateName(string languageId, NameGenerationOptions options)
    {
        // Implement your name generation logic
        throw new NotImplementedException();
    }

    public IEnumerable<string> GenerateNames(string languageId, int count, NameGenerationOptions options)
    {
        for (int i = 0; i < count; i++)
        {
            yield return GenerateName(languageId, options);
        }
    }

    public string GenerateSentence(string languageId, SentenceTemplate template)
    {
        // Implement sentence generation
        throw new NotImplementedException();
    }

    public string GenerateParagraph(string languageId, int sentenceCount)
    {
        // Implement paragraph generation
        throw new NotImplementedException();
    }
}
```

### Step 3: Add Plugin Metadata

Create a plugin manifest file `plugin.json`:

```json
{
  "id": "my-language-plugin",
  "name": "My Language Plugin",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Custom language generator for specialized use cases",
  "entryPoint": "MyLanguagePlugin.MyLanguageService",
  "dependencies": {
    "PigeonPea.Language.Contracts": "1.0.0"
  },
  "supportedLanguages": ["my-custom-language"]
}
```

## Implementing Custom Generators

### Custom Phonology Engine

Create specialized phonological rules:

```csharp
public class CustomPhonologyEngine : IPhonologyEngine
{
    public bool ValidatePhonemeInventory(PhonemeInventory inventory)
    {
        // Custom validation logic
        if (inventory.Vowels.Count < 3)
            return false;

        if (inventory.Consonants.Count < 5)
            return false;

        return true;
    }

    public string GenerateSyllable(SyllableTemplate template, PhonemeInventory inventory, Random random)
    {
        // Custom syllable generation
        var syllable = new StringBuilder();

        foreach (char c in template.Pattern)
        {
            switch (c)
            {
                case 'C':
                    syllable.Append(inventory.Consonants[random.Next(inventory.Consonants.Count)]);
                    break;
                case 'V':
                    syllable.Append(inventory.Vowels[random.Next(inventory.Vowels.Count)]);
                    break;
            }
        }

        return syllable.ToString();
    }

    public bool IsValidWord(string word, PhonologyRules rules)
    {
        // Custom word validation
        return !string.IsNullOrEmpty(word);
    }

    public bool ValidateSyllableTemplate(SyllableTemplate template, PhonemeInventory inventory)
    {
        // Validate template against inventory
        return !string.IsNullOrEmpty(template.Pattern);
    }
}
```

### Custom Grammar Engine

Implement specialized grammatical rules:

```csharp
public class CustomGrammarEngine : IGrammarEngine
{
    public bool ValidateGrammar(GrammarRules rules)
    {
        return rules != null && rules.MorphologyRules.Any();
    }

    public string[] ApplyWordOrder(string[] words, WordOrder order)
    {
        // Custom word order logic
        return order switch
        {
            WordOrder.SVO => words,
            WordOrder.SOV => new[] { words[0], words[2], words[1] },
            WordOrder.VSO => new[] { words[1], words[0], words[2] },
            _ => words
        };
    }

    public string ApplyMorphology(string root, MorphologyRule rule)
    {
        // Custom morphology application
        return rule.Type switch
        {
            "suffix" => root + rule.Pattern.Replace("{root}", ""),
            "prefix" => rule.Pattern.Replace("{root}", "") + root,
            _ => root
        };
    }

    public string FormCompound(string[] roots, CompoundRule rule)
    {
        // Custom compound formation
        if (rule.Connector != null)
            return string.Join(rule.Connector, roots);
        return string.Concat(roots);
    }
}
```

### Advanced: Markov Chain Name Generator

Example of a more sophisticated generator:

```csharp
public class MarkovNameGenerator
{
    private readonly Dictionary<string, List<char>> _transitions = new();
    private readonly int _order;

    public MarkovNameGenerator(int order = 2)
    {
        _order = order;
    }

    public void Train(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            var padded = new string('^', _order) + name.ToLower() + '$';

            for (int i = 0; i < padded.Length - _order; i++)
            {
                var context = padded.Substring(i, _order);
                var next = padded[i + _order];

                if (!_transitions.ContainsKey(context))
                    _transitions[context] = new List<char>();

                _transitions[context].Add(next);
            }
        }
    }

    public string Generate(Random random, int minLength = 4, int maxLength = 10)
    {
        var result = new StringBuilder();
        var context = new string('^', _order);

        while (result.Length < maxLength)
        {
            if (!_transitions.ContainsKey(context))
                break;

            var options = _transitions[context];
            var next = options[random.Next(options.Count)];

            if (next == '$' && result.Length >= minLength)
                break;

            if (next != '$')
                result.Append(next);

            context = context.Substring(1) + next;
        }

        return result.Length > 0
            ? char.ToUpper(result[0]) + result.ToString().Substring(1)
            : "Unnamed";
    }
}
```

## Plugin Registration

### Manual Registration

```csharp
public class PluginRegistry
{
    private readonly Dictionary<string, ILanguageService> _plugins = new();

    public void RegisterPlugin(string id, ILanguageService plugin)
    {
        _plugins[id] = plugin;
    }

    public ILanguageService? GetPlugin(string id)
    {
        return _plugins.TryGetValue(id, out var plugin) ? plugin : null;
    }

    public IEnumerable<string> GetRegisteredPlugins()
    {
        return _plugins.Keys;
    }
}
```

### Automatic Discovery

```csharp
public class PluginLoader
{
    public IEnumerable<ILanguageService> DiscoverPlugins(string pluginDirectory)
    {
        var plugins = new List<ILanguageService>();

        foreach (var dll in Directory.GetFiles(pluginDirectory, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var types = assembly.GetTypes()
                    .Where(t => typeof(ILanguageService).IsAssignableFrom(t) && !t.IsInterface);

                foreach (var type in types)
                {
                    if (Activator.CreateInstance(type) is ILanguageService plugin)
                    {
                        plugins.Add(plugin);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load plugin from {dll}: {ex.Message}");
            }
        }

        return plugins;
    }
}
```

## Testing Your Plugin

### Unit Tests

```csharp
using Xunit;

public class MyLanguageServiceTests
{
    [Fact]
    public async Task LoadLanguage_ValidConfig_ReturnsTrue()
    {
        // Arrange
        var service = new MyLanguageService();
        var configPath = "test-language.json";

        // Act
        var result = await service.LoadLanguageAsync("test", configPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GenerateName_ValidOptions_ReturnsNonEmptyString()
    {
        // Arrange
        var service = new MyLanguageService();
        var options = new NameGenerationOptions
        {
            MinSyllables = 2,
            MaxSyllables = 4
        };

        // Act
        var name = service.GenerateName("test", options);

        // Assert
        Assert.False(string.IsNullOrEmpty(name));
    }
}
```

### Integration Tests

```csharp
[Fact]
public async Task EndToEnd_LoadTranslateGenerate_WorksCorrectly()
{
    // Arrange
    var service = new MyLanguageService();
    await service.LoadLanguageAsync("test", "test-language.json");

    // Act - Generate name
    var name = service.GenerateName("test", new NameGenerationOptions());

    // Act - Translate
    var translated = await service.TranslateAsync("hello", "english", "test");

    // Assert
    Assert.NotNull(name);
    Assert.NotNull(translated);
}
```

## Best Practices

### Do's

✅ **Implement All Interface Methods**: Even if some return NotImplementedException initially
✅ **Use Dependency Injection**: Accept dependencies through constructor
✅ **Log Extensively**: Use ILogger for debugging and monitoring
✅ **Handle Errors Gracefully**: Catch and log exceptions, don't crash
✅ **Document Your API**: Add XML comments to public methods
✅ **Version Your Plugin**: Use semantic versioning
✅ **Test Thoroughly**: Write unit and integration tests

### Don'ts

❌ **Don't Block Async Methods**: Use async/await properly
❌ **Don't Store State Globally**: Use instance fields
❌ **Don't Ignore Thread Safety**: Consider concurrent access
❌ **Don't Hard-Code Paths**: Use configuration
❌ **Don't Swallow Exceptions**: Log and rethrow or handle properly

### Performance Tips

1. **Cache Loaded Languages**: Don't reload from disk repeatedly
2. **Use StringBuilder**: For string concatenation in loops
3. **Lazy Load Resources**: Load lexicons only when needed
4. **Pool Random Instances**: Reuse Random objects with seeds
5. **Batch Operations**: Process multiple items together when possible

## Example: Complete Plugin

Here's a complete example of a simple plugin:

```csharp
using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts;
using PigeonPea.Language.Contracts.Models;
using System.Text.Json;

namespace SimpleLanguagePlugin;

public class SimpleLanguageService : ILanguageService
{
    private readonly ILogger<SimpleLanguageService> _logger;
    private readonly Dictionary<string, LanguageDefinition> _languages = new();
    private readonly Random _random = new();

    public SimpleLanguageService(ILogger<SimpleLanguageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> LoadLanguageAsync(string languageId, string configPath)
    {
        try
        {
            _logger.LogInformation("Loading language {LanguageId} from {Path}", languageId, configPath);

            var json = await File.ReadAllTextAsync(configPath);
            var definition = JsonSerializer.Deserialize<LanguageDefinition>(json);

            if (definition == null)
            {
                _logger.LogError("Failed to deserialize language definition");
                return false;
            }

            _languages[languageId] = definition;
            _logger.LogInformation("Successfully loaded language {LanguageId}", languageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading language {LanguageId}", languageId);
            return false;
        }
    }

    public Task<bool> UnloadLanguageAsync(string languageId)
    {
        var removed = _languages.Remove(languageId);
        _logger.LogInformation("Unloaded language {LanguageId}: {Success}", languageId, removed);
        return Task.FromResult(removed);
    }

    public IReadOnlyList<string> GetLoadedLanguages()
    {
        return _languages.Keys.ToList();
    }

    public string GenerateName(string languageId, NameGenerationOptions options)
    {
        if (!_languages.TryGetValue(languageId, out var language))
            throw new InvalidOperationException($"Language {languageId} not loaded");

        var syllableCount = _random.Next(options.MinSyllables, options.MaxSyllables + 1);
        var name = new StringBuilder();

        for (int i = 0; i < syllableCount; i++)
        {
            var template = language.Phonology.SyllableTemplates[
                _random.Next(language.Phonology.SyllableTemplates.Count)];

            foreach (char c in template.Pattern)
            {
                if (c == 'C')
                    name.Append(language.Phonology.Inventory.Consonants[
                        _random.Next(language.Phonology.Inventory.Consonants.Count)]);
                else if (c == 'V')
                    name.Append(language.Phonology.Inventory.Vowels[
                        _random.Next(language.Phonology.Inventory.Vowels.Count)]);
            }
        }

        // Capitalize first letter
        if (name.Length > 0)
            name[0] = char.ToUpper(name[0]);

        return name.ToString();
    }

    public IEnumerable<string> GenerateNames(string languageId, int count, NameGenerationOptions options)
    {
        for (int i = 0; i < count; i++)
        {
            yield return GenerateName(languageId, options);
        }
    }

    public Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        // Simple word-by-word translation
        var words = text.Split(' ');
        var translated = words.Select(w => GenerateName(targetLanguage, new NameGenerationOptions
        {
            MinSyllables = 1,
            MaxSyllables = 2
        }));

        return Task.FromResult(string.Join(" ", translated));
    }

    public string GenerateSentence(string languageId, SentenceTemplate template)
    {
        return $"{template.Subject} {template.Verb} {template.Object}";
    }

    public string GenerateParagraph(string languageId, int sentenceCount)
    {
        var sentences = new List<string>();
        for (int i = 0; i < sentenceCount; i++)
        {
            sentences.Add(GenerateName(languageId, new NameGenerationOptions()));
        }
        return string.Join(". ", sentences) + ".";
    }
}
```

## Deployment

### Package Your Plugin

```bash
dotnet pack -c Release
```

### Install Plugin

1. Copy DLL to plugin directory
2. Add plugin.json manifest
3. Restart application or reload plugins

### Distribution

Consider publishing to:

- NuGet (for .NET plugins)
- GitHub Releases
- Custom plugin repository

## Resources

- [PigeonPea Language Contracts API](../src/PigeonPea.Language.Contracts/README.md)
- [Language Creation Guide](language-creation-guide.md)
- [RFC-013: Plugin Architecture](../../docs/rfcs/013-plugin-architecture-refinement-tiered.md)

## Support

For questions or issues:

- Open an issue on GitHub
- Join the community Discord
- Check the documentation wiki

Happy plugin development! 🚀
