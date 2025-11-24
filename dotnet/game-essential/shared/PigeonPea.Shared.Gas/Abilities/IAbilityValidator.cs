using PigeonPea.Shared.Gas.Attributes;
using PigeonPea.Shared.Gas.Tags;

namespace PigeonPea.Shared.Gas.Abilities;

/// <summary>
/// Interface for custom ability validation logic.
/// Implement this to add game-specific validation rules.
/// </summary>
public interface IAbilityValidator
{
    /// <summary>
    /// Validates if an ability can be activated.
    /// Returns true if valid, false otherwise.
    /// </summary>
    /// <param name="ability">The ability being validated</param>
    /// <param name="casterAttributes">Caster's attribute set</param>
    /// <param name="casterTags">Caster's active tags</param>
    /// <param name="reason">Validation failure reason (if applicable)</param>
    bool CanActivate(
        AbilityDefinition ability,
        AttributeSet casterAttributes,
        TagSet casterTags,
        out string reason);
}
