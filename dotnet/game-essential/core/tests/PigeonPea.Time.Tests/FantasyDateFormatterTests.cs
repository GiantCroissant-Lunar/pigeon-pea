using FluentAssertions;
using PigeonPea.Plugin.Time.Harptos;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;
using Xunit;

namespace PigeonPea.Time.Tests;

public class FantasyDateFormatterTests
{
    private readonly HarptosCalendar _harptosCalendar;
    private readonly FantasyDateFormatter _formatter;

    public FantasyDateFormatterTests()
    {
        _harptosCalendar = new HarptosCalendar();
        _formatter = new FantasyDateFormatter(_harptosCalendar);
    }

    [Fact]
    public void ConstructorShouldThrowArgumentNullExceptionWhenCalendarDefinitionIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FantasyDateFormatter(null!));
    }

    [Theory]
    [InlineData("yyyy-MM-dd HH:mm:ss", "0001-01-01 00:00:00")]
    [InlineData("yyyy/MM/dd HH:mm", "0001/01/01 00:00")]
    [InlineData("dd-MM-yyyy", "01-01-0001 00:00:00")]
    [InlineData("MM/dd/yyyy", "01/01-0001 00:00:00")]
    [InlineData("HH:mm:ss", "00:00:00")]
    [InlineData("H:m", "0:00")]
    [InlineData("m:s", "0:00")]
    [InlineData("s", "0")]
    public void FormatShouldReplaceBasicPatterns(string pattern, string expected)
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act
        var result = _formatter.Format(date, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1234, 5, 15, 14, 30, 45, "yyyy-MM-dd HH:mm:ss", "1234-05-15 14:30:45")]
    [InlineData(999, 12, 31, 23, 59, 59, "yyyy/MM/dd HH:mm:ss", "0999/12/31 23:59:59")]
    [InlineData(2024, 2, 29, 12, 0, 0, "dd-MM-yyyy HH:mm", "29-02-2024 12:00:00")]
    public void FormatShouldFormatComplexDates(int year, int month, int day, int hour, int minute, int second, string pattern, string expected)
    {
        // Arrange
        var date = new FantasyDate(year, month, day, hour, minute, second);

        // Act
        var result = _formatter.Format(date, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "y", "1")]
    [InlineData(10, "y", "10")]
    [InlineData(100, "y", "100")]
    [InlineData(1000, "y", "1000")]
    [InlineData(1, "yy", "01")]
    [InlineData(10, "yy", "10")]
    [InlineData(99, "yy", "99")]
    [InlineData(100, "yy", "100")]
    [InlineData(1000, "yy", "1000")]
    public void FormatShouldFormatYearPatterns(int year, string pattern, string expected)
    {
        // Arrange
        var date = new FantasyDate(year, 1, 1, 0, 0, 0);

        // Act
        var result = _formatter.Format(date, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "M", "1")]
    [InlineData(9, "M", "9")]
    [InlineData(10, "M", "10")]
    [InlineData(12, "M", "12")]
    public void FormatShouldFormatMonthPatterns(int month, string pattern, string expected)
    {
        // Arrange
        var date = new FantasyDate(1, month, 1, 0, 0, 0);

        // Act
        var result = _formatter.Format(date, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "d", "1")]
    [InlineData(9, "d", "9")]
    [InlineData(10, "d", "10")]
    [InlineData(31, "d", "31")]
    public void FormatShouldFormatDayPatterns(int day, string pattern, string expected)
    {
        // Arrange
        var date = new FantasyDate(1, 1, day, 0, 0, 0);

        // Act
        var result = _formatter.Format(date, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "H", "0")]
    [InlineData(1, "H", "1")]
    [InlineData(12, "H", "12")]
    [InlineData(23, "H", "23")]
    public void FormatShouldFormatHourPatterns(int hour, string pattern, string expected)
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, hour, 0, 0);

        // Act
        var result = _formatter.Format(date, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "m", "0")]
    [InlineData(1, "m", "1")]
    [InlineData(30, "m", "30")]
    [InlineData(59, "m", "59")]
    public void FormatShouldFormatMinutePatterns(int minute, string pattern, string expected)
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, minute, 0);

        // Act
        var result = _formatter.Format(date, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "s", "0")]
    [InlineData(1, "s", "1")]
    [InlineData(30, "s", "30")]
    [InlineData(59, "s", "59")]
    public void FormatShouldFormatSecondPatterns(int second, string pattern, string expected)
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, second);

        // Act
        var result = _formatter.Format(date, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatShouldUseDefaultMonthNamesWhenMonthNamesNotProvided()
    {
        // Arrange
        var date = new FantasyDate(1, 5, 15, 12, 30, 45);

        // Act
        var result1 = _formatter.Format(date, "MMMM dd, yyyy");
        var result2 = _formatter.Format(date, "MMM dd, yyyy");

        // Assert
        result1.Should().Be("Month 5 15, 0001");
        result2.Should().Be("M5 15, 0001");
    }

    [Fact]
    public void FormatShouldUseCustomMonthNamesWhenMonthNamesProvided()
    {
        // Arrange
        var date = new FantasyDate(1, 5, 15, 12, 30, 45);
        var monthNames = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        // Act
        var result1 = _formatter.Format(date, "MMMM dd, yyyy", monthNames);
        var result2 = _formatter.Format(date, "MMM dd, yyyy", monthNames);

        // Assert
        result1.Should().Be("May 15, 0001");
        result2.Should().Be("May 15, 0001");
    }

    [Fact]
    public void FormatShouldHandleEmptyMonthNamesArray()
    {
        // Arrange
        var date = new FantasyDate(1, 5, 15, 12, 30, 45);
        var emptyMonthNames = Array.Empty<string>();

        // Act
        var result1 = _formatter.Format(date, "MMMM dd, yyyy", emptyMonthNames);
        var result2 = _formatter.Format(date, "MMM dd, yyyy", emptyMonthNames);

        // Assert
        result1.Should().Be("Month 5 15, 0001");
        result2.Should().Be("M5 15, 0001");
    }

    [Fact]
    public void FormatShouldHandleMonthNamesArrayOutOfBounds()
    {
        // Arrange
        var date = new FantasyDate(1, 15, 15, 12, 30, 45); // Month 15 (beyond array bounds)
        var monthNames = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        // Act
        var result = _formatter.Format(date, "MMMM dd, yyyy", monthNames);

        // Assert
        result.Should().Be("December 15, 0001"); // Should use last month name in array
    }

    [Fact]
    public void FormatShouldHandleShortMonthNames()
    {
        // Arrange
        var date = new FantasyDate(1, 5, 15, 12, 30, 45);
        var shortMonthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        // Act
        var result = _formatter.Format(date, "MMM dd, yyyy", shortMonthNames);

        // Assert
        result.Should().Be("May 15, 0001"); // Should use "May" from short month names
    }

    [Fact]
    public void FormatDefaultShouldUseIsoPattern()
    {
        // Arrange
        var date = new FantasyDate(2024, 5, 15, 14, 30, 45);

        // Act
        var result = _formatter.FormatDefault(date);

        // Assert
        result.Should().Be("2024-05-15 14:30:45");
    }

    [Fact]
    public void FormatDefaultShouldUseCustomMonthNamesWhenProvided()
    {
        // Arrange
        var date = new FantasyDate(2024, 5, 15, 14, 30, 45);
        var monthNames = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        // Act
        var result = _formatter.FormatDefault(date, monthNames);

        // Assert
        result.Should().Be("2024-05-15 14:30:45"); // Default pattern doesn't use month names
    }

    [Fact]
    public void FormatLongDateShouldUseLongDatePattern()
    {
        // Arrange
        var date = new FantasyDate(2024, 5, 15, 14, 30, 45);

        // Act
        var result = _formatter.FormatLongDate(date);

        // Assert
        result.Should().Be("May 15, 2024");
    }

    [Fact]
    public void FormatLongDateShouldUseCustomMonthNamesWhenProvided()
    {
        // Arrange
        var date = new FantasyDate(2024, 5, 15, 14, 30, 45);
        var monthNames = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        // Act
        var result = _formatter.FormatLongDate(date, monthNames);

        // Assert
        result.Should().Be("May 15, 2024");
    }

    [Fact]
    public void FormatShortDateShouldUseShortDatePattern()
    {
        // Arrange
        var date = new FantasyDate(2024, 5, 15, 14, 30, 45);

        // Act
        var result = _formatter.FormatShortDate(date);

        // Assert
        result.Should().Be("05/15/2024");
    }

    [Fact]
    public void FormatShortDateShouldUseCustomMonthNamesWhenProvided()
    {
        // Arrange
        var date = new FantasyDate(2024, 5, 15, 14, 30, 45);
        var monthNames = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        // Act
        var result = _formatter.FormatShortDate(date, monthNames);

        // Assert
        result.Should().Be("05/15/2024"); // Short date pattern doesn't use month names
    }

    [Fact]
    public void FormatTimeShouldUseTimePattern()
    {
        // Arrange
        var date = new FantasyDate(2024, 5, 15, 14, 30, 45);

        // Act
        var result = _formatter.FormatTime(date);

        // Assert
        result.Should().Be("14:30:45");
    }

    [Fact]
    public void FormatReadableShouldUseReadablePattern()
    {
        // Arrange
        var date = new FantasyDate(2024, 5, 15, 14, 30, 45);

        // Act
        var result = _formatter.FormatReadable(date);

        // Assert
        result.Should().Be("May 15, 2024 14:30");
    }

    [Fact]
    public void FormatReadableShouldUseCustomMonthNamesWhenProvided()
    {
        // Arrange
        var date = new FantasyDate(2024, 5, 15, 14, 30, 45);
        var monthNames = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        // Act
        var result = _formatter.FormatReadable(date, monthNames);

        // Assert
        result.Should().Be("May 15, 2024 14:30");
    }

    [Theory]
    [InlineData("Year yyyy, Month dd, HH:mm", "Year 2024, May 15, 14:30:45")]
    [InlineData("Date: dd/MM/yyyy HH:mm", "Date: 05/15/2024 14:30:45")]
    [InlineData("yyyy-MM-dd'T'HH:mm:ss", "2024-05-15'T14:30:45")]
    [InlineData("d/M/yyyy H:mm", "15/05/2024 14:30")]
    [InlineData("MM/dd/yyyy", "05/15/2024")]
    public void FormatShouldHandleComplexPatterns(string pattern, string expected)
    {
        // Arrange
        var date = new FantasyDate(2024, 5, 15, 14, 30, 45);

        // Act
        var result = _formatter.Format(date, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatShouldHandleZeroPaddedNumbers()
    {
        // Arrange
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act
        var result = _formatter.Format(date, "yyyy-MM-dd HH:mm:ss");

        // Assert
        result.Should().Be("0001-01-01 00:00:00");
    }

    [Fact]
    public void FormatShouldHandleLargeNumbers()
    {
        // Arrange
        var date = new FantasyDate(99999, 12, 31, 23, 59, 59);

        // Act
        var result = _formatter.Format(date, "yyyy-MM-dd HH:mm:ss");

        // Assert
        result.Should().Be("99999-12-31 23:59:59");
    }
}
