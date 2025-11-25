using NodaTime;

namespace PigeonPea.Platform.Contracts.Time;

/// <summary>
/// Manages multiple calendars and provides conversion between fantasy and real-world time.
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Registers a calendar with a unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the calendar.</param>
    /// <param name="calendar">The calendar definition to register.</param>
    /// <exception cref="InvalidOperationException">Thrown when a calendar with the same ID is already registered.</exception>
    void RegisterCalendar(string id, ICalendarDefinition calendar);

    /// <summary>
    /// Gets a calendar by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the calendar to retrieve.</param>
    /// <returns>The calendar definition.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no calendar with the specified ID is found.</exception>
    ICalendarDefinition GetCalendar(string id);

    /// <summary>
    /// Converts a date from one fantasy calendar to another.
    /// </summary>
    /// <param name="date">The date to convert.</param>
    /// <param name="fromCalendarId">The identifier of the source calendar.</param>
    /// <param name="toCalendarId">The identifier of the target calendar.</param>
    /// <returns>The equivalent date in the target calendar.</returns>
    FantasyDate Convert(FantasyDate date, string fromCalendarId, string toCalendarId);

    /// <summary>
    /// Converts a fantasy date to real-world time.
    /// </summary>
    /// <param name="date">The fantasy date to convert.</param>
    /// <param name="calendarId">The identifier of the calendar containing the fantasy date.</param>
    /// <param name="zone">The time zone for the resulting real-world date.</param>
    /// <returns>The equivalent real-world zoned date/time.</returns>
    ZonedDateTime ToRealWorld(FantasyDate date, string calendarId, DateTimeZone zone);

    /// <summary>
    /// Converts real-world time to a fantasy date.
    /// </summary>
    /// <param name="realDate">The real-world date/time to convert.</param>
    /// <param name="calendarId">The identifier of the target calendar.</param>
    /// <returns>The equivalent fantasy date in the specified calendar.</returns>
    FantasyDate FromRealWorld(ZonedDateTime realDate, string calendarId);

    /// <summary>
    /// Gets the current fantasy date for a calendar based on system time.
    /// </summary>
    /// <param name="calendarId">The identifier of the calendar.</param>
    /// <returns>The current fantasy date in the specified calendar.</returns>
    FantasyDate Now(string calendarId);
}
