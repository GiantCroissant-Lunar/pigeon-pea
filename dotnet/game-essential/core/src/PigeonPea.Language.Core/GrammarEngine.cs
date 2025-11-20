using Microsoft.Extensions.Logging;
using PigeonPea.Language.Contracts.Grammar;

namespace PigeonPea.Language.Core;

public class GrammarEngine : IGrammarEngine
{
    private readonly ILogger<GrammarEngine> _logger;

    public GrammarEngine(ILogger<GrammarEngine> logger)
    {
        _logger = logger;
    }

    public string[] ApplyWordOrder(string[] words, WordOrder order)
    {
        if (words == null || words.Length == 0)
        {
            return Array.Empty<string>();
        }

        // For proper word order transformation, we need to identify S, V, O
        // This is a simplified implementation that assumes:
        // - words[0] = Subject
        // - words[1] = Verb
        // - words[2] = Object (if exists)

        if (words.Length < 2)
        {
            return words;
        }

        var subject = words[0];
        var verb = words[1];
        var obj = words.Length > 2 ? words[2] : null;
        var remaining = words.Length > 3 ? words.Skip(3).ToArray() : Array.Empty<string>();

        var result = order switch
        {
            WordOrder.SVO => BuildSentence(subject, verb, obj, remaining),
            WordOrder.SOV => BuildSentence(subject, obj, verb, remaining),
            WordOrder.VSO => BuildSentence(verb, subject, obj, remaining),
            WordOrder.VOS => BuildSentence(verb, obj, subject, remaining),
            WordOrder.OVS => BuildSentence(obj, verb, subject, remaining),
            WordOrder.OSV => BuildSentence(obj, subject, verb, remaining),
            _ => words
        };

        _logger.LogDebug("Applied word order {Order}: [{Original}] -> [{Result}]",
            order, string.Join(", ", words), string.Join(", ", result));

        return result;
    }

    public string ApplyMorphology(string root, MorphologyRule rule)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("Root cannot be null or empty", nameof(root));
        }

        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            _logger.LogWarning("Morphology rule pattern is empty");
            return root;
        }

        // Replace {root} placeholder with actual root
        var result = rule.Pattern.Replace("{root}", root, StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("Applied morphology rule '{RuleName}' ({Type}): {Root} -> {Result}",
            rule.Name, rule.Type, root, result);

        return result;
    }

    public string FormCompound(string[] roots, CompoundRule rule)
    {
        if (roots == null || roots.Length == 0)
        {
            throw new ArgumentException("Roots cannot be null or empty", nameof(roots));
        }

        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            _logger.LogWarning("Compound rule pattern is empty");
            return string.Join("", roots);
        }

        var result = rule.Pattern;

        // Replace {root1}, {root2}, etc. with actual roots
        for (int i = 0; i < roots.Length; i++)
        {
            var placeholder = $"{{root{i + 1}}}";
            result = result.Replace(placeholder, roots[i], StringComparison.OrdinalIgnoreCase);
        }

        // If there's a connector and it wasn't in the pattern, add it between roots
        if (!string.IsNullOrEmpty(rule.Connector) && !rule.Pattern.Contains(rule.Connector, StringComparison.Ordinal))
        {
            result = string.Join(rule.Connector, roots);
        }

        _logger.LogDebug("Formed compound ({Type}): [{Roots}] -> {Result}",
            rule.Type, string.Join(", ", roots), result);

        return result;
    }

    public bool ValidateGrammar(GrammarRules rules)
    {
        if (rules == null)
        {
            _logger.LogWarning("Grammar rules are null");
            return false;
        }

        // Validate morphology rules
        if (rules.MorphologyRules != null)
        {
            foreach (var rule in rules.MorphologyRules)
            {
                if (string.IsNullOrWhiteSpace(rule.Name))
                {
                    _logger.LogWarning("Morphology rule has empty name");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(rule.Pattern))
                {
                    _logger.LogWarning("Morphology rule '{Name}' has empty pattern", rule.Name);
                    return false;
                }

                if (!rule.Pattern.Contains("{root}", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Morphology rule '{Name}' pattern does not contain {{root}} placeholder", rule.Name);
                    return false;
                }
            }
        }

        // Validate compound rules
        if (rules.CompoundRules != null)
        {
            foreach (var rule in rules.CompoundRules)
            {
                if (string.IsNullOrWhiteSpace(rule.Pattern))
                {
                    _logger.LogWarning("Compound rule has empty pattern");
                    return false;
                }

                // Check if pattern contains at least {root1}
                if (!rule.Pattern.Contains("{root1}", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Compound rule pattern does not contain {{root1}} placeholder");
                    return false;
                }
            }
        }

        return true;
    }

    private static string[] BuildSentence(string? part1, string? part2, string? part3, string[] remaining)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(part1))
        {
            parts.Add(part1);
        }

        if (!string.IsNullOrEmpty(part2))
        {
            parts.Add(part2);
        }

        if (!string.IsNullOrEmpty(part3))
        {
            parts.Add(part3);
        }

        if (remaining.Length > 0)
        {
            parts.AddRange(remaining);
        }

        return parts.ToArray();
    }
}
