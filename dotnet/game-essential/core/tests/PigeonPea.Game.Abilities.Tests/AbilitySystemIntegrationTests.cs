using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using FluentAssertions;
using MessagePipe;
using PigeonPea.Shared.Gas.Abilities;
using PigeonPea.Shared.Gas.Attributes;
using PigeonPea.Shared.Gas.Effects;
using PigeonPea.Shared.Gas.Tags;
using PigeonPea.Game.Abilities.Components;
using PigeonPea.Game.Abilities.Events;
using PigeonPea.Game.Abilities.Integration;
using PigeonPea.Game.Abilities.Systems;
using Xunit;

namespace PigeonPea.Game.Abilities.Tests;

public class AbilitySystemIntegrationTests
{
    private sealed class TestPublisher<T> : IPublisher<T>
    {
        public List<T> Published { get; } = new();

        public void Publish(T message)
        {
            Published.Add(message);
        }
    }

    [Fact]
    public void TryCastAbility_AppliesCostDamageAndCooldown()
    {
        using var world = World.Create();

        var caster = world.Create();
        var target = world.Create();

        var firebolt = CreateFireboltAbility();

        // Give ability to caster
        world.GiveAbility(caster, firebolt);

        // Initialize attributes
        ref var casterAsc = ref caster.Get<AbilitySystemComponent>();
        casterAsc.Attributes.SetBaseValue("Mana", 50f);

        if (!target.Has<AbilitySystemComponent>())
        {
            target.Add(new AbilitySystemComponent());
        }

        ref var targetAsc = ref target.Get<AbilitySystemComponent>();
        targetAsc.Attributes.SetBaseValue("Health", 100f);

        var castPublisher = new TestPublisher<AbilityCastEvent>();
        var failedPublisher = new TestPublisher<AbilityCastFailedEvent>();
        var effectPublisher = new TestPublisher<EffectAppliedEvent>();

        // Act
        var success = world.TryCastAbility(
            caster,
            firebolt.Id,
            target,
            currentTime: 0f,
            abilityCastPublisher: castPublisher,
            castFailedPublisher: failedPublisher,
            effectAppliedPublisher: effectPublisher);

        // Assert
        success.Should().BeTrue();
        failedPublisher.Published.Should().BeEmpty();
        castPublisher.Published.Should().HaveCount(1);
        effectPublisher.Published.Should().NotBeEmpty();

        // Cost should be applied to caster mana: 50 - 10 = 40
        casterAsc.Attributes.GetBaseValue("Mana").Should().Be(40f);

        // Damage should be applied to target health: 100 - 25 = 75
        targetAsc.Attributes.GetBaseValue("Health").Should().Be(75f);

        // Cooldown should be started
        casterAsc.CooldownTimers[firebolt.Id].Should().Be(firebolt.CooldownSeconds);
    }

    [Fact]
    public void CooldownSystem_PreventsRecastUntilExpired()
    {
        using var world = World.Create();

        var caster = world.Create();
        var target = world.Create();

        var firebolt = CreateFireboltAbility();
        world.GiveAbility(caster, firebolt);

        ref var casterAsc = ref caster.Get<AbilitySystemComponent>();
        casterAsc.Attributes.SetBaseValue("Mana", 50f);

        if (!target.Has<AbilitySystemComponent>())
        {
            target.Add(new AbilitySystemComponent());
        }

        ref var targetAsc = ref target.Get<AbilitySystemComponent>();
        targetAsc.Attributes.SetBaseValue("Health", 100f);

        var castPublisher = new TestPublisher<AbilityCastEvent>();
        var failedPublisher = new TestPublisher<AbilityCastFailedEvent>();
        var effectPublisher = new TestPublisher<EffectAppliedEvent>();

        // First cast: should succeed
        var t0 = 0f;
        var success1 = world.TryCastAbility(
            caster,
            firebolt.Id,
            target,
            currentTime: t0,
            abilityCastPublisher: castPublisher,
            castFailedPublisher: failedPublisher,
            effectAppliedPublisher: effectPublisher);

        success1.Should().BeTrue();

        // Advance time by half the cooldown
        CooldownSystem.Update(world, firebolt.CooldownSeconds / 2f);

        // Second cast: should fail due to cooldown
        var t1 = firebolt.CooldownSeconds / 2f;
        var success2 = world.TryCastAbility(
            caster,
            firebolt.Id,
            target,
            currentTime: t1,
            abilityCastPublisher: castPublisher,
            castFailedPublisher: failedPublisher,
            effectAppliedPublisher: effectPublisher);

        success2.Should().BeFalse();
        failedPublisher.Published.Should().NotBeEmpty();

        // Advance time by remaining cooldown
        CooldownSystem.Update(world, firebolt.CooldownSeconds / 2f);

        // Third cast: cooldown expired, should succeed
        var t2 = firebolt.CooldownSeconds;
        var success3 = world.TryCastAbility(
            caster,
            firebolt.Id,
            target,
            currentTime: t2,
            abilityCastPublisher: castPublisher,
            castFailedPublisher: failedPublisher,
            effectAppliedPublisher: effectPublisher);

        success3.Should().BeTrue();
    }

    private static AbilityDefinition CreateFireboltAbility()
    {
        var damageEffect = new GameplayEffect
        {
            Id = "Firebolt_Damage",
            Name = "Firebolt Damage",
            Description = "Deal 25 fire damage to target",
            DurationPolicy = EffectDurationPolicy.Instant,
            Modifiers = new List<EffectModifier>
            {
                new(new AttributeModifier("Health", ModifierOperation.Add, -25f))
            }
        };

        return new AbilityDefinition
        {
            Id = "Firebolt",
            Name = "Firebolt",
            Description = "Hurl a bolt of fire at an enemy",
            CooldownSeconds = 2f,
            Cost = new AbilityCost
            {
                Modifiers = new List<AttributeModifier>
                {
                    new("Mana", ModifierOperation.Add, -10f)
                }
            },
            ActivationRequiredTags = new List<GameplayTag>
            {
                new("State.Alive")
            },
            ActivationBlockedTags = new List<GameplayTag>
            {
                new("State.Stunned"),
                new("State.Silenced")
            },
            Targeting = new AbilityTargeting
            {
                Type = TargetingType.SingleTarget,
                Range = 10f,
                RequiresLineOfSight = false,
                CanTargetEnemies = true,
                CanTargetSelf = false
            },
            Effects = new List<GameplayEffect> { damageEffect }
        };
    }
}
