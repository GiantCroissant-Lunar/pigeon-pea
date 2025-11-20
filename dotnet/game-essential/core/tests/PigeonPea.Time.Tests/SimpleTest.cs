using FluentAssertions;
using PigeonPea.Plugin.Time.Harptos;
using PigeonPea.Time.Core;
using Xunit;

namespace PigeonPea.Time.Tests;

public class SimpleTest
{
    [Fact]
    public void BasicTestShouldPass()
    {
        // Arrange
        var worldClock = new PigeonPea.Time.Core.WorldClock(NodaTime.Instant.FromUtc(2000, 1, 1, 0, 0), realSecondsPerGameSecond: 1.0, ticksPerGameSecond: 1);
        var service = new PigeonPea.Time.Core.CalendarService(worldClock);
        var calendar = new PigeonPea.Plugin.Time.Harptos.HarptosCalendar();

        // Act
        service.RegisterCalendar("test", calendar);
        var retrieved = service.GetCalendar("test");

        // Assert
        retrieved.Should().Be(calendar);
    }
}