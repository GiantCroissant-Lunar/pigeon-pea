using FluentAssertions;
using NodaTime;
using PigeonPea.Plugin.Time.Configurable;
using PigeonPea.Plugin.Time.Harptos;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;
using Xunit;

namespace PigeonPea.Time.Tests;

public class CalendarServiceTests
{
    private readonly WorldClock _worldClock;
    private readonly CalendarService _service;
    private readonly HarptosCalendar _harptosCalendar;
    private readonly ConfigurableCalendar _configurableCalendar;

    public CalendarServiceTests()
    {
        _worldClock = new WorldClock(Instant.FromUtc(2000, 1, 1, 0, 0), realSecondsPerGameSecond: 1.0, ticksPerGameSecond: 1);
        _service = new CalendarService(_worldClock);
        _harptosCalendar = new HarptosCalendar();

        var config = new CalendarConfig
        {
            Name = "TestCalendar",
            TicksPerDay = 86400,
            Months = new List<MonthConfig>()
        };
        _configurableCalendar = new ConfigurableCalendar(config);
    }

    [Fact]
    public void ConstructorShouldThrowArgumentNullExceptionWhenWorldClockIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CalendarService(null!));
    }

    [Fact]
    public void RegisterCalendarShouldThrowArgumentExceptionWhenIdIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.RegisterCalendar(null!, _harptosCalendar));
    }

    [Fact]
    public void RegisterCalendarShouldThrowArgumentExceptionWhenIdIsWhitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.RegisterCalendar("   ", _harptosCalendar));
    }

    [Fact]
    public void RegisterCalendarShouldThrowArgumentNullExceptionWhenCalendarIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.RegisterCalendar("test", null!));
    }

    [Fact]
    public void RegisterCalendarShouldRegisterCalendarSuccessfully()
    {
        // Act
        _service.RegisterCalendar("harptos", _harptosCalendar);

        // Assert
        var retrievedCalendar = _service.GetCalendar("harptos");
        retrievedCalendar.Should().Be(_harptosCalendar);
        _service.IsCalendarRegistered("harptos").Should().BeTrue();
    }

    [Fact]
    public void RegisterCalendarShouldThrowInvalidOperationExceptionWhenCalendarWithSameIdAlreadyRegistered()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _service.RegisterCalendar("harptos", _harptosCalendar));
    }

    [Fact]
    public void RegisterCalendarShouldBeCaseInsensitive()
    {
        // Act
        _service.RegisterCalendar("HARPTOS", _harptosCalendar);

        // Assert - Should be able to retrieve with different cases
        var retrievedCalendar1 = _service.GetCalendar("harptos");
        var retrievedCalendar2 = _service.GetCalendar("Harptos");
        var retrievedCalendar3 = _service.GetCalendar("HARPTOS");

        retrievedCalendar1.Should().Be(_harptosCalendar);
        retrievedCalendar2.Should().Be(_harptosCalendar);
        retrievedCalendar3.Should().Be(_harptosCalendar);
    }

    [Fact]
    public void GetCalendarShouldThrowArgumentExceptionWhenIdIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GetCalendar(null!));
    }

    [Fact]
    public void GetCalendarShouldThrowArgumentExceptionWhenIdIsWhitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GetCalendar("   "));
    }

    [Fact]
    public void GetCalendarShouldThrowKeyNotFoundExceptionWhenCalendarNotFound()
    {
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _service.GetCalendar("nonexistent"));
    }

    [Fact]
    public void ConvertShouldThrowArgumentExceptionWhenFromCalendarIdIsNull()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.Convert(date, null!, "harptos"));
    }

    [Fact]
    public void ConvertShouldThrowArgumentExceptionWhenToCalendarIdIsNull()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.Convert(date, "harptos", null!));
    }

    [Fact]
    public void ConvertShouldThrowKeyNotFoundExceptionWhenFromCalendarNotFound()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _service.Convert(date, "nonexistent", "harptos"));
    }

    [Fact]
    public void ConvertShouldThrowKeyNotFoundExceptionWhenToCalendarNotFound()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _service.Convert(date, "harptos", "nonexistent"));
    }

    [Fact]
    public void ConvertShouldConvertBetweenSameCalendar()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var originalDate = new FantasyDate(1, 1, 15, 12, 30, 45);

        // Act
        var convertedDate = _service.Convert(originalDate, "harptos", "harptos");

        // Assert
        convertedDate.Should().Be(originalDate);
    }

    [Fact]
    public void ConvertShouldConvertBetweenDifferentCalendars()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        _service.RegisterCalendar("configurable", _configurableCalendar);
        var originalDate = new FantasyDate(1, 1, 1, 0, 0, 0); // Hammer 1, Year 1, midnight

        // Act
        var convertedDate = _service.Convert(originalDate, "harptos", "configurable");

        // Assert
        // Both calendars start at the same epoch, so the date should be equivalent
        convertedDate.Should().Be(new FantasyDate(1, 1, 1, 0, 0, 0));
    }

    [Fact]
    public void ToRealWorldShouldThrowArgumentExceptionWhenCalendarIdIsNull()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);
        var zone = DateTimeZone.Utc;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.ToRealWorld(date, null!, zone));
    }

    [Fact]
    public void ToRealWorldShouldThrowKeyNotFoundExceptionWhenCalendarNotFound()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var date = new FantasyDate(1, 1, 1, 0, 0, 0);
        var zone = DateTimeZone.Utc;

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _service.ToRealWorld(date, "nonexistent", zone));
    }

    [Fact]
    public void ToRealWorldShouldConvertFantasyDateToRealWorld()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var fantasyDate = new FantasyDate(1, 1, 1, 0, 0, 0); // Hammer 1, Year 1, midnight
        var zone = DateTimeZone.Utc;

        // Act
        var realDate = _service.ToRealWorld(fantasyDate, "harptos", zone);

        // Assert
        realDate.Should().Be(zone.AtStrictly(LocalDateTime.FromDateTime(new DateTime(2000, 1, 1, 0, 0, 0))));
    }

    [Fact]
    public void ToRealWorldShouldConvertFantasyDateWithTimeToRealWorld()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var fantasyDate = new FantasyDate(1, 1, 1, 12, 30, 45); // Hammer 1, Year 1, 12:30:45
        var zone = DateTimeZone.Utc;

        // Act
        var realDate = _service.ToRealWorld(fantasyDate, "harptos", zone);

        // Assert
        realDate.Should().Be(zone.AtStrictly(LocalDateTime.FromDateTime(new DateTime(2000, 1, 1, 12, 30, 45))));
    }

    [Fact]
    public void FromRealWorldShouldThrowArgumentExceptionWhenCalendarIdIsNull()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var realDate = DateTimeZone.Utc.AtStrictly(LocalDateTime.FromDateTime(new DateTime(2000, 1, 1, 0, 0, 0)));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.FromRealWorld(realDate, null!));
    }

    [Fact]
    public void FromRealWorldShouldThrowKeyNotFoundExceptionWhenCalendarNotFound()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var realDate = DateTimeZone.Utc.AtStrictly(LocalDateTime.FromDateTime(new DateTime(2000, 1, 1, 0, 0, 0)));

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _service.FromRealWorld(realDate, "nonexistent"));
    }

    [Fact]
    public void FromRealWorldShouldConvertRealWorldToFantasyDate()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var realDate = DateTimeZone.Utc.AtStrictly(LocalDateTime.FromDateTime(new DateTime(2000, 1, 1, 0, 0, 0)));
        var zone = DateTimeZone.Utc;

        // Act
        var fantasyDate = _service.FromRealWorld(realDate, "harptos");

        // Assert
        fantasyDate.Should().Be(new FantasyDate(1, 1, 1, 0, 0, 0));
    }

    [Fact]
    public void FromRealWorldShouldConvertRealWorldWithTimeToFantasyDate()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var realDate = DateTimeZone.Utc.AtStrictly(LocalDateTime.FromDateTime(new DateTime(2000, 1, 1, 12, 30, 45)));
        var zone = DateTimeZone.Utc;

        // Act
        var fantasyDate = _service.FromRealWorld(realDate, "harptos");

        // Assert
        fantasyDate.Should().Be(new FantasyDate(1, 1, 1, 12, 30, 45));
    }

    [Fact]
    public void NowShouldThrowArgumentExceptionWhenCalendarIdIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.Now(null!));
    }

    [Fact]
    public void NowShouldThrowKeyNotFoundExceptionWhenCalendarNotFound()
    {
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _service.Now("nonexistent"));
    }

    [Fact]
    public void NowShouldReturnCurrentFantasyDate()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var before = SystemClock.Instance.GetCurrentInstant();

        // Act
        var fantasyDate = _service.Now("harptos");

        var after = SystemClock.Instance.GetCurrentInstant();

        // Assert
        fantasyDate.Should().NotBeNull();

        // Convert back to real world to verify it's within the expected range
        var realDate = _service.ToRealWorld(fantasyDate, "harptos", DateTimeZone.Utc);
        Assert.True(realDate.ToInstant() >= before);
        Assert.True(realDate.ToInstant() <= after);
    }

    [Fact]
    public void GetRegisteredCalendarIdsShouldReturnAllRegisteredIds()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        _service.RegisterCalendar("configurable", _configurableCalendar);

        // Act
        var ids = _service.GetRegisteredCalendarIds();

        // Assert
        ids.Should().HaveCount(2);
        ids.Should().Contain("harptos");
        ids.Should().Contain("configurable");
    }

    [Fact]
    public void GetRegisteredCalendarIdsShouldReturnReadOnlyCollection()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);

        // Act
        var ids = _service.GetRegisteredCalendarIds();

        // Assert
        ids.Should().BeOfType<System.Collections.Generic.IReadOnlyCollection<string>>();
    }

    [Fact]
    public void IsCalendarRegisteredShouldReturnFalseWhenIdIsNull()
    {
        // Act & Assert
        _service.IsCalendarRegistered(null!).Should().BeFalse();
    }

    [Fact]
    public void IsCalendarRegisteredShouldReturnFalseWhenIdIsWhitespace()
    {
        // Act & Assert
        _service.IsCalendarRegistered("   ").Should().BeFalse();
    }

    [Fact]
    public void IsCalendarRegisteredShouldReturnFalseWhenCalendarNotRegistered()
    {
        // Act & Assert
        _service.IsCalendarRegistered("nonexistent").Should().BeFalse();
    }

    [Fact]
    public void IsCalendarRegisteredShouldReturnTrueWhenCalendarIsRegistered()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);

        // Act & Assert
        _service.IsCalendarRegistered("harptos").Should().BeTrue();
        _service.IsCalendarRegistered("HARPTOS").Should().BeTrue(); // Case insensitive
    }

    [Fact]
    public void RoundTripConversionShouldWork()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        var originalFantasyDate = new FantasyDate(1, 1, 15, 12, 30, 45);
        var zone = DateTimeZone.Utc;

        // Act - Fantasy -> Real -> Fantasy
        var realDate = _service.ToRealWorld(originalFantasyDate, "harptos", zone);
        var convertedFantasyDate = _service.FromRealWorld(realDate, "harptos");

        // Assert
        convertedFantasyDate.Should().Be(originalFantasyDate);
    }

    [Fact]
    public void MultipleCalendarOperationsShouldWorkConcurrently()
    {
        // Arrange
        _service.RegisterCalendar("harptos", _harptosCalendar);
        _service.RegisterCalendar("configurable", _configurableCalendar);

        // Act & Assert - Multiple operations should work without interference
        var harptosDate = _service.Now("harptos");
        var configurableDate = _service.Now("configurable");

        harptosDate.Should().NotBeNull();
        configurableDate.Should().NotBeNull();

        var harptosIds = _service.GetRegisteredCalendarIds();
        harptosIds.Should().HaveCount(2);
        harptosIds.Should().Contain("harptos");
        harptosIds.Should().Contain("configurable");
    }
}