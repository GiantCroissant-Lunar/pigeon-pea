using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared.Goap.WorldState;
using PigeonPea.Shared.Perception.Enums;
using PigeonPea.Shared.Perception.Models;
using PigeonPea.Shared.Gas.Attributes;
using PigeonPea.Game.Abilities.Components;
using PigeonPea.Game.Perception.Components;
using PigeonPea.Shared.ECS.Components;
using SharedComponents = PigeonPea.Shared.Components;

namespace PigeonPea.Game.AI.Adapters;

/// <summary>
/// Converts PerceptionData (from Nexus-Perception) to GOAP WorldState (for Nexus-GOAP).
/// This bridges the perception and planning layers.
/// </summary>
public static class PerceptionToWorldStateAdapter
{
    /// <summary>
    /// Converts perception data plus ECS self entity state into a GOAP world state snapshot.
    /// </summary>
    public static WorldState Convert(PerceptionData perception, Entity self)
    {
        var state = new WorldState();

        // === VISUAL PERCEPTION ===
        // Player visibility
        var visiblePlayer = perception.Visual.GetClosestEntity("Player");
        state = state.Set("PlayerVisible", visiblePlayer != null);

        if (visiblePlayer != null)
        {
            state = state.Set("PlayerDistance", visiblePlayer.Distance);
            if (visiblePlayer.Health.HasValue)
            {
                state = state.Set("PlayerHealth", visiblePlayer.Health.Value);
            }
            state = state.Set("PlayerDirection", (int)visiblePlayer.DirectionFromSelf);
        }

        // Visible enemies
        var visibleEnemies = perception.Visual.GetEntitiesOfType("Enemy");
        state = state.Set("VisibleEnemyCount", visibleEnemies.Count);
        state = state.Set("HasVisibleEnemies", visibleEnemies.Count > 0);

        // Visible items (generic)
        var visibleItems = perception.Visual.GetEntitiesOfType("Item");
        state = state.Set("VisibleItemCount", visibleItems.Count);

        // === AUDITORY PERCEPTION ===
        state = state.Set("HeardFootsteps", perception.Auditory.HeardFootsteps());
        state = state.Set("HeardCombat", perception.Auditory.HeardCombat());

        var loudestSound = perception.Auditory.HeardSounds
            .OrderByDescending(s => s.Volume)
            .FirstOrDefault();
        if (loudestSound != null)
        {
            state = state.Set("LoudSoundDirection", (int)loudestSound.Direction);
            state = state.Set("LoudSoundDistance", loudestSound.Distance);
        }

        // === KNOWLEDGE / MEMORY ===
        state = state.Set("KnownEnemyCount", perception.Knowledge.KnownEnemies.Count);
        state = state.Set("HasKnownThreats", perception.Knowledge.KnownEnemies.Count > 0);

        // Do we know where the player was last seen? (by type string, if stored that way)
        // This is a heuristic; exact keying is higher-level game logic.
        var knowsAnyLastPosition = perception.Knowledge.LastKnownPositions.Count > 0;
        state = state.Set("HasLastKnownPositions", knowsAnyLastPosition);

        // Facts as generic flags
        foreach (var factKvp in perception.Knowledge.KnownFacts)
        {
            var key = $"Fact_{factKvp.Key}";
            state = state.Set(key, true);
        }

        // === AWARENESS ===
        state = state.Set("AlertLevel", (int)perception.Awareness.AlertLevel);
        state = state.Set("ThreatLevel", (int)perception.Awareness.ThreatLevel);
        state = state.Set("IsAlert", perception.Awareness.IsAlert());
        state = state.Set("IsSuspicious", perception.Awareness.IsSuspicious());
        state = state.Set("IsInDanger", perception.Awareness.IsInDanger());

        // Emotional state
        state = state.Set("EmotionalState", (int)perception.Awareness.EmotionalState);
        state = state.Set("IsAfraid", perception.Awareness.EmotionalState == EmotionalState.Afraid);
        state = state.Set("IsConfident", perception.Awareness.EmotionalState == EmotionalState.Confident);
        state = state.Set("IsAngry", perception.Awareness.EmotionalState == EmotionalState.Angry);
        state = state.Set("IsCurious", perception.Awareness.EmotionalState == EmotionalState.Curious);

        // Suspicious / primary target positions if known
        if (perception.Awareness.SuspiciousPosition.HasValue)
        {
            var pos = perception.Awareness.SuspiciousPosition.Value;
            state = state.Set("SuspiciousPosX", pos.X);
            state = state.Set("SuspiciousPosY", pos.Y);
        }

        if (perception.Awareness.PrimaryTargetPosition.HasValue)
        {
            var pos = perception.Awareness.PrimaryTargetPosition.Value;
            state = state.Set("PrimaryTargetPosX", pos.X);
            state = state.Set("PrimaryTargetPosY", pos.Y);
        }

        // Primary target flags
        var hasPrimaryTarget = perception.Awareness.PrimaryTarget != null
                                || perception.Awareness.PrimaryTargetPosition.HasValue;
        state = state.Set("HasPrimaryTarget", hasPrimaryTarget);

        bool primaryTargetIsPlayer = false;
        if (perception.Awareness.PrimaryTarget is Entity primaryTargetEntity)
        {
            primaryTargetIsPlayer = primaryTargetEntity.Has<SharedComponents.PlayerComponent>();
        }

        state = state.Set("PrimaryTargetIsPlayer", primaryTargetIsPlayer);

        // === SELF STATE (from ECS components) ===
        if (self.TryGet<Health>(out var health))
        {
            state = state.Set("SelfHealth", health.Current);
            state = state.Set("SelfMaxHealth", health.Maximum);
            var percent = health.Maximum > 0 ? (float)health.Current / health.Maximum : 0f;
            state = state.Set("SelfHealthPercent", percent);
            state = state.Set("IsLowHealth", percent < 0.3f);
        }

        if (self.TryGet<Position>(out var position))
        {
            state = state.Set("SelfX", position.X);
            state = state.Set("SelfY", position.Y);
        }

        // Inventory (from PigeonPea.Shared.Components.Inventory)
        if (self.TryGet<SharedComponents.Inventory>(out var inventory))
        {
            state = state.Set("HasInventory", true);
            state = state.Set("InventoryItemCount", inventory.Items.Count);

            // Check for a basic health potion by item name or type if present
            bool hasHealthPotion = inventory.Items.Any(e =>
            {
                if (!e.Has<SharedComponents.Item>())
                    return false;
                ref var item = ref e.Get<SharedComponents.Item>();
                return item.Name.Contains("potion", StringComparison.OrdinalIgnoreCase);
            });

            state = state.Set("HasHealthPotion", hasHealthPotion);
        }
        else
        {
            state = state.Set("HasInventory", false);
        }

        // Abilities (from AbilitySystemComponent)
        if (self.TryGet<AbilitySystemComponent>(out var abilities))
        {
            state = state.Set("HasAbilities", abilities.KnownAbilities.Count > 0);

            // Mana attribute (if defined)
            float mana = GetAttributeSafe(abilities.Attributes, "Mana");
            state = state.Set("Mana", mana);
            state = state.Set("HasMana", mana > 0f);

            // Specific abilities
            bool hasFireball = abilities.KnownAbilities.Any(a => a.Id == "Fireball");
            bool hasHeal = abilities.KnownAbilities.Any(a => a.Id == "Heal");
            state = state.Set("HasFireballAbility", hasFireball);
            state = state.Set("HasHealAbility", hasHeal);

            if (hasFireball && abilities.CooldownTimers != null)
            {
                float cd = 0f;
                abilities.CooldownTimers.TryGetValue("Fireball", out cd);
                state = state.Set("FireballReady", cd <= 0f);
            }
        }

        return state;
    }

    private static float GetAttributeSafe(AttributeSet attributes, string attributeId)
    {
        try
        {
            return attributes.GetCurrentValue(attributeId);
        }
        catch
        {
            return 0f;
        }
    }
}
