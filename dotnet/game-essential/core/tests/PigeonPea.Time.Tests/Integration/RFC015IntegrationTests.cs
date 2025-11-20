#pragma warning disable CA1707 // Identifiers should not contain underscores

using FluentAssertions;
using NodaTime;
using PigeonPea.Plugin.Time.Harptos;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;
using Xunit;

namespace PigeonPea.Time.Tests.Integration;

/// <summary>
/// Integration tests for RFC-015 use cases.
/// These tests validate end-to-end scenarios across multiple components.
/// </summary>
public class RFC015IntegrationTests
{
    private readonly ICalendarService _calendarService;
    private readonly HarptosCalendar _harptosCalendar;

    public RFC015IntegrationTests()
    {
        var worldClock = new WorldClock(
            realEpoch: Instant.FromUtc(2025, 1, 1, 0, 0),
            realSecondsPerGameSecond: 1.0
        );

        _calendarService = new CalendarService(worldClock);
        _harptosCalendar = new HarptosCalendar();
        _calendarService.RegisterCalendar("harptos", _harptosCalendar);
    }

    #region Use Case 1: Event Logging

    [Fact]
    public void EventLogging_ShouldLogWithBothFantasyAndRealWorldTimestamps()
    {
        // Arrange - Player defeats a boss
        var fantasyDate = new FantasyDate(1372, 1, 15, 14, 30, 0);

        // Act - Convert to real-world time
        var realDate = _calendarService.ToRealWorld(fantasyDate, "harptos", DateTimeZone.Utc);

        // Assert - Both timestamps should be available
        fantasyDate.Should().NotBeNull();
        realDate.Should().NotBeNull();
        realDate.Zone.Should().Be(DateTimeZone.Utc);
    }

    [Fact]
    public void EventLogging_ShouldSupportRoundTripConversion()
    {
        // Arrange
        var originalFantasyDate = new FantasyDate(1372, 1, 15, 14, 30, 0);

        // Act - Convert to real-world and back
        var realDate = _calendarService.ToRealWorld(originalFantasyDate, "harptos", DateTimeZone.Utc);
        var backToFantasy = _calendarService.FromRealWorld(realDate, "harptos");

        // Assert - Should preserve the date (within 1 second tolerance)
        var duration = FantasyDuration.Between(originalFantasyDate, backToFantasy, _harptosCalendar);
        Math.Abs(duration.TotalSeconds).Should().BeLessThanOrEqualTo(1);
    }

    #endregion

    #region Use Case 2: Scheduled Events

    [Fact]
    public void ScheduledEvents_ShouldConvertToMultipleTimeZones()
    {
        // Arrange - Festival of Midsummer
        var festivalDate = new FantasyDate(1372, 15, 1, 0, 0, 0);

        // Act - Convert to different time zones
        var utcTime = _calendarService.ToRealWorld(festivalDate, "harptos", DateTimeZone.Utc);
        var laTime = _calendarService.ToRealWorld(festivalDate, "harptos", DateTimeZoneProviders.Tzdb["America/Los_Angeles"]);
        var nyTime = _calendarService.ToRealWorld(festivalDate, "harptos", DateTimeZoneProviders.Tzdb["America/New_York"]);

        // Assert - All should represent the same instant
        utcTime.ToInstant().Should().Be(laTime.ToInstant());
        utcTime.ToInstant().Should().Be(nyTime.ToInstant());

        // But different local times
        utcTime.LocalDateTime.Should().NotBe(laTime.LocalDateTime);
        utcTime.LocalDateTime.Should().NotBe(nyTime.LocalDateTime);
    }

    [Fact]
    public void ScheduledEvents_ShouldSupportQuestDeadlines()
    {
        // Arrange
        var questStart = new FantasyDate(1372, 1, 1, 0, 0, 0);
        var questDeadline = new FantasyDate(1372, 1, 15, 23, 59, 59);
        var currentDate = new FantasyDate(1372, 1, 10, 12, 0, 0);

        // Act - Check if quest is still active
        var isActive = currentDate.IsBefore(questDeadline, _harptosCalendar);
        var duration = currentDate.DurationTo(questDeadline, _harptosCalendar);

        // Assert
        isActive.Should().BeTrue();
        duration.Days.Should().BeGreaterThan(0);
    }

    #endregion

    #region Use Case 3: Time-Based Progression

    [Fact]
    public void TimeProgression_ShouldSupportAcceleratedTime()
    {
        // Arrange - 60× speed (1 real minute = 1 game hour)
        var acceleratedClock = new WorldClock(
            realEpoch: Instant.FromUtc(2025, 1, 1, 0, 0),
            realSecondsPerGameSecond: 1.0 / 60.0
        );
        var acceleratedService = new CalendarService(acceleratedClock);
        acceleratedService.RegisterCalendar("harptos", _harptosCalendar);

        var startInstant = Instant.FromUtc(2025, 1, 1, 0, 0);
        var tenMinutesLater = startInstant.Plus(Duration.FromMinutes(10));

        // Act
        var startDate = acceleratedService.FromRealWorld(startInstant.InUtc(), "harptos");
        var endDate = acceleratedService.FromRealWorld(tenMinutesLater.InUtc(), "harptos");

        // Assert - 10 real minutes = 10 game hours
        var duration = startDate.DurationTo(endDate, _harptosCalendar);
        duration.Hours.Should().Be(10);
    }

