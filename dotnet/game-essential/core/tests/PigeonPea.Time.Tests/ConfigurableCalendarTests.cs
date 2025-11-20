using FluentAssertions;
using PigeonPea.Plugin.Time.Configurable;
using PigeonPea.Time.Contracts;
using Xunit;

namespace PigeonPea.Time.Tests;

public class ConfigurableCalendarTests
{
    [Fact]
    public void SimpleCalendarShouldWork()
    {
        var config = new CalendarConfig
        {
            Name = "Simple",
            TicksPerDay = 100,
            Months = new List<MonthConfig>
            {
                new() { Name = "Month1", Days = 10 },
                new() { Name = "Month2", Days = 20 }
            }
        };
        var calendar = new ConfigurableCalendar(config);

        // Year 1, Month 1, Day 1
        var tick = new WorldTick(0);
        var date = calendar.FromWorldTick(tick);
        date.Should().Be(new FantasyDate(1, 1, 1, 0, 0, 0));

        // Year 1, Month 2, Day 1 (After 10 days)
        tick = new WorldTick(10 * 100);
        date = calendar.FromWorldTick(tick);
        date.Should().Be(new FantasyDate(1, 2, 1, 0, 0, 0));

        // Year 2, Month 1, Day 1 (After 30 days)
        tick = new WorldTick(30 * 100);
        date = calendar.FromWorldTick(tick);
        date.Should().Be(new FantasyDate(2, 1, 1, 0, 0, 0));
    }

    [Fact]
    public void LeapYearShouldWork()
    {
        var config = new CalendarConfig
        {
            Name = "Leap",
            TicksPerDay = 100,
            Months = new List<MonthConfig>
            {
                new() { Name = "Month1", Days = 10, LeapDayRule = new LeapDayRule() }
            },
            LeapRule = new LeapRule { Interval = 2 } // Every 2 years
        };
        var calendar = new ConfigurableCalendar(config);

        // Year 1 (Normal): 10 days
        // Year 2 (Leap): 11 days
        // Year 3 (Normal): 10 days

        // Start of Year 2
        var tick = new WorldTick(10 * 100);
        var date = calendar.FromWorldTick(tick);
        date.Should().Be(new FantasyDate(2, 1, 1, 0, 0, 0));

        // End of Year 2 (Day 11)
        tick = new WorldTick((10 + 10) * 100); // Day 11 (0-indexed 10)
        date = calendar.FromWorldTick(tick);
        date.Should().Be(new FantasyDate(2, 1, 11, 0, 0, 0));

        // Start of Year 3
        tick = new WorldTick((10 + 11) * 100);
        date = calendar.FromWorldTick(tick);
        date.Should().Be(new FantasyDate(3, 1, 1, 0, 0, 0));
    }
}
