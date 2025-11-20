using FluentAssertions;
using NodaTime;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;
using Xunit;

namespace PigeonPea.Time.Tests;

public class WorldClockTests
{
    [Fact]
    public void RealTimeMappingShouldWork()
    {
        var epoch = Instant.FromUtc(2000, 1, 1, 0, 0);
        var clock = new WorldClock(epoch, realSecondsPerGameSecond: 1.0, ticksPerGameSecond: 1);

        var tick = new WorldTick(3600); // 1 hour
        var instant = clock.ToInstant(tick);

        instant.Should().Be(epoch + Duration.FromHours(1));
    }

    [Fact]
    public void FastTimeMappingShouldWork()
    {
        var epoch = Instant.FromUtc(2000, 1, 1, 0, 0);
        // 1 real second = 60 game seconds (60x speed)
        // So 1 real second = 1 game minute
        var clock = new WorldClock(epoch, realSecondsPerGameSecond: 1.0 / 60.0, ticksPerGameSecond: 1);

        // 1 hour in game = 60 minutes = 3600 seconds
        var tick = new WorldTick(3600);

        // In real time, this should be 3600 / 60 = 60 seconds = 1 minute
        var instant = clock.ToInstant(tick);

        instant.Should().Be(epoch + Duration.FromMinutes(1));
    }
}
