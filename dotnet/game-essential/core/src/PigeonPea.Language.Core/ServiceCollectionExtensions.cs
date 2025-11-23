using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts;

namespace PigeonPea.Language.Core;

/// <summary>
/// Extension methods for registering PigeonPea Language services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PigeonPea Language services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Optional configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPigeonPeaLanguage(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // Register core services
        services.AddLogging();

        // Register configuration if provided
        if (configuration != null)
        {
            services.Configure<NameGenerationConfiguration>(
                configuration.GetSection("PigeonPea:Language:NameGeneration"));
        }
        else
        {
            // Register default configuration
            services.Configure<NameGenerationConfiguration>(config => { });
        }

        // Register language services
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddSingleton<INameGeneratorAdapter, LanguageToFMGAdapter>();

        // Register repository (singleton for shared state)
        services.AddSingleton<LanguageDefinitionRepository>();

        // Register core engines
        services.AddSingleton<PhonologyEngine>();
        services.AddSingleton<LexiconManager>();
        services.AddSingleton<GrammarEngine>();
        services.AddSingleton<SoundChangeEngine>();

        return services;
    }

    /// <summary>
    /// Adds PigeonPea Language services with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPigeonPeaLanguage(
        this IServiceCollection services,
        Action<NameGenerationConfiguration> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        // Register configuration
        services.Configure(configure);

        // Register services
        return services.AddPigeonPeaLanguage();
    }

    /// <summary>
    /// Adds PigeonPea Language services with specific configuration instance
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPigeonPeaLanguage(
        this IServiceCollection services,
        NameGenerationConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Register configuration
        services.Configure<NameGenerationConfiguration>(config =>
        {
            config.DefaultMode = configuration.DefaultMode;
            config.CustomTemplateMappings = new Dictionary<string, string>(configuration.CustomTemplateMappings);
            config.EnableTemplateCache = configuration.EnableTemplateCache;
            config.MarkovCorpusPath = configuration.MarkovCorpusPath;
            config.MaxCacheSize = configuration.MaxCacheSize;
            config.EnableDetailedLogging = configuration.EnableDetailedLogging;
            config.DefaultSeed = configuration.DefaultSeed;
            config.MaxGenerationAttempts = configuration.MaxGenerationAttempts;
            config.EnablePhonemeWeighting = configuration.EnablePhonemeWeighting;
            config.CustomPhonemeWeights = new Dictionary<string, Dictionary<char, double>>(configuration.CustomPhonemeWeights);
        });

        // Register services
        return services.AddPigeonPeaLanguage();
    }
}
