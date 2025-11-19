using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts.Grammar;
using PigeonPea.Language.Contracts.Lexicon;
using PigeonPea.Language.Contracts.Models;
using System.Text.RegularExpressions;

namespace PigeonPea.Language.Core;

public class TranslationEngine
{
    private readonly LexiconManager _lexiconManager;
    private readonly GrammarEngine _grammarEngine;
    private readonly NameGenerator _nameGenerator;
    private readonly ILogger<TranslationEngine> _logger;

    public TranslationEngine(
        LexiconManager lexiconManager,
        GrammarEngine grammarEngine,
        NameGenerator nameGenerator,
        ILogger<TranslationEngine> logger)
    {
        _lexiconManager = lexiconManager ?? throw new ArgumentNullException(nameof(lexiconManager));
        _grammarEngine = grammarEngine ?? throw new ArgumentNullException(nameof(grammarEngine));
        _nameGenerator = nameGenerator ?? throw new ArgumentNullException(nameof(nameGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string TranslateToFantasy(string englishText, string targetLanguageId, LanguageDefinition targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(englishText))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(targetLanguageId))
        {
            throw new ArgumentException("Target language ID cannot be null or empty", nameof(targetLanguageId));
        }

        if (targetLanguage == null)
        {
            throw new ArgumentNullException(nameof(targetLanguage));
        }

        _logger.LogDebug("Translating to {Language}: {Text}", targetLanguageId, englishText);

        // Tokenize English text
        var tokens = TokenizeEnglish(englishText);

        // Translate each token
        var translatedTokens = new List<string>();
        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            // Look up in lexicon (lowercase for case-insensitive matching)
#pragma warning disable CA1308 // Normalize strings to uppercase
            var entry = _lexiconManager.LookupByMeaning(targetLanguageId, token.ToLowerInvariant());
#pragma warning restore CA1308

            if (entry != null)
            {
                translatedTokens.Add(entry.Word);
            }
            else
            {
                // Word not in lexicon - generate a phonologically appropriate word
                _logger.LogDebug("Word '{Word}' not in lexicon, generating phonologically appropriate word", token);
                
                var generatedWord = _nameGenerator.GenerateName(new Contracts.NameGenerationOptions
                {
                    MinSyllables = 1,
                    MaxSyllables = 3,
                    Type = Contracts.NameType.Item,
#pragma warning disable CA1307 // Specify StringComparison for clarity
                    Seed = token.GetHashCode() // Use word hash as seed for consistency
#pragma warning restore CA1307
                });

#pragma warning disable CA1308 // Normalize strings to uppercase
                translatedTokens.Add(generatedWord.ToLowerInvariant());
#pragma warning restore CA1308
            }
        }

        // Apply grammar transformation
        if (translatedTokens.Count > 0)
        {
            var reordered = _grammarEngine.ApplyWordOrder(
                translatedTokens.ToArray(),
                targetLanguage.Grammar.WordOrder);

            var result = string.Join(" ", reordered);
            
            _logger.LogDebug("Translation result: {Result}", result);
            
            return result;
        }

        return string.Empty;
    }

    public string TranslateToEnglish(string fantasyText, string sourceLanguageId, LanguageDefinition sourceLanguage)
    {
        if (string.IsNullOrWhiteSpace(fantasyText))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(sourceLanguageId))
        {
            throw new ArgumentException("Source language ID cannot be null or empty", nameof(sourceLanguageId));
        }

        if (sourceLanguage == null)
        {
            throw new ArgumentNullException(nameof(sourceLanguage));
        }

        _logger.LogDebug("Translating from {Language}: {Text}", sourceLanguageId, fantasyText);

        // Tokenize fantasy text (morphology-aware)
        var tokens = TokenizeFantasy(fantasyText, sourceLanguage);

        // Translate each token
        var translatedTokens = new List<string>();
        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            // Look up in reverse lexicon (lowercase for case-insensitive matching)
#pragma warning disable CA1308 // Normalize strings to uppercase
            var meaning = _lexiconManager.LookupByWord(sourceLanguageId, token.ToLowerInvariant());
#pragma warning restore CA1308

