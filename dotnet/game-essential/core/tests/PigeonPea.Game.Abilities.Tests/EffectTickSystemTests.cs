using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using FluentAssertions;
using PigeonPea.Gas.Attributes;
using PigeonPea.Gas.Effects;
using PigeonPea.Gas.Tags;
using PigeonPea.Game.Abilities.Components;
using PigeonPea.Game.Abilities.Systems;
using Xunit;

namespace PigeonPea.Game.Abilities.Tests;

public class EffectTickSystemTests
{
    [Fact]
    public void PeriodicEffect_TicksDamageAndExpires()
    {
        using var world = World.Create();

        var entity = world.Create();
        entity.Add(new AbilitySystemComponent());
        entity.Add(new ActiveEffectsComponent());

        ref var asc = ref entity.Get<AbilitySystemComponent>();
        ref var effects = ref entity.Get<ActiveEffectsComponent>();

        asc.Attributes.SetBaseValue("Health", 100f);

        var poisonEffect = new GameplayEffect
        {
            Id = "Poison",
            Name = "Poison",
            Description = "Deal 5 damage every second for 10 seconds",
            DurationPolicy = EffectDurationPolicy.Periodic,
            DurationSeconds = 10f,
            PeriodSeconds = 1f,
            Modifiers = new List<EffectModifier>
            {
                new(new AttributeModifier("Health", ModifierOperation.Add, -5f), applyOnTick: true)
            }
        };

        // Add active effect to entity
        var activePoison = new ActiveEffect(poisonEffect, sourceId: "TestCaster");
        effects.Effects.Add(activePoison);

        // After 1 second: 1 tick of -5 damage
        EffectTickSystem.Update(world, 1f);
        asc.Attributes.GetBaseValue("Health").Should().Be(95f);
        effects.Effects.Should().HaveCount(1);

        // After 4 more seconds: total 5 ticks => 25 damage
        EffectTickSystem.Update(world, 4f);
        asc.Attributes.GetBaseValue("Health").Should().Be(75f);
        effects.Effects.Should().HaveCount(1);

        // After 5 more seconds: total 10 ticks => 50 damage, effect expires and is removed
        EffectTickSystem.Update(world, 5f);
        asc.Attributes.GetBaseValue("Health").Should().Be(50f);
        effects.Effects.Should().BeEmpty();
    }

    [Fact]
    public void DurationEffect_RemovesPersistentModifierAndTagsOnExpiry()
    {
        using var world = World.Create();

        var entity = world.Create();
        entity.Add(new AbilitySystemComponent());
        entity.Add(new ActiveEffectsComponent());

        ref var asc = ref entity.Get<AbilitySystemComponent>();
        ref var effects = ref entity.Get<ActiveEffectsComponent>();

        asc.Attributes.SetBaseValue("Attack", 10f);

        var buffTag = new GameplayTag("State.Buff.Might");

        var buffEffect = new GameplayEffect
        {
            Id = "MightBuff",
            Name = "Might",
            Description = "Increase attack by 5 for 5 seconds",
            DurationPolicy = EffectDurationPolicy.Duration,
            DurationSeconds = 5f,
            Modifiers = new List<EffectModifier>
            {
                // Persistent modifier (ApplyOnTick = false)
                new(new AttributeModifier("Attack", ModifierOperation.Add, 5f))
            },
            GrantedTags = new List<GameplayTag> { buffTag }
        };

        // Simulate AbilityExecutionSystem applying persistent modifier and tag
        var persistentModifier = new AttributeModifier("Attack", ModifierOperation.Add, 5f, buffEffect.Id);
        asc.Attributes.AddModifier(persistentModifier);
        asc.ActiveTags.AddTag(buffTag);

        var activeBuff = new ActiveEffect(buffEffect, sourceId: "TestCaster");
        effects.Effects.Add(activeBuff);

        // While buff is active, attack should be increased
        asc.Attributes.GetCurrentValue("Attack").Should().Be(15f);
        asc.ActiveTags.HasTag(buffTag, TagMatchType.Exact).Should().BeTrue();
        effects.Effects.Should().HaveCount(1);

        // After 5 seconds, buff expires: modifier and tag should be removed
        EffectTickSystem.Update(world, 5f);

        asc.Attributes.GetCurrentValue("Attack").Should().Be(10f);
        asc.ActiveTags.HasTag(buffTag, TagMatchType.Exact).Should().BeFalse();
        effects.Effects.Should().BeEmpty();
    }

    [Fact]
    public void PeriodicHealingEffect_TicksHealingAndExpires()
    {
        using var world = World.Create();

        var entity = world.Create();
        entity.Add(new AbilitySystemComponent());
        entity.Add(new ActiveEffectsComponent());

        ref var asc = ref entity.Get<AbilitySystemComponent>();
        ref var effects = ref entity.Get<ActiveEffectsComponent>();

        asc.Attributes.SetBaseValue("Health", 50f);

        var regenEffect = new GameplayEffect
        {
            Id = "Regeneration",
            Name = "Regeneration",
            Description = "Heal 5 health every second for 10 seconds",
            DurationPolicy = EffectDurationPolicy.Periodic,
            DurationSeconds = 10f,
            PeriodSeconds = 1f,
            Modifiers = new List<EffectModifier>
            {
                new(new AttributeModifier("Health", ModifierOperation.Add, 5f), applyOnTick: true)
            }
        };

        // Add active effect to entity
        var activeRegen = new ActiveEffect(regenEffect, sourceId: "TestCaster");
        effects.Effects.Add(activeRegen);

        // After 1 second: 1 tick of +5 healing
        EffectTickSystem.Update(world, 1f);
        asc.Attributes.GetBaseValue("Health").Should().Be(55f);
        effects.Effects.Should().HaveCount(1);

        // After 4 more seconds: total 5 ticks => +25 healing
        EffectTickSystem.Update(world, 4f);
        asc.Attributes.GetBaseValue("Health").Should().Be(75f);
        effects.Effects.Should().HaveCount(1);

        // After 5 more seconds: total 10 ticks => +50 healing, effect expires and is removed
        EffectTickSystem.Update(world, 5f);
        asc.Attributes.GetBaseValue("Health").Should().Be(100f);
        effects.Effects.Should().BeEmpty();
    }
}
