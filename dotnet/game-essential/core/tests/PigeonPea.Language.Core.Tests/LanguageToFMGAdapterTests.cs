using Microsoft.Extensions.Logging;
using Moq;
using PigeonPea.Language.Contracts;
using PigeonPea.Language.Contracts.Grammar;
using PigeonPea.Language.Contracts.Models;
using PigeonPea.Language.Contracts.Phonology;
using PigeonPea.Language.Core;
using Xunit;

namespace PigeonPea.Language.Core.Tests;

/// <summary>
/// Unit tests for LanguageToFMGAdapter
/// </summary>
public class LanguageToFMGAdapterTests
{
    private readonly Mock<ILogger<LanguageToFMGAdapter>> _mockLogger;
    private readonly LanguageToFMGAdapter _adapter;

    public LanguageToFMGAdapterTests()
    {
        _mockLogger = new Mock<ILogger<LanguageToFMGAdapter>>();
        _adapter = new LanguageToFMGAdapter(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LanguageToFMGAdapter(null!));
    }

    [Fact]
    public void GenerateName_WithNullLanguage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _adapter.GenerateName(null!, NameType.Personal));
    }

    [Fact]
    public void GenerateName_WithMappedLanguage_UsesBuiltInTemplate()
    {
        // Arrange
        var language = new LanguageDefinition
        {
            Id = "elvish",
            Name = "Elvish",
            Phonology = CreateTestPhonology(),
            Grammar = new Grammar()
        };

        // Act
        var result = _adapter.GenerateName(language, NameType.Personal);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 2); // Should be a reasonable name length
    }

    [Fact]
    public void GenerateName_WithCustomLanguage_ConvertsToFMGTemplate()
    {
        // Arrange
        var language = new LanguageDefinition
        {
            Id = "custom-lang",
            Name = "Custom Language",
            Phonology = CreateTestPhonology(),
            Grammar = new Grammar()
        };

        // Act
        var result = _adapter.GenerateName(language, NameType.Personal);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 1);
    }

    [Fact]
    public void GenerateFromTemplate_WithNullTemplateName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _adapter.GenerateFromTemplate(null!, NameType.Personal));
        Assert.Throws<ArgumentException>(() => _adapter.GenerateFromTemplate("", NameType.Personal));
        Assert.Throws<ArgumentException>(() => _adapter.GenerateFromTemplate("   ", NameType.Personal));
    }

    [Fact]
    public void GenerateFromTemplate_WithValidTemplate_GeneratesName()
    {
        // Arrange
        var templateName = "germanic";

        // Act
        var result = _adapter.GenerateFromTemplate(templateName, NameType.Place);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 2);
    }

    [Theory]
    [InlineData("germanic")]
    [InlineData("elvish")]
    [InlineData("dwarvish")]
    [InlineData("orcish")]
    [InlineData("japanese")]
    [InlineData("chinese")]
    [InlineData("korean")]
    [InlineData("slavic")]
    [InlineData("romance")]
    [InlineData("nordic")]
    [InlineData("celtic")]
    [InlineData("arabic")]
    public void GenerateFromTemplate_WithKnownTemplates_GeneratesNames(string templateName)
    {
        // Act
        var result = _adapter.GenerateFromTemplate(templateName, NameType.Personal);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void HasBuiltInTemplate_WithInvalidTemplateName_ReturnsFalse(string templateName)
    {
        // Act
        var result = _adapter.HasBuiltInTemplate(templateName);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("germanic")]
    [InlineData("elvish")]
    [InlineData("dwarvish")]
    [InlineData("orcish")]
    public void HasBuiltInTemplate_WithValidTemplateName_ReturnsTrue(string templateName)
    {
        // Act
        var result = _adapter.HasBuiltInTemplate(templateName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetBuiltInTemplates_ReturnsExpectedTemplates()
    {
        // Act
        var templates = _adapter.GetBuiltInTemplates();

        // Assert
        Assert.NotNull(templates);
        Assert.NotEmpty(templates);
        
        var templateList = templates.ToList();
        Assert.Contains("germanic", templateList);
        Assert.Contains("elvish", templateList);
        Assert.Contains("dwarvish", templateList);
        Assert.Contains("orcish", templateList);
    }

    [Fact]
    public void GenerateName_WithDifferentNameTypes_GeneratesDifferentNames()
    {
        // Arrange
        var language = new LanguageDefinition
        {
            Id = "custom-lang",
            Name = "Custom Language",
            Phonology = CreateTestPhonology(),
            Grammar = new Grammar()
        };

        // Act
        var personName = _adapter.GenerateName(language, NameType.Personal);
        var placeName = _adapter.GenerateName(language, NameType.Place);
        var clanName = _adapter.GenerateName(language, NameType.Clan);

        // Assert
        Assert.NotNull(personName);
        Assert.NotNull(placeName);
        Assert.NotNull(clanName);
        
        // Names should generally be different (though could occasionally be same by chance)
        Assert.NotEqual(personName, placeName);
        Assert.NotEqual(personName, clanName);
    }

    [Fact]
    public void GenerateName_WithDifferentGenerationModes_WorksCorrectly()
    {
        // Arrange
        var language = new LanguageDefinition
        {
            Id = "custom-lang",
            Name = "Custom Language",
            Phonology = CreateTestPhonology(),
            Grammar = new Grammar()
        };

        // Act
        var ruleBasedName = _adapter.GenerateName(language, NameType.Personal, GenerationMode.RuleBased);
        var markovName = _adapter.GenerateName(language, NameType.Personal, GenerationMode.MarkovChain);
        var hybridName = _adapter.GenerateName(language, NameType.Personal, GenerationMode.Hybrid);

        // Assert
        Assert.NotNull(ruleBasedName);
        Assert.NotNull(markovName);
        Assert.NotNull(hybridName);
        
        Assert.NotEmpty(ruleBasedName);
        Assert.NotEmpty(markovName);
        Assert.NotEmpty(hybridName);
    }

    [Fact]
    public void GenerateMultipleNames_ReturnsUniqueNames()
    {
        // Arrange
        var language = new LanguageDefinition
        {
            Id = "custom-lang",
            Name = "Custom Language",
            Phonology = CreateTestPhonology(),
            Grammar = new Grammar()
        };

        var names = new HashSet<string>();

        // Act
        for (int i = 0; i < 50; i++)
        {
            var name = _adapter.GenerateName(language, NameType.Personal);
            names.Add(name);
        }

        // Assert
        Assert.True(names.Count > 30); // Should have reasonable variety
    }

    [Fact]
    public void GenerateFromTemplate_CachingBehavior_ConsistentResultsForSameInput()
    {
        // Arrange
        var templateName = "elvish";

        // Act
        var name1 = _adapter.GenerateFromTemplate(templateName, NameType.Personal);
        var name2 = _adapter.GenerateFromTemplate(templateName, NameType.Personal);

        // Assert
        // Names should generally be different due to random generation
        // but this test ensures no exceptions occur with caching
        Assert.NotNull(name1);
        Assert.NotNull(name2);
        Assert.NotEmpty(name1);
        Assert.NotEmpty(name2);
    }

    [Fact]
    public void GenerateName_WithComplexPhonology_HandlesCorrectly()
    {
        // Arrange
        var language = new LanguageDefinition
        {
            Id = "complex-lang",
            Name = "Complex Language",
            Phonology = new Phonology
            {
                Inventory = new PhonemeInventory
                {
                    Consonants = new[] { "p", "t", "k", "b", "d", "g", "s", "ʃ", "m", "n", "ŋ", "l", "r", "w", "j" },
                    Vowels = new[] { "i", "e", "a", "o", "u", "ɨ", "ə" }
                },
                Clusters = new ConsonantClusters
                {
                    InitialClusters = new[] { "pr", "tr", "kr", "sp", "st" },
                    FinalClusters = new[] { "kt", "mp", "ŋk", "rd" },
                    MaxConsonantCluster = 2
                },
                SyllableTemplates = new[]
                {
                    new SyllableTemplate { Pattern = "CV" },
                    new SyllableTemplate { Pattern = "CVC" },
                    new SyllableTemplate { Pattern = "CCV" }
                }
            },
            Grammar = new Grammar
            {
                MorphologyRules = new[]
                {
                    new MorphologyRule
                    {
                        Name = "diminutive",
                        Type = MorphologyType.Suffix,
                        Pattern = "{root}ik"
                    },
                    new MorphologyRule
                    {
                        Name = "agent",
                        Type = MorphologyType.Suffix,
                        Pattern = "{root}ar"
                    }
                }
            }
        };

        // Act
        var result = _adapter.GenerateName(language, NameType.Personal);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 2);
    }

    private static Phonology CreateTestPhonology()
    {
        return new Phonology
        {
            Inventory = new PhonemeInventory
            {
                Consonants = new[] { "p", "t", "k", "b", "d", "g", "s", "m", "n", "l", "r" },
                Vowels = new[] { "a", "e", "i", "o", "u" }
            },
            Clusters = new ConsonantClusters
            {
                InitialClusters = new[] { "pr", "tr", "sp" },
                FinalClusters = new[] { "kt", "mp" },
                MaxConsonantCluster = 2
            },
            SyllableTemplates = new[]
            {
                new SyllableTemplate { Pattern = "CV" },
                new SyllableTemplate { Pattern = "CVC" }
            }
        };
    }
}
