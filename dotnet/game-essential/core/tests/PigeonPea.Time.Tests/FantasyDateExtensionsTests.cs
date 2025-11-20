using FluentAssertions;
using NodaTime;
using PigeonPea.Plugin.Time.Harptos;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;
using Xunit;

namespace PigeonPea.Time.Tests;

public class FantasyDateExtensionsTests
{
    private readonly HarptosCalendar _harptosCalendar;

    public FantasyDateExtensionsTests()
    {
        _harptosCalendar = new HarptosCalendar();
    }

    [Fact]
    public void AddDaysShouldHandleMonthBoundary()
    {
        // Arrange - Hammer has 30 days
        var date = new FantasyDate(1, 1, 28, 12, 0, 0); // Hammer 28

        // Act
        var newDate = date.AddDays(_harptosCalendar, 5); // Should go to Alturiak 2

        // Assert
        newDate.Month.Should().Be(2); // Alturiak
        newDate.Day.Should().BeGreaterThan(1); // Should have rolled over
    }

    [Fact]
    public void AddDaysShouldHandleNegativeDays()
    {
        // Arrange
        var date = new FantasyDate(1, 2, 5, 12, 0, 0); // Alturiak 5

        // Act
        var newDate = date.AddDays(_harptosCalendar, -10); // Should go back to Hammer

        // Assert
        newDate.Month.Should().Be(1); // Hammer
    }

    [Fact]
    public void AddDaysShouldHandleYearBoundary()
    {
        // Arrange - Near end of year
        var date = new FantasyDate(1, 12, 28, 12, 0, 0); // Late in year

        // Act
        var newDate = date.AddDays(_harptosCalendar, 10); // Should roll into next year

        // Assert
        newDate.Year.Should().BeGreaterThan(1);
    }

