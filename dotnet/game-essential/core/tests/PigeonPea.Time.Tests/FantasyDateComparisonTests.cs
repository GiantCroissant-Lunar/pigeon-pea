using FluentAssertions;
using PigeonPea.Plugin.Time.Harptos;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;
using Xunit;

namespace PigeonPea.Time.Tests;

public class FantasyDateComparisonTests
{
    private readonly HarptosCalendar _harptosCalendar;

    public FantasyDateComparisonTests()
    {
        _harptosCalendar = new HarptosCalendar();
    }

    [Fact]
    public void IsBeforeShouldReturnTrueWhenDateIsEarlier()
    {
        // Arrange
        var earlier = new FantasyDate(1372, 1, 15, 12, 0, 0);
        var later = new FantasyDate(1372, 1, 16, 12, 0, 0);

        // Act
        var result = earlier.IsBefore(later, _harptosCalendar);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBeforeShouldReturnFalseWhenDateIsLater()
    {
        // Arrange
        var earlier = new FantasyDate(1372, 1, 15, 12, 0, 0);
        var later = new FantasyDate(1372, 1, 16, 12, 0, 0);

        // Act
        var result = later.IsBefore(earlier, _harptosCalendar);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAfterShouldReturnTrueWhenDateIsLater()
    {
        // Arrange
        var earlier = new FantasyDate(1372, 1, 15, 12, 0, 0);
        var later = new FantasyDate(1372, 1, 16, 12, 0, 0);

        // Act
        var result = later.IsAfter(earlier, _harptosCalendar);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAfterShouldReturnFalseWhenDateIsEarlier()
    {
        // Arrange
        var earlier = new FantasyDate(1372, 1, 15, 12, 0, 0);
        var later = new FantasyDate(1372, 1, 16, 12, 0, 0);

        // Act
        var result = earlier.IsAfter(later, _harptosCalendar);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSameAsShouldReturnTrueForIdenticalDates()
    {
        // Arrange
        var date1 = new FantasyDate(1372, 1, 15, 12, 30, 45);
        var date2 = new FantasyDate(1372, 1, 15, 12, 30, 45);

        // Act
        var result = date1.IsSameAs(date2, _harptosCalendar);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSameAsShouldReturnFalseForDifferentDates()
    {
        // Arrange
        var date1 = new FantasyDate(1372, 1, 15, 12, 30, 45);
        var date2 = new FantasyDate(1372, 1, 15, 12, 30, 46); // 1 second difference

        // Act
        var result = date1.IsSameAs(date2, _harptosCalendar);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsBeforeOrSameAsShouldReturnTrueForEarlierDate()
    {
        // Arrange
        var earlier = new FantasyDate(1372, 1, 15, 12, 0, 0);
        var later = new FantasyDate(1372, 1, 16, 12, 0, 0);

        // Act
        var result = earlier.IsBeforeOrSameAs(later, _harptosCalendar);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBeforeOrSameAsShouldReturnTrueForSameDate()
    {
        // Arrange
        var date1 = new FantasyDate(1372, 1, 15, 12, 0, 0);
        var date2 = new FantasyDate(1372, 1, 15, 12, 0, 0);

        // Act
        var result = date1.IsBeforeOrSameAs(date2, _harptosCalendar);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAfterOrSameAsShouldReturnTrueForLaterDate()
    {
        // Arrange
        var earlier = new FantasyDate(1372, 1, 15, 12, 0, 0);
        var later = new FantasyDate(1372, 1, 16, 12, 0, 0);

        // Act
        var result = later.IsAfterOrSameAs(earlier, _harptosCalendar);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAfterOrSameAsShouldReturnTrueForSameDate()
    {
        // Arrange
        var date1 = new FantasyDate(1372, 1, 15, 12, 0, 0);
        var date2 = new FantasyDate(1372, 1, 15, 12, 0, 0);

        // Act
        var result = date1.IsAfterOrSameAs(date2, _harptosCalendar);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBeforeShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var date1 = new FantasyDate(1372, 1, 15, 12, 0, 0);
        var date2 = new FantasyDate(1372, 1, 16, 12, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => date1.IsBefore(date2, null!));
    }

    [Fact]
    public void IsAfterShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Arrange
        var date1 = new FantasyDate(1372, 1, 15, 12, 0, 0);
        var date2 = new FantasyDate(1372, 1, 16, 12, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => date1.IsAfter(date2, null!));
    }

    [Fact]
    public void ComparisonShouldWorkAcrossYearBoundaries()
    {
        // Arrange
        var endOfYear = new FantasyDate(1372, 12, 30, 23, 59, 59);
        var startOfNextYear = new FantasyDate(1373, 1, 1, 0, 0, 0);

        // Act & Assert
        endOfYear.IsBefore(startOfNextYear, _harptosCalendar).Should().BeTrue();
        startOfNextYear.IsAfter(endOfYear, _harptosCalendar).Should().BeTrue();
    }
}
