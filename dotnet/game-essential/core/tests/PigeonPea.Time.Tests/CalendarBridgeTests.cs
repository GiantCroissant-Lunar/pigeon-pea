using FluentAssertions;
using NodaTime;
using PigeonPea.Plugin.Time.Harptos;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;
using Xunit;

namespace PigeonPea.Time.Tests;

public class CalendarBridgeTests
{
    private readonly WorldClock _worldClock;
    private readonly HarptosCalendar _harptosCalendar;
    private readonly CalendarBridge _bridge;

    public CalendarBridgeTests()
    {
        _worldClock = new WorldClock(Instant.FromUtc(2000, 1, 1, 0, 0), realSecondsPerGameSecond: 1.0, ticksPerGameSecond: 1);
        _harptosCalendar = new HarptosCalendar();
        _bridge = new CalendarBridge(_worldClock, _harptosCalendar);
    }

    [Fact]
    public void ConstructorShouldThrowArgumentNullExceptionWhenWorldClockIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CalendarBridge(null!, _harptosCalendar));
    }

    [Fact]
    public void ConstructorShouldThrowArgumentNullExceptionWhenCalendarDefinitionIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CalendarBridge(_worldClock, null!));
    }

    [Fact]
    public void ToRealWorldShouldThrowArgumentNullExceptionWhenZoneIsNull()
    {
        // Arrange
        var fantasyDate = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _bridge.ToRealWorld(fantasyDate, null!));
    }

    [Fact]
    public void ToRealWorldShouldConvertFantasyDateToRealWorld()
    {
        // Arrange
        var fantasyDate = new FantasyDate(1, 1, 1, 0, 0, 0); // Hammer 1, Year 1, midnight
        var zone = DateTimeZone.Utc;

        // Act
        var realDate = _bridge.ToRealWorld(fantasyDate, zone);

        // Assert
        realDate.Should().Be(zone.AtStrictly(LocalDateTime.FromDateTime(new DateTime(2000, 1, 1, 0, 0, 0))));
    }

    [Fact]
    public void ToRealWorldShouldConvertFantasyDateWithTimeToRealWorld()
    {
        // Arrange
        var fantasyDate = new FantasyDate(1, 1, 1, 12, 30, 45); // Hammer 1, Year 1, 12:30:45
        var zone = DateTimeZone.Utc;

        // Act
        var realDate = _bridge.ToRealWorld(fantasyDate, zone);

        // Assert
        realDate.Should().Be(zone.AtStrictly(LocalDateTime.FromDateTime(new DateTime(2000, 1, 1, 12, 30, 45))));
    }

    [Fact]
    public void FromRealWorldShouldConvertRealWorldToFantasyDate()
    {
        // Arrange
        var realDate = DateTimeZone.Utc.AtStrictly(LocalDateTime.FromDateTime(new DateTime(2000, 1, 1, 0, 0, 0)));
        var zone = DateTimeZone.Utc;

        // Act
        var fantasyDate = _bridge.FromRealWorld(realDate);

        // Assert
        fantasyDate.Should().Be(new FantasyDate(1, 1, 1, 0, 0, 0));
    }

    [Fact]
    public void FromRealWorldShouldConvertRealWorldWithTimeToFantasyDate()
    {
        // Arrange
        var realDate = DateTimeZone.Utc.AtStrictly(LocalDateTime.FromDateTime(new DateTime(2000, 1, 1, 12, 30, 45)));
        var zone = DateTimeZone.Utc;

        // Act
        var fantasyDate = _bridge.FromRealWorld(realDate);

        // Assert
        fantasyDate.Should().Be(new FantasyDate(1, 1, 1, 12, 30, 45));
    }

    [Fact]
    public void ToInstantShouldConvertFantasyDateToInstant()
    {
        // Arrange
        var fantasyDate = new FantasyDate(1, 1, 1, 0, 0, 0); // Hammer 1, Year 1, midnight

        // Act
        var instant = _bridge.ToInstant(fantasyDate);

        // Assert
        instant.Should().Be(Instant.FromUtc(2000, 1, 1, 0, 0));
    }

    [Fact]
    public void FromInstantShouldConvertInstantToFantasyDate()
    {
        // Arrange
        var instant = Instant.FromUtc(2000, 1, 1, 0, 0);

        // Act
        var fantasyDate = _bridge.FromInstant(instant);

        // Assert
        fantasyDate.Should().Be(new FantasyDate(1, 1, 1, 0, 0, 0));
    }

    [Theory]
    [InlineData(1, 1, 1, 0, 0, 0)] // Year 1, Hammer 1, midnight
    [InlineData(1, 1, 15, 12, 30, 45)] // Year 1, Hammer 15, 12:30:45
    [InlineData(4, 16, 1, 23, 59, 59)] // Year 4, Shieldmeet 1, 23:59:59 (leap year)
    [InlineData(10, 7, 30, 6, 15, 30)] // Year 10, Flamerule 30, 6:15:30
    public void RoundTripConversionShouldWork(int year, int month, int day, int hour, int minute, int second)
    {
        // Arrange
        var originalFantasyDate = new FantasyDate(year, month, day, hour, minute, second);
        var zone = DateTimeZone.Utc;

        // Act - Fantasy -> Real -> Fantasy
        var realDate = _bridge.ToRealWorld(originalFantasyDate, zone);
        var convertedFantasyDate = _bridge.FromRealWorld(realDate);

        // Assert
        convertedFantasyDate.Should().Be(originalFantasyDate);
    }

    [Theory]
    [InlineData(1, 1, 1, 0, 0, 0)] // Year 1, Hammer 1, midnight
    [InlineData(1, 1, 15, 12, 30, 45)] // Year 1, Hammer 15, 12:30:45
    [InlineData(4, 16, 1, 23, 59, 59)] // Year 4, Shieldmeet 1, 23:59:59 (leap year)
    [InlineData(10, 7, 30, 6, 15, 30)] // Year 10, Flamerule 30, 6:15:30
    public void InstantRoundTripConversionShouldWork(int year, int month, int day, int hour, int minute, int second)
    {
        // Arrange
        var originalFantasyDate = new FantasyDate(year, month, day, hour, minute, second);

        // Act - Fantasy -> Instant -> Fantasy
        var instant = _bridge.ToInstant(originalFantasyDate);
        var convertedFantasyDate = _bridge.FromInstant(instant);

        // Assert
        convertedFantasyDate.Should().Be(originalFantasyDate);
    }

    [Fact]
    public void RoundTripWithDifferentTimeZonesShouldWork()
    {
        // Arrange
        var originalFantasyDate = new FantasyDate(1, 1, 1, 12, 0, 0);
        var utcZone = DateTimeZone.Utc;
        var plusOneZone = DateTimeZone.ForOffset(Offset.FromHours(1));

        // Act - Fantasy -> UTC -> Fantasy
        var utcRealDate = _bridge.ToRealWorld(originalFantasyDate, utcZone);
        var utcConvertedFantasyDate = _bridge.FromRealWorld(utcRealDate);

        // Act - Fantasy -> UTC+1 -> Fantasy
        var plusOneRealDate = _bridge.ToRealWorld(originalFantasyDate, plusOneZone);
        var plusOneConvertedFantasyDate = _bridge.FromRealWorld(plusOneRealDate);

        // Assert
        utcConvertedFantasyDate.Should().Be(originalFantasyDate);
        plusOneConvertedFantasyDate.Should().Be(originalFantasyDate);
    }

    [Fact]
    public void ConversionAcrossMultipleDaysShouldWork()
    {
        // Arrange
        var fantasyDate = new FantasyDate(1, 2, 1, 0, 0, 0); // Alturiak 1, Year 1, midnight (after 30 days)
        var zone = DateTimeZone.Utc;

        // Act
        var realDate = _bridge.ToRealWorld(fantasyDate, zone);
        var convertedFantasyDate = _bridge.FromRealWorld(realDate);

        // Assert
        convertedFantasyDate.Should().Be(new FantasyDate(1, 2, 1, 0, 0, 0));
    }

    [Fact]
    public void ConversionAcrossMultipleYearsShouldWork()
    {
        // Arrange
        var fantasyDate = new FantasyDate(5, 1, 1, 0, 0, 0); // Hammer 1, Year 5, midnight (after 4 years)
        var zone = DateTimeZone.Utc;

        // Act
        var realDate = _bridge.ToRealWorld(fantasyDate, zone);
        var convertedFantasyDate = _bridge.FromRealWorld(realDate);

        // Assert
        convertedFantasyDate.Should().Be(new FantasyDate(5, 1, 1, 0, 0, 0));
    }
}