    [Fact]
    public void AddHoursShouldHandleDayBoundary()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 15, 22, 0, 0); // 10 PM

        // Act
        var newDate = date.AddHours(_harptosCalendar, 5); // Should be next day at 3 AM

        // Assert
        newDate.Day.Should().Be(16);
        newDate.Hour.Should().Be(3);
    }

    [Fact]
    public void AddHoursShouldHandleNegativeHours()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 15, 2, 0, 0); // 2 AM

        // Act
        var newDate = date.AddHours(_harptosCalendar, -5); // Should be previous day at 9 PM

        // Assert
        newDate.Day.Should().Be(14);
        newDate.Hour.Should().Be(21);
    }

    [Fact]
    public void AddMinutesShouldHandleHourBoundary()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 15, 23, 50, 0); // 11:50 PM

        // Act
        var newDate = date.AddMinutes(_harptosCalendar, 20); // Should be next day at 12:10 AM

        // Assert
        newDate.Day.Should().Be(16);
        newDate.Hour.Should().Be(0);
        newDate.Minute.Should().Be(10);
    }

    [Fact]
    public void AddSecondsShouldHandleMinuteBoundary()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 15, 12, 59, 50); // 12:59:50

        // Act
        var newDate = date.AddSeconds(_harptosCalendar, 20); // Should be 13:00:10

        // Assert
        newDate.Hour.Should().Be(13);
        newDate.Minute.Should().Be(0);
        newDate.Second.Should().Be(10);
    }

    [Fact]
    public void AddYearsShouldAddYearsCorrectly()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 15, 12, 0, 0);

        // Act
        var newDate = date.AddYears(_harptosCalendar, 5);

        // Assert
        newDate.Year.Should().Be(1377);
        newDate.Month.Should().Be(1);
        newDate.Day.Should().Be(15);
    }

    [Fact]
    public void AddYearsShouldHandleNegativeYears()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 15, 12, 0, 0);

        // Act
        var newDate = date.AddYears(_harptosCalendar, -10);

        // Assert
        newDate.Year.Should().Be(1362);
    }

    [Fact]
    public void AddMonthsShouldHandleYearBoundary()
    {
        // Arrange
        var date = new FantasyDate(1372, 11, 15, 12, 0, 0); // Month 11

        // Act
        var newDate = date.AddMonths(_harptosCalendar, 3); // Should be month 2 of next year

        // Assert
        newDate.Year.Should().Be(1373);
        newDate.Month.Should().Be(2);
    }

    [Fact]
    public void AddMonthsShouldHandleNegativeMonths()
    {
        // Arrange
        var date = new FantasyDate(1372, 2, 15, 12, 0, 0); // Month 2

        // Act
        var newDate = date.AddMonths(_harptosCalendar, -3); // Should be month 11 of previous year

        // Assert
        newDate.Year.Should().Be(1371);
        newDate.Month.Should().Be(11);
    }

    [Theory]
    [InlineData(1, 1, 15, 12, 0, 0, 10)] // Add 10 days
    [InlineData(1, 1, 15, 12, 0, 0, 100)] // Add 100 days
    [InlineData(1, 1, 15, 12, 0, 0, -5)] // Subtract 5 days
    public void AddDaysRoundTripShouldPreserveDate(int year, int month, int day, int hour, int minute, int second, int daysToAdd)
    {
        // Arrange
        var originalDate = new FantasyDate(year, month, day, hour, minute, second);

        // Act - Add and subtract
        var modifiedDate = originalDate.AddDays(_harptosCalendar, daysToAdd);
        var restoredDate = modifiedDate.AddDays(_harptosCalendar, -daysToAdd);

        // Assert
        restoredDate.Should().Be(originalDate);
    }

    [Fact]
    public void AddDaysShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => date.AddDays(null!, 5));
    }

    [Fact]
    public void AddHoursShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => date.AddHours(null!, 5));
    }

    [Fact]
    public void AddMinutesShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => date.AddMinutes(null!, 5));
    }

    [Fact]
    public void AddSecondsShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => date.AddSeconds(null!, 5));
    }

    [Fact]
    public void AddYearsShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => date.AddYears(null!, 5));
    }

    [Fact]
    public void AddMonthsShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => date.AddMonths(null!, 5));
    }

    #region Validation Tests

    [Fact]
    public void IsValidShouldReturnTrueForValidDate()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 15, 12, 30, 45);

        // Act
        var isValid = date.IsValid(_harptosCalendar);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValidShouldReturnFalseForInvalidHour()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 15, 25, 30, 45); // Hour 25 is invalid

        // Act
        var isValid = date.IsValid(_harptosCalendar);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValidShouldReturnFalseForInvalidMinute()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 15, 12, 60, 45); // Minute 60 is invalid

        // Act
        var isValid = date.IsValid(_harptosCalendar);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValidShouldReturnFalseForInvalidSecond()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 15, 12, 30, 60); // Second 60 is invalid

        // Act
        var isValid = date.IsValid(_harptosCalendar);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValidShouldReturnFalseForInvalidMonth()
    {
        // Arrange
        var date = new FantasyDate(1372, 13, 15, 12, 30, 45); // Month 13 is invalid (Harptos has 12 months)

        // Act
        var isValid = date.IsValid(_harptosCalendar);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValidShouldReturnFalseForInvalidDay()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 35, 12, 30, 45); // Day 35 is invalid (Hammer has 30 days)

        // Act
        var isValid = date.IsValid(_harptosCalendar);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void NormalizeShouldReturnSameDateForValidDate()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 15, 12, 30, 45);

        // Act
        var normalized = date.Normalize(_harptosCalendar);

        // Assert
        normalized.Should().Be(date);
    }

    [Fact]
    public void NormalizeShouldFixInvalidHour()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 15, 25, 30, 45); // Hour 25 should roll to next day

        // Act
        var normalized = date.Normalize(_harptosCalendar);

        // Assert
        normalized.Day.Should().Be(16);
        normalized.Hour.Should().Be(1); // 25 - 24 = 1
    }

    [Fact]
    public void NormalizeShouldFixInvalidDay()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 35, 12, 30, 45); // Day 35 should roll to next month

        // Act
        var normalized = date.Normalize(_harptosCalendar);

        // Assert
        normalized.Month.Should().Be(2); // Should roll to Alturiak
        normalized.IsValid(_harptosCalendar).Should().BeTrue();
    }

    [Fact]
    public void NormalizeShouldFixMultipleOverflows()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 35, 25, 65, 70); // Multiple invalid components

        // Act
        var normalized = date.Normalize(_harptosCalendar);

        // Assert
        normalized.IsValid(_harptosCalendar).Should().BeTrue();
    }

    [Fact]
    public void IsValidShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => date.IsValid(null!));
    }

    [Fact]
    public void NormalizeShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => date.Normalize(null!));
    }

    #endregion
}
