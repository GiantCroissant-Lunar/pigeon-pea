using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using PigeonPea.Shared.Perception.Enums;
using PigeonPea.Shared.Perception.Models;
using Xunit;

namespace PigeonPea.Perception.Core.Tests.Auditory;

public class AuditoryPerceptionDataTests
{
    [Fact]
    public void GetSoundsOfType_FiltersCorrectly()
    {
        var auditory = new AuditoryPerceptionData
        {
            HeardSounds = new List<SoundEvent>
            {
                new() { Type = SoundType.Footsteps, Distance = 5f },
                new() { Type = SoundType.CombatNoise, Distance = 10f },
                new() { Type = SoundType.Footsteps, Distance = 3f }
            }
        };

        var footsteps = auditory.GetSoundsOfType(SoundType.Footsteps).ToList();

        footsteps.Should().HaveCount(2);
        footsteps.All(s => s.Type == SoundType.Footsteps).Should().BeTrue();
    }

    [Fact]
    public void GetClosestSound_ReturnsNearestSound()
    {
        var auditory = new AuditoryPerceptionData
        {
            HeardSounds = new List<SoundEvent>
            {
                new() { Type = SoundType.Footsteps, Distance = 8f },
                new() { Type = SoundType.Footsteps, Distance = 2f },
                new() { Type = SoundType.Footsteps, Distance = 5f }
            }
        };

        var closest = auditory.GetClosestSound(SoundType.Footsteps);

        closest.Should().NotBeNull();
        closest!.Distance.Should().Be(2f);
    }

    [Fact]
    public void HeardCombat_DetectsCombatSounds()
    {
        var auditory = new AuditoryPerceptionData
        {
            HeardSounds = new List<SoundEvent>
            {
                new() { Type = SoundType.Footsteps },
                new() { Type = SoundType.CombatNoise }
            }
        };

        auditory.HeardCombat().Should().BeTrue();
    }
}
