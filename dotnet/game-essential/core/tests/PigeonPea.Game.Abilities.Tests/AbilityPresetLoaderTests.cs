using System;
using System.IO;
using Arch.Core;
using Arch.Core.Extensions;
using FluentAssertions;
using MessagePipe;
using PigeonPea.Game.Abilities.Components;
using PigeonPea.Game.Abilities.Events;
using PigeonPea.Game.Abilities.Integration;
using PigeonPea.Game.Abilities.Presets;
using Xunit;

namespace PigeonPea.Game.Abilities.Tests;

public class AbilityPresetLoaderTests
{
    private sealed class TestPublisher<T> : IPublisher<T>
    {
        public System.Collections.Generic.List<T> Published { get; } = new();
        public void Publish(T message) => Published.Add(message);
    }

    [Fact]
    public void LoadAbilityFromJson_FileAndCastViaEcs_WorksAsExpected()
    {
        // Arrange
        using var world = World.Create();

        var caster = world.Create();
        var target = world.Create();

        // Ensure components
        caster.Add(new AbilitySystemComponent());
        target.Add(new AbilitySystemComponent());

        ref var casterAsc = ref caster.Get<AbilitySystemComponent>();
        ref var targetAsc = ref target.Get<AbilitySystemComponent>();

        casterAsc.Attributes.SetBaseValue("Mana", 50f);
        targetAsc.Attributes.SetBaseValue("Health", 100f);

        // Load Firebolt ability from JSON preset
        var baseDir = AppContext.BaseDirectory;
        var presetPath = Path.Combine(baseDir, "Presets", "Firebolt.json");
        File.Exists(presetPath).Should().BeTrue($"Preset file not found at {presetPath}");

        var firebolt = AbilityPresetLoader.LoadAbilityFromFile(presetPath);

        // Give ability to caster
        world.GiveAbility(caster, firebolt);

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
}
