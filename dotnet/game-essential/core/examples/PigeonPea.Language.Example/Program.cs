using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts;
using PigeonPea.Language.Contracts.Models;
using PigeonPea.Language.Core;
using System.Text.Json;

namespace PigeonPea.Language.Example;

/// <summary>
/// Example console application demonstrating the Fantasy Language Service.
/// Supports commands: load, list, translate, generate-name, help, exit
/// </summary>
class Program
{
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly LanguageDefinitionRepository _repository = new(_loggerFactory.CreateLogger<LanguageDefinitionRepository>());
    private static readonly PhonologyEngine _phonologyEngine = new(_loggerFactory.CreateLogger<PhonologyEngine>());
    private static readonly LexiconManager _lexiconManager = new(_loggerFactory.CreateLogger<LexiconManager>());
    private static readonly GrammarEngine _grammarEngine = new(_loggerFactory.CreateLogger<GrammarEngine>());
    private static readonly SoundChangeEngine _soundChangeEngine = new(_loggerFactory.CreateLogger<SoundChangeEngine>());
    private static readonly LanguageService _languageService = new(
        _repository,
        _phonologyEngine,
        _lexiconManager,
        _grammarEngine,
        _soundChangeEngine,
        _loggerFactory);
    private static readonly Dictionary<string, string> _loadedLanguages = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  Fantasy Language Service - Demo CLI");
        Console.WriteLine("===========================================");
        Console.WriteLine();
        Console.WriteLine("Type 'help' for available commands");
        Console.WriteLine();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
                continue;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower(System.Globalization.CultureInfo.InvariantCulture);

            try
            {
                switch (command)
                {
                    case "help":
                        ShowHelp();
                        break;
                    case "load":
                        await LoadLanguageCommand(parts).ConfigureAwait(false);
                        break;
                    case "list":
                        ListLanguagesCommand();
                        break;
                    case "translate":
                        await TranslateCommand(parts).ConfigureAwait(false);
                        break;
                    case "generate-name":
                    case "name":
                        GenerateNameCommand(parts);
                        break;
                    case "exit":
                    case "quit":
                        Console.WriteLine("Goodbye!");
                        return;
                    default:
                        Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine();
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("Available Commands:");
        Console.WriteLine("  load <path>              - Load a language definition from JSON file");
        Console.WriteLine("  list                     - List all loaded languages");
        Console.WriteLine("  translate <lang> <text>  - Translate English text to fantasy language");
        Console.WriteLine("  generate-name <lang> [count] - Generate names in the specified language");
        Console.WriteLine("  help                     - Show this help message");
        Console.WriteLine("  exit                     - Exit the application");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  load Examples/high-elvish.json");
        Console.WriteLine("  translate high-elvish hello world");
        Console.WriteLine("  generate-name high-elvish 5");
    }

    static async Task LoadLanguageCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            Console.WriteLine("Usage: load <path>");
            Console.WriteLine("Example: load Examples/high-elvish.json");
            return;
        }

        var path = string.Join(" ", parts.Skip(1));
        
        // Resolve relative path
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            // Try relative to the Language.Core project
            var alternativePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..", "src", "PigeonPea.Language.Core",
                path);
            fullPath = Path.GetFullPath(alternativePath);
        }

        if (!File.Exists(fullPath))
        {
            Console.WriteLine($"File not found: {path}");
            return;
        }

        Console.WriteLine($"Loading language from: {fullPath}");
        
        var json = await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
        var definition = JsonSerializer.Deserialize<LanguageDefinition>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (definition == null)
        {
            Console.WriteLine("Failed to parse language definition");
            return;
        }

        var success = await _languageService.LoadLanguageAsync(definition.Id, fullPath).ConfigureAwait(false);
        if (!success)
        {
            Console.WriteLine("Failed to load language");
            return;
        }

        _loadedLanguages[definition.Id] = definition.Name;
        
        Console.WriteLine($"✓ Loaded language: {definition.Name} ({definition.Id})");
        Console.WriteLine($"  Description: {definition.Description}");
        Console.WriteLine($"  Vowels: {string.Join(", ", definition.Phonology.Inventory.Vowels)}");
        Console.WriteLine($"  Consonants: {string.Join(", ", definition.Phonology.Inventory.Consonants)}");
        Console.WriteLine($"  Word Order: {definition.Grammar.WordOrder.ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    static void ListLanguagesCommand()
    {
        var languages = _languageService.GetLoadedLanguages();
        
        if (!languages.Any())
        {
            Console.WriteLine("No languages loaded. Use 'load <path>' to load a language.");
            return;
        }

        Console.WriteLine("Loaded Languages:");
        foreach (var langId in languages)
        {
            var name = _loadedLanguages.TryGetValue(langId, out var n) ? n : langId;
            Console.WriteLine($"  - {name} ({langId})");
        }
    }

    static async Task TranslateCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: translate <language-id> <text>");
            Console.WriteLine("Example: translate high-elvish hello world");
            return;
        }

        var languageId = parts[1];
        var text = string.Join(" ", parts.Skip(2));

        if (!_languageService.GetLoadedLanguages().Contains(languageId))
        {
            Console.WriteLine($"Language '{languageId}' is not loaded. Use 'load' command first.");
            return;
        }

        Console.WriteLine($"Translating: \"{text}\"");
        Console.WriteLine($"Target: {languageId}");
        
        var result = await _languageService.TranslateAsync(text, "english", languageId).ConfigureAwait(false);
        
        Console.WriteLine($"Result: {result}");
        Console.WriteLine();
        Console.WriteLine("Note: Translation requires a lexicon file. If words are missing,");
        Console.WriteLine("they will be generated based on phonological rules.");
    }

    static void GenerateNameCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            Console.WriteLine("Usage: generate-name <language-id> [count]");
            Console.WriteLine("Example: generate-name high-elvish 5");
            return;
        }

        var languageId = parts[1];
        var count = parts.Length > 2 && int.TryParse(parts[2], out var c) ? c : 5;

        if (!_languageService.GetLoadedLanguages().Contains(languageId))
        {
            Console.WriteLine($"Language '{languageId}' is not loaded. Use 'load' command first.");
            return;
        }

        Console.WriteLine($"Generating {count} names in {languageId}:");
        Console.WriteLine();

        var options = new NameGenerationOptions
        {
            MinSyllables = 2,
            MaxSyllables = 4,
            Type = NameType.Personal
        };

        var names = _languageService.GenerateNames(languageId, count, options).ToList();
        
        for (int i = 0; i < names.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {names[i]}");
        }
    }
}
