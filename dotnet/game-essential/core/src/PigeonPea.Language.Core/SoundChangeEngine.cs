using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts.Models;
using PigeonPea.Language.Contracts.SoundChange;
using System.Text.RegularExpressions;

namespace PigeonPea.Language.Core;

public class SoundChangeEngine : ISoundChangeEngine
{
    private readonly ILogger<SoundChangeEngine> _logger;

    public SoundChangeEngine(ILogger<SoundChangeEngine> logger)
    {
        _logger = logger;
    }

    public string ApplySoundChange(string word, SoundChangeRule rule)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return word;
        }

        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        if (string.IsNullOrWhiteSpace(rule.SourcePhoneme))
        {
            _logger.LogWarning("Sound change rule '{Name}' has empty source phoneme", rule.Name);
            return word;
        }

        if (string.IsNullOrWhiteSpace(rule.TargetPhoneme))
        {
            _logger.LogWarning("Sound change rule '{Name}' has empty target phoneme", rule.Name);
            return word;
        }

        // Apply sound change based on context
        var result = string.IsNullOrWhiteSpace(rule.Context)
            ? ApplyUnconditionalChange(word, rule)
            : ApplyContextualChange(word, rule);

        if (result != word)
        {
            _logger.LogDebug("Applied sound change '{Name}': {Original} → {Result}",
                rule.Name, word, result);
        }

        return result;
    }

    public IEnumerable<string> ApplySoundChanges(IEnumerable<string> words, IEnumerable<SoundChangeRule> rules)
    {
        if (words == null)
        {
            throw new ArgumentNullException(nameof(words));
        }

        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        // Sort rules by order
        var sortedRules = rules.OrderBy(r => r.Order).ToList();

        var results = new List<string>();

        foreach (var word in words)
        {
            var transformedWord = word;

            // Apply each rule in order
            foreach (var rule in sortedRules)
            {
                transformedWord = ApplySoundChange(transformedWord, rule);
            }

            results.Add(transformedWord);
        }

        _logger.LogDebug("Applied {RuleCount} sound change rules to {WordCount} words",
            sortedRules.Count, results.Count);

        return results;
    }

    public LanguageDefinition DeriveLanguage(LanguageDefinition parent, IEnumerable<SoundChangeRule> rules)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        _logger.LogInformation("Deriving daughter language from '{ParentLanguage}'", parent.Name);

        // Create a new language definition based on the parent
        var daughterLanguage = parent with
        {
            Id = $"{parent.Id}-derived",
            Name = $"{parent.Name} (Derived)",
            ParentLanguageId = parent.Id,
            SoundChanges = rules.ToList()
        };

        // Note: In a full implementation, we would also:
        // 1. Transform the lexicon by applying sound changes to all words
        // 2. Update phoneme inventory if sound changes introduce new phonemes or remove old ones
        // 3. Update phonotactic rules if sound changes affect allowed clusters

        // For now, we return the derived language with the sound change rules attached
        // The actual lexicon transformation would happen when the lexicon is loaded

        _logger.LogInformation("Derived language '{DaughterLanguage}' with {RuleCount} sound change rules",
            daughterLanguage.Name, rules.Count());

        return daughterLanguage;
    }

    private string ApplyUnconditionalChange(string word, SoundChangeRule rule)
    {
        // Simple replacement: source → target everywhere
        return word.Replace(rule.SourcePhoneme, rule.TargetPhoneme, StringComparison.Ordinal);
    }

    private string ApplyContextualChange(string word, SoundChangeRule rule)
    {
        // Parse context specification
        // Common patterns:
        // "_a"  = before 'a'
        // "a_"  = after 'a'
        // "a_b" = between 'a' and 'b'
        // "#_"  = word-initial
        // "_#"  = word-final

        var context = rule.Context!;
        var source = rule.SourcePhoneme;
        var target = rule.TargetPhoneme;

        // Word-initial: #_
        if (context == "#_")
        {
            if (word.StartsWith(source, StringComparison.Ordinal))
            {
                return target + word.Substring(source.Length);
            }
            return word;
        }

        // Word-final: _#
        if (context == "_#")
        {
            if (word.EndsWith(source, StringComparison.Ordinal))
            {
                return word.Substring(0, word.Length - source.Length) + target;
            }
            return word;
        }

        // Before phoneme: _X
        if (context.StartsWith('_'))
        {
            var followingPhoneme = context.Substring(1);
            var pattern = $"{Regex.Escape(source)}(?={Regex.Escape(followingPhoneme)})";
            return Regex.Replace(word, pattern, target);
        }

        // After phoneme: X_
        if (context.EndsWith('_'))
        {
            var precedingPhoneme = context.Substring(0, context.Length - 1);
            var pattern = $"(?<={Regex.Escape(precedingPhoneme)}){Regex.Escape(source)}";
            return Regex.Replace(word, pattern, target);
        }

        // Between phonemes: X_Y
        if (context.Contains('_', StringComparison.Ordinal))
        {
            var parts = context.Split('_');
            if (parts.Length == 2)
            {
                var before = parts[0];
                var after = parts[1];
                var pattern = $"(?<={Regex.Escape(before)}){Regex.Escape(source)}(?={Regex.Escape(after)})";
                return Regex.Replace(word, pattern, target);
            }
        }

        // If context doesn't match known patterns, log warning and apply unconditionally
        _logger.LogWarning("Unknown context pattern '{Context}' in rule '{Name}', applying unconditionally",
            context, rule.Name);
        return ApplyUnconditionalChange(word, rule);
    }
}
