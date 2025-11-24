using System.Collections.Generic;
using FluentAssertions;
using PigeonPea.Shared.Gas.Attributes;
using PigeonPea.Shared.Gas.Effects;
using PigeonPea.Shared.Gas.Tags;
using Xunit;

namespace PigeonPea.Gas.Core.Tests.Effects;

public class GameplayEffectTests
{
    [Fact]
    public void InstantEffect_HasZeroDuration()
    {
        var effect = new GameplayEffect
        {
            Id = "Heal",
            DurationPolicy = EffectDurationPolicy.Instant,
            Modifiers = new List<EffectModifier>
            {
                new(new AttributeModifier("Health", ModifierOperation.Add, 50f))
            }
        };

        effect.DurationPolicy.Should().Be(EffectDurationPolicy.Instant);
    }

    [Fact]
    public void PeriodicEffect_HasDurationAndPeriod()
    {
        var effect = new GameplayEffect
        {
            Id = "Poison",
            DurationPolicy = EffectDurationPolicy.Periodic,
            DurationSeconds = 10f,
            PeriodSeconds = 1f,
            Modifiers = new List<EffectModifier>
            {
                new(new AttributeModifier("Health", ModifierOperation.Add, -5f), applyOnTick: true)
            }
        };

        effect.DurationPolicy.Should().Be(EffectDurationPolicy.Periodic);
        effect.DurationSeconds.Should().Be(10f);
        effect.PeriodSeconds.Should().Be(1f);
    }

    [Fact]
    public void ActiveEffect_IsExpired_WhenRemainingTimeIsZero()
    {
        var effectDef = new GameplayEffect
        {
            Id = "Buff",
            DurationPolicy = EffectDurationPolicy.Duration,
            DurationSeconds = 5f
        };
        var activeEffect = new ActiveEffect(effectDef);

        activeEffect.RemainingTime = 0f;

        activeEffect.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void ActiveEffect_IsNotExpired_ForInfiniteEffect()
    {
        var effectDef = new GameplayEffect
        {
            Id = "Aura",
            DurationPolicy = EffectDurationPolicy.Infinite
        };
        var activeEffect = new ActiveEffect(effectDef);

        activeEffect.RemainingTime = 0f;

        activeEffect.IsExpired.Should().BeFalse();
    }
}
