using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using PigeonPea.Language.Contracts;
using PigeonPea.Language.Contracts.Models;
using PigeonPea.Language.Core;
using Xunit;

namespace PigeonPea.Language.Core.Tests;

/// <summary>
/// Integration tests for LanguageService with bridge adapter
/// </summary>
public class LanguageServiceIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILanguageService _languageService;

    public LanguageServiceIntegrationTests()
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });

        // Add PigeonPea Language services
        services.AddPigeonPeaLanguage(config =>
        {
            config.DefaultMode = GenerationMode.RuleBased;
            config.EnableTemplateCache = true;
            config.EnableDetailedLogging = true;
        });

        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        _languageService = _serviceProvider.GetRequiredService<ILanguageService>();
    }

    [Fact]
    public void ServiceProvider_CanResolveAllServices()
    {
        // Act & Assert
        Assert.NotNull(_languageService);
        Assert.IsType<LanguageService>(_languageService);
        
        var adapter = _serviceProvider.GetService<INameGeneratorAdapter>();
        Assert.NotNull(adapter);
        Assert.IsType<LanguageToFMGAdapter>(adapter);
    }

    [Fact]
    public void GenerateNameAdvanced_WithBuiltInLanguage_GeneratesNames()
    {
        // Arrange - This would normally be loaded from a file, but for testing we'll create a mock
        // In a real scenario, you'd call: await _languageService.LoadLanguageAsync("elvish", "path/to/elvish.json")
        
        // For this test, we'll use template-based generation instead
        var templateName = "elvish";

        // Act
        var result = _languageService.GenerateNameFromTemplate(templateName, NameType.Personal);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 2);
        
        // Should look somewhat Elvish
        Assert.True(ContainsElvishCharacters(result));
    }

    [Fact]
    public void GenerateNameFromTemplate_WithAllTemplates_GeneratesNames()
    {
        // Arrange
        var templates = _languageService.GetAvailableNameGenerationTemplates();
        Assert.NotEmpty(templates);

        // Act & Assert - Test each template
        foreach (var template in templates.Take(5)) // Test first 5 to keep test fast
        {
            var name = _languageService.GenerateNameFromTemplate(template, NameType.Personal);
            
            Assert.NotNull(name);
            Assert.NotEmpty(name);
            Assert.True(name.Length > 1);
        }
    }

    [Fact]
    public void GenerateNameFromTemplate_WithDifferentNameTypes_GeneratesAppropriateNames()
    {
        // Arrange
        var templateName = "germanic";

        // Act
        var personName = _languageService.GenerateNameFromTemplate(templateName, NameType.Personal);
        var placeName = _languageService.GenerateNameFromTemplate(templateName, NameType.Place);
        var clanName = _languageService.GenerateNameFromTemplate(templateName, NameType.Clan);

        // Assert
        Assert.NotNull(personName);
        Assert.NotNull(placeName);
        Assert.NotNull(clanName);
        
        Assert.NotEmpty(personName);
        Assert.NotEmpty(placeName);
        Assert.NotEmpty(clanName);
        
        // Names should generally be different
        Assert.NotEqual(personName, placeName);
    }

    [Fact]
    public void GenerateNameFromTemplate_WithDifferentGenerationModes_WorksCorrectly()
    {
        // Arrange
        var templateName = "dwarvish";

        // Act
        var ruleBasedName = _languageService.GenerateNameFromTemplate(templateName, NameType.Personal, GenerationMode.RuleBased);
        var markovName = _languageService.GenerateNameFromTemplate(templateName, NameType.Personal, GenerationMode.MarkovChain);
        var hybridName = _languageService.GenerateNameFromTemplate(templateName, NameType.Personal, GenerationMode.Hybrid);

        // Assert
        Assert.NotNull(ruleBasedName);
        Assert.NotNull(markovName);
        Assert.NotNull(hybridName);
        
        Assert.NotEmpty(ruleBasedName);
        Assert.NotEmpty(markovName);
        Assert.NotEmpty(hybridName);
    }

    [Fact]
    public void GetAvailableNameGenerationTemplates_ReturnsExpectedTemplates()
    {
        // Act
        var templates = _languageService.GetAvailableNameGenerationTemplates();

        // Assert
        Assert.NotNull(templates);
        Assert.NotEmpty(templates);
        
        var templateList = templates.ToList();
        Assert.Contains("germanic", templateList);
        Assert.Contains("elvish", templateList);
        Assert.Contains("dwarvish", templateList);
        Assert.Contains("orcish", templateList);
        Assert.Contains("japanese", templateList);
    }

    [Fact]
    public void GenerateMultipleNames_ReturnsUniqueNames()
    {
        // Arrange
        var templateName = "elvish";
        var names = new HashSet<string>();

        // Act
        for (int i = 0; i < 20; i++)
        {
            var name = _languageService.GenerateNameFromTemplate(templateName, NameType.Personal);
            names.Add(name);
        }

        // Assert
        Assert.True(names.Count > 15); // Should have reasonable variety
    }

    [Fact]
    public void DifferentLanguages_GenerateDifferentStyleNames()
    {
        // Arrange
        var elvishName = _languageService.GenerateNameFromTemplate("elvish", NameType.Personal);
        var dwarvishName = _languageService.GenerateNameFromTemplate("dwarvish", NameType.Personal);
        var orcishName = _languageService.GenerateNameFromTemplate("orcish", NameType.Personal);
        var japaneseName = _languageService.GenerateNameFromTemplate("japanese", NameType.Personal);

        // Assert
        Assert.NotNull(elvishName);
        Assert.NotNull(dwarvishName);
        Assert.NotNull(orcishName);
        Assert.NotNull(japaneseName);
        
        // Names should generally be different
        var allNames = new[] { elvishName, dwarvishName, orcishName, japaneseName };
        var uniqueNames = allNames.Distinct().ToArray();
        Assert.Equal(allNames.Length, uniqueNames.Length);
        
        // Should have some characteristic differences
        Assert.True(ContainsElvishCharacters(elvishName));
        Assert.True(ContainsDwarvishCharacters(dwarvishName));
    }

    [Fact]
    public void Configuration_CustomMappings_WorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        services.AddPigeonPeaLanguage(config =>
        {
            config.CustomTemplateMappings = new Dictionary<string, string>
            {
                ["custom-elvish"] = "elvish",
                ["custom-dwarvish"] = "dwarvish"
            };
        });

        var provider = services.BuildServiceProvider();
        var languageService = provider.GetRequiredService<ILanguageService>();
        var adapter = provider.GetRequiredService<INameGeneratorAdapter>();

        // Act
        var hasCustomElvish = adapter.HasBuiltInTemplate("custom-elvish");
        var hasCustomDwarvish = adapter.HasBuiltInTemplate("custom-dwarvish");

        // Assert
        Assert.True(hasCustomElvish);
        Assert.True(hasCustomDwarvish);
    }

    [Fact]
    public void ErrorHandling_InvalidInputs_ThrowsAppropriateExceptions()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _languageService.GenerateNameFromTemplate("", NameType.Personal));
        
        Assert.Throws<ArgumentException>(() => 
            _languageService.GenerateNameFromTemplate(null!, NameType.Personal));
        
        Assert.Throws<ArgumentException>(() => 
            _languageService.GenerateNameFromTemplate("   ", NameType.Personal));
    }

    [Fact]
    public void Performance_GenerateMultipleNames_PerformsAcceptably()
    {
        // Arrange
        var templateName = "germanic";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 100; i++)
        {
            _languageService.GenerateNameFromTemplate(templateName, NameType.Personal);
        }

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should be fast (< 50ms per name)
    }

    private static bool ContainsElvishCharacters(string name)
    {
        // Elvish names often contain: l, r, v, th, el, ia, on
        var lowerName = name.ToLowerInvariant();
        return lowerName.Contains('l') && lowerName.Contains('r') && 
               (lowerName.Contains('v') || lowerName.Contains("th") || 
                lowerName.Contains("el") || lowerName.Contains("ia") || lowerName.Contains("on"));
    }

    private static bool ContainsDwarvishCharacters(string name)
    {
        // Dwarvish names often contain: k, r, d, g, ur, im, dor
        var lowerName = name.ToLowerInvariant();
        return (lowerName.Contains('k') || lowerName.Contains('d') || lowerName.Contains('g')) &&
               (lowerName.Contains('r') || lowerName.Contains("ur") || 
                lowerName.Contains("im") || lowerName.Contains("dor"));
    }
}