            if (meaning != null)
            {
                translatedTokens.Add(meaning);
            }
            else
            {
                // Word not in lexicon - preserve original and mark as unknown
                _logger.LogDebug("Word '{Word}' not in lexicon, preserving original", token);
                translatedTokens.Add($"[{token}]");
            }
        }

        // Apply reverse grammar transformation (convert to English SVO)
        if (translatedTokens.Count > 0)
        {
            // First, reverse the source language word order to get back to SVO
            var reordered = ReverseWordOrder(
                translatedTokens.ToArray(),
                sourceLanguage.Grammar.WordOrder);

            var result = string.Join(" ", reordered);
            
            _logger.LogDebug("Translation result: {Result}", result);
            
            return result;
        }

        return string.Empty;
    }

    private static string[] TokenizeEnglish(string text)
    {
        // Simple tokenization: split on whitespace and punctuation
        var tokens = Regex.Split(text, @"[\s\p{P}]+")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        return tokens;
    }

    private static string[] TokenizeFantasy(string text, LanguageDefinition language)
    {
        // For now, use simple whitespace tokenization
        // A full implementation would parse based on morphological boundaries
        var tokens = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // TODO: Implement morphology-aware tokenization
        // This would split words at morphological boundaries based on language.Grammar.MorphologyRules

        return tokens;
    }

    private string[] ReverseWordOrder(string[] words, WordOrder sourceOrder)
    {
        // Convert from source language word order back to English SVO
        if (words.Length < 2)
        {
            return words;
        }

        // Map the source order back to SVO
        return sourceOrder switch
        {
            WordOrder.SVO => words, // Already SVO
            WordOrder.SOV => ReverseSOVtoSVO(words),
            WordOrder.VSO => ReverseVSOtoSVO(words),
            WordOrder.VOS => ReverseVOStoSVO(words),
            WordOrder.OVS => ReverseOVStoSVO(words),
            WordOrder.OSV => ReverseOSVtoSVO(words),
            _ => words
        };
    }

    private static string[] ReverseSOVtoSVO(string[] words)
    {
        // SOV: Subject-Object-Verb → SVO: Subject-Verb-Object
        if (words.Length < 3)
        {
            return words;
        }

        var subject = words[0];
        var obj = words[1];
        var verb = words[2];
        var remaining = words.Length > 3 ? words.Skip(3).ToArray() : Array.Empty<string>();

        return BuildSentence(subject, verb, obj, remaining);
    }

    private static string[] ReverseVSOtoSVO(string[] words)
    {
        // VSO: Verb-Subject-Object → SVO: Subject-Verb-Object
        if (words.Length < 3)
        {
            return words;
        }

        var verb = words[0];
        var subject = words[1];
        var obj = words[2];
        var remaining = words.Length > 3 ? words.Skip(3).ToArray() : Array.Empty<string>();

        return BuildSentence(subject, verb, obj, remaining);
    }

    private static string[] ReverseVOStoSVO(string[] words)
    {
        // VOS: Verb-Object-Subject → SVO: Subject-Verb-Object
        if (words.Length < 3)
        {
            return words;
        }

        var verb = words[0];
        var obj = words[1];
        var subject = words[2];
        var remaining = words.Length > 3 ? words.Skip(3).ToArray() : Array.Empty<string>();

        return BuildSentence(subject, verb, obj, remaining);
    }

    private static string[] ReverseOVStoSVO(string[] words)
    {
        // OVS: Object-Verb-Subject → SVO: Subject-Verb-Object
        if (words.Length < 3)
        {
            return words;
        }

        var obj = words[0];
        var verb = words[1];
        var subject = words[2];
        var remaining = words.Length > 3 ? words.Skip(3).ToArray() : Array.Empty<string>();

        return BuildSentence(subject, verb, obj, remaining);
    }

    private static string[] ReverseOSVtoSVO(string[] words)
    {
        // OSV: Object-Subject-Verb → SVO: Subject-Verb-Object
        if (words.Length < 3)
        {
            return words;
        }

        var obj = words[0];
        var subject = words[1];
        var verb = words[2];
        var remaining = words.Length > 3 ? words.Skip(3).ToArray() : Array.Empty<string>();

        return BuildSentence(subject, verb, obj, remaining);
    }

    private static string[] BuildSentence(string subject, string verb, string obj, string[] remaining)
    {
        var parts = new List<string> { subject, verb, obj };

        if (remaining.Length > 0)
        {
            parts.AddRange(remaining);
        }

        return parts.ToArray();
    }
}
