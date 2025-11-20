using FluentAssertions;
using PigeonPea.Plugin.Time.Harptos;
using PigeonPea.Time.Contracts;
using Xunit;

namespace PigeonPea.Time.Tests;

public class FantasyDurationTests
{
    private readonly HarptosCalendar _harptosCalendar;

    public FantasyDurationTests()
    {
        _harptosCalendar = new HarptosCalendar();
    }

    [Fact]
    public void BetweenShouldCalculateDurationCorrectly()
    {
        // Arrange
        var start = new FantasyDate(1372, 1, 15, 12, 30, 0);
        var end = new FantasyDate(1372, 1, 17, 14, 45, 30);

        // Act
        var duration = FantasyDuration.Between(start, end, _harptosCalendar);

        // Assert
        duration.Days.Should().Be(2);
        duration.Hours.Should().Be(2);
        duration.Minutes.Should().Be(15);
        duration.Seconds.Should().Be(30);
    }

    [Fact]
    public void BetweenShouldHandleNegativeDuration()
    {
        // Arrange
        var start = new FantasyDate(1372, 1, 17, 14, 45, 30);
        var end = new FantasyDate(1372, 1, 15, 12, 30, 0);

        // Act
        var duration = FantasyDuration.Between(start, end, _harptosCalendar);

        // Assert
        duration.Days.Should().BeNegative();
    }

    [Fact]
    public void BetweenShouldHandleSameDate()
    {
        // Arrange
        var date = new FantasyDate(1372, 1, 15, 12, 30, 0);

        // Act
        var duration = FantasyDuration.Between(date, date, _harptosCalendar);

        // Assert
        duration.Should().Be(FantasyDuration.Zero);
    }

    [Fact]
    public void ToStringShouldFormatCorrectly()
    {
        // Arrange
        var duration = new FantasyDuration(1, 2, 3, 4, 5, 6);

        // Act
        var str = duration.ToString();

        // Assert
        str.Should().Contain("1 year");
        str.Should().Contain("2 months");
        str.Should().Contain("3 days");
        str.Should().Contain("4 hours");
        str.Should().Contain("5 minutes");
        str.Should().Contain("6 seconds");
    }

    [Fact]
    public void ToStringShouldHandlePlurals()
    {
        // Arrange
        var duration = new FantasyDuration(2, 0, 0, 0, 0, 0);

        // Act
        var str = duration.ToString();

        // Assert
        str.Should().Contain("2 years"); // Plural
    }

    [Fact]
    public void ToStringShouldHandleZero()
    {
        // Arrange
        var duration = FantasyDuration.Zero;

        // Act
        var str = duration.ToString();

        // Assert
        str.Should().Be("0 seconds");
    }

    [Fact]
    public void TotalDaysShouldCalculateCorrectly()
    {
        // Arrange
        var duration = new FantasyDuration(0, 0, 2, 12, 0, 0); // 2.5 days

        // Act
        var totalDays = duration.TotalDays;

        // Assert
        totalDays.Should().BeApproximately(2.5, 0.01);
    }

    [Fact]
    public void TotalHoursShouldCalculateCorrectly()
    {
        // Arrange
        var duration = new FantasyDuration(0, 0, 1, 12, 30, 0); // 1 day, 12.5 hours

        // Act
        var totalHours = duration.TotalHours;

        // Assert
        totalHours.Should().BeApproximately(36.5, 0.01);
    }

    [Fact]
    public void BetweenShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var start = new FantasyDate(1372, 1, 15, 12, 30, 0);
        var end = new FantasyDate(1372, 1, 17, 14, 45, 30);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FantasyDuration.Between(start, end, null!));
    }
}
