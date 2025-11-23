namespace PigeonPea.Gas.Abilities;

/// <summary>
/// Defines when an ability can be activated.
/// </summary>
public enum AbilityActivationPolicy
{
    /// <summary>Can be activated any time requirements are met</summary>
    Always,

    /// <summary>Can only be activated on the caster's turn (turn-based games)</summary>
    OnTurn,

    /// <summary>Can only be activated as a reaction to an event</summary>
    OnEvent,

    /// <summary>Can only be activated while channeling</summary>
    WhileChanneling
}