    [Fact]
    public void TimeProgression_ShouldSupportRealTime()
    {
        // Arrange - Real-time (1:1)
        var realTimeClock = new WorldClock(
            realEpoch: Instant.FromUtc(2025, 1, 1, 0, 0),
            realSecondsPerGameSecond: 1.0
        );
        var realTimeService = new CalendarService(realTimeClock);
        realTimeService.RegisterCalendar("harptos", _harptosCalendar);

        var startInstant = Instant.FromUtc(2025, 1, 1, 0, 0);
        var oneHourLater = startInstant.Plus(Duration.FromHours(1));

        // Act
        var startDate = realTimeService.FromRealWorld(startInstant.InUtc(), "harptos");
        var endDate = realTimeService.FromRealWorld(oneHourLater.InUtc(), "harptos");

        // Assert - 1 real hour = 1 game hour
        var duration = startDate.DurationTo(endDate, _harptosCalendar);
        duration.Hours.Should().Be(1);
    }

    #endregion

    #region Use Case 4: Multi-Calendar Conversion

    [Fact]
    public void MultiCalendar_ShouldConvertBetweenCalendars()
    {
        // Arrange - Register a second calendar (using Harptos as a stand-in)
        _calendarService.RegisterCalendar("elven", _harptosCalendar);

        var harptosDate = new FantasyDate(1372, 1, 15, 14, 30, 0);

        // Act - Convert between calendars
        var elvenDate = _calendarService.Convert(harptosDate, "harptos", "elven");

        // Assert - Should preserve the same moment in time
        var harptosInstant = _calendarService.ToRealWorld(harptosDate, "harptos", DateTimeZone.Utc).ToInstant();
        var elvenInstant = _calendarService.ToRealWorld(elvenDate, "elven", DateTimeZone.Utc).ToInstant();

        harptosInstant.Should().Be(elvenInstant);
    }

    [Fact]
    public void MultiCalendar_ShouldSupportRoundTripConversion()
    {
        // Arrange
        _calendarService.RegisterCalendar("elven", _harptosCalendar);
        var originalDate = new FantasyDate(1372, 1, 15, 14, 30, 0);

        // Act - Convert to elven and back
        var elvenDate = _calendarService.Convert(originalDate, "harptos", "elven");
        var backToHarptos = _calendarService.Convert(elvenDate, "elven", "harptos");

        // Assert - Should preserve the date
        backToHarptos.Should().Be(originalDate);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void ComplexScenario_QuestTimerWithTimeZones()
    {
        // Arrange - A quest that starts in one timezone and ends in another
        var questStart = new FantasyDate(1372, 1, 1, 0, 0, 0);
        var questEnd = new FantasyDate(1372, 1, 7, 23, 59, 59); // 7 days later

        // Act - Convert to different player time zones
        var playerLA = _calendarService.ToRealWorld(questEnd, "harptos", DateTimeZoneProviders.Tzdb["America/Los_Angeles"]);
        var playerTokyo = _calendarService.ToRealWorld(questEnd, "harptos", DateTimeZoneProviders.Tzdb["Asia/Tokyo"]);


        // Calculate remaining time
        var currentDate = new FantasyDate(1372, 1, 5, 12, 0, 0);
        var remaining = currentDate.DurationTo(questEnd, _harptosCalendar);

        // Assert
        remaining.TotalDays.Should().BeGreaterThan(2);
        remaining.TotalDays.Should().BeLessThan(3);

        // Same instant, different local times
        playerLA.ToInstant().Should().Be(playerTokyo.ToInstant());
    }

    [Fact]
    public void ComplexScenario_EventSchedulingWithValidation()
    {
        // Arrange - Schedule an event, but validate the date first
        var eventDate = new FantasyDate(1372, 1, 35, 12, 0, 0); // Invalid day!

        // Act - Normalize the invalid date
        var validDate = eventDate.Normalize(_harptosCalendar);

        // Assert - Should be normalized to a valid date
        validDate.IsValid(_harptosCalendar).Should().BeTrue();
        validDate.Month.Should().Be(2); // Should roll to next month

        // Should be able to convert to real-world time
        var realDate = _calendarService.ToRealWorld(validDate, "harptos", DateTimeZone.Utc);
        realDate.Should().NotBeNull();
    }

    [Fact]
    public void ComplexScenario_CompareEventsAcrossYears()
    {
        // Arrange - Two events in different years
        var event1 = new FantasyDate(1372, 12, 30, 23, 59, 59);
        var event2 = new FantasyDate(1373, 1, 1, 0, 0, 0);

        // Act
        var event1IsEarlier = event1.IsBefore(event2, _harptosCalendar);
        var duration = event1.DurationTo(event2, _harptosCalendar);

        // Assert
        event1IsEarlier.Should().BeTrue();
        duration.Years.Should().Be(0); // Duration is only 1 second, so years should be 0
        duration.TotalSeconds.Should().Be(1);
    }

    #endregion
}
