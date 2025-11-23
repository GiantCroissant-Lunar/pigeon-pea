using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts;
using PigeonPea.Language.Core;

namespace PigeonPea.Language.Core.Examples;

/// <summary>
/// Example demonstrating the Language to FMG Bridge Adapter usage
/// </summary>
public class NameGeneratorBridgeExample
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== PigeonPea Language to FMG Bridge Adapter Demo ===\n");

        // Set up dependency injection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });

        // Add PigeonPea Language services with configuration
        services.AddPigeonPeaLanguage(config =>
        {
            config.DefaultMode = GenerationMode.RuleBased;
            config.EnableTemplateCache = true;
            config.EnableDetailedLogging = false;
            config.CustomTemplateMappings = new Dictionary<string, string>
            {
                ["high-elvish"] = "elvish",
                ["mountain-dwarves"] = "dwarvish",
                ["black-orcs"] = "orcish"
            };
        });

        var serviceProvider = services.BuildServiceProvider();
        var languageService = serviceProvider.GetRequiredService<ILanguageService>();

        try
        {
            // Example 1: Generate English town names using built-in template
            Console.WriteLine("1. English Town Names (Germanic Template):");
            for (int i = 0; i < 5; i++)
            {
                var townName = languageService.GenerateNameFromTemplate(
                    "germanic",
                    NameType.Place,
                    GenerationMode.RuleBased);
                Console.WriteLine($"   {townName}");
            }
            Console.WriteLine();

            // Example 2: Generate Elvish names
            Console.WriteLine("2. Elvish Names:");
            for (int i = 0; i < 5; i++)
            {
                var elvishName = languageService.GenerateNameFromTemplate(
                    "elvish",
                    NameType.Personal,
                    GenerationMode.RuleBased);
                Console.WriteLine($"   {elvishName}");
            }
            Console.WriteLine();

            // Example 3: Generate Dwarvish fortress names
            Console.WriteLine("3. Dwarvish Fortress Names:");
            for (int i = 0; i < 5; i++)
            {
                var dwarvenName = languageService.GenerateNameFromTemplate(
                    "dwarvish",
                    NameType.Place,
                    GenerationMode.RuleBased);
                Console.WriteLine($"   {dwarvenName}");
            }
            Console.WriteLine();

            // Example 4: Generate Orcish clan names
            Console.WriteLine("4. Orcish Clan Names:");
            for (int i = 0; i < 5; i++)
            {
                var orcishName = languageService.GenerateNameFromTemplate(
                    "orcish",
                    NameType.Clan,
                    GenerationMode.RuleBased);
                Console.WriteLine($"   {orcishName}");
            }
            Console.WriteLine();

            // Example 5: Generate Japanese place names
            Console.WriteLine("5. Japanese Place Names:");
            for (int i = 0; i < 5; i++)
            {
                var japaneseName = languageService.GenerateNameFromTemplate(
                    "japanese",
                    NameType.Place,
                    GenerationMode.RuleBased);
                Console.WriteLine($"   {japaneseName}");
            }
            Console.WriteLine();

            // Example 6: Use Markov chain mode for authentic names
            Console.WriteLine("6. Elvish Names (Markov Chain Mode):");
            for (int i = 0; i < 5; i++)
            {
                var markovName = languageService.GenerateNameFromTemplate(
                    "elvish",
                    NameType.Personal,
                    GenerationMode.MarkovChain);
                Console.WriteLine($"   {markovName}");
            }
            Console.WriteLine();

            // Example 7: Use Hybrid mode
            Console.WriteLine("7. Dwarvish Names (Hybrid Mode):");
            for (int i = 0; i < 5; i++)
            {
                var hybridName = languageService.GenerateNameFromTemplate(
                    "dwarvish",
                    NameType.Personal,
                    GenerationMode.Hybrid);
                Console.WriteLine($"   {hybridName}");
            }
            Console.WriteLine();

            // Example 8: Generate different types of names
            Console.WriteLine("8. Different Name Types (Germanic):");
            Console.WriteLine($"   Person: {languageService.GenerateNameFromTemplate("germanic", NameType.Personal)}");
            Console.WriteLine($"   Place:  {languageService.GenerateNameFromTemplate("germanic", NameType.Place)}");
            Console.WriteLine($"   Clan:   {languageService.GenerateNameFromTemplate("germanic", NameType.Clan)}");
            Console.WriteLine($"   Item:    {languageService.GenerateNameFromTemplate("germanic", NameType.Item)}");
            Console.WriteLine($"   Title:   {languageService.GenerateNameFromTemplate("germanic", NameType.Title)}");
            Console.WriteLine();

            // Example 9: Show available templates
            Console.WriteLine("9. Available Name Generation Templates:");
            var templates = languageService.GetAvailableNameGenerationTemplates();
            foreach (var template in templates.OrderBy(t => t))
            {
                Console.WriteLine($"   {template}");
            }
            Console.WriteLine();

            // Example 10: Generate names for a fantasy world
            Console.WriteLine("10. Fantasy World Name Generation:");
            Console.WriteLine($"   Kingdom: {languageService.GenerateNameFromTemplate("romance", NameType.Place)}");
            Console.WriteLine($"   Capital: {languageService.GenerateNameFromTemplate("germanic", NameType.Place)}");
            Console.WriteLine($"   King: {languageService.GenerateNameFromTemplate("elvish", NameType.Personal)}");
            Console.WriteLine($"   Queen: {languageService.GenerateNameFromTemplate("elvish", NameType.Personal)}");
            Console.WriteLine($"   Mountain: {languageService.GenerateNameFromTemplate("dwarvish", NameType.Place)}");
            Console.WriteLine($"   Forest: {languageService.GenerateNameFromTemplate("celtic", NameType.Place)}");
            Console.WriteLine($"   River: {languageService.GenerateNameFromTemplate("nordic", NameType.Place)}");
            Console.WriteLine();

            Console.WriteLine("=== Demo completed successfully! ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Example of using the bridge adapter in a game context
    /// </summary>
    public static void GameContextExample(IServiceProvider serviceProvider)
    {
        var languageService = serviceProvider.GetRequiredService<ILanguageService>();

        // Generate names for different game elements
        var playerCharacter = new Character
        {
            Name = languageService.GenerateNameFromTemplate("elvish", NameType.Personal),
            Race = "Elf",
            Class = "Ranger"
        };

        var town = new Location
        {
            Name = languageService.GenerateNameFromTemplate("germanic", NameType.Place),
            Type = "Town",
            Population = 1500
        };

        var dungeon = new Location
        {
            Name = languageService.GenerateNameFromTemplate("dwarvish", NameType.Place),
            Type = "Dungeon",
            Description = "Ancient dwarven ruins"
        };

        var guild = new Guild
        {
            Name = languageService.GenerateNameFromTemplate("orcish", NameType.Clan),
            Type = "Warrior Guild",
            Members = 25
        };

        Console.WriteLine($"Generated Character: {playerCharacter.Name} the {playerCharacter.Race} {playerCharacter.Class}");
        Console.WriteLine($"Generated Town: {town.Name} ({town.Type}, Population: {town.Population})");
        Console.WriteLine($"Generated Dungeon: {dungeon.Name} ({dungeon.Type}) - {dungeon.Description}");
        Console.WriteLine($"Generated Guild: {guild.Name} ({guild.Type}, {guild.Members} members)");
    }

    /// <summary>
    /// Example classes for game context
    /// </summary>
    public class Character
    {
        public string Name { get; set; } = string.Empty;
        public string Race { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
    }

    public class Location
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Population { get; set; }
    }

    public class Guild
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Members { get; set; }
    }
}
