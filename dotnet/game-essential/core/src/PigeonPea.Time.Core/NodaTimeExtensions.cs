using NodaTime;
using PigeonPea.Time.Contracts;

namespace PigeonPea.Time.Core;

/// <summary>
/// Extension methods for NodaTime types to provide fluent API functionality with fantasy dates.
/// </summary>
public static class NodaTimeExtensions
{
    /// <summary>
    /// Converts this real-world zoned date/time to a fantasy date using the specified calendar service.
    /// </summary>
    /// <param name="realDate">The real-world zoned date/time to convert.</param>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="calendarId">The ID of the target calendar.</param>
    /// <returns>The equivalent fantasy date in the specified calendar.</returns>
    public static FantasyDate ToFantasyDate(this ZonedDateTime realDate, ICalendarService calendarService, string calendarId)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        return calendarService.FromRealWorld(realDate, calendarId);
    }

    /// <summary>
    /// Converts this real-world instant to a fantasy date using the specified calendar service.
    /// </summary>
    /// <param name="instant">The real-world instant to convert.</param>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="calendarId">The ID of the target calendar.</param>
    /// <returns>The equivalent fantasy date in the specified calendar.</returns>
    public static FantasyDate ToFantasyDate(this Instant instant, ICalendarService calendarService, string calendarId)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        // Convert instant to UTC zoned date time first
        var utcDateTime = instant.InUtc();
        return calendarService.FromRealWorld(utcDateTime, calendarId);
    }

    /// <summary>
    /// Converts this real-world zoned date/time to a fantasy date using the specified calendar service and UTC zone.
    /// </summary>
    /// <param name="realDate">The real-world zoned date/time to convert.</param>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="calendarId">The ID of the target calendar.</param>
    /// <returns>The equivalent fantasy date in the specified calendar.</returns>
    public static FantasyDate ToFantasyDateUtc(this ZonedDateTime realDate, ICalendarService calendarService, string calendarId)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        // Convert to UTC first, then to fantasy date
        var utcDateTime = realDate.ToInstant().InUtc();
        return calendarService.FromRealWorld(utcDateTime, calendarId);
    }

    /// <summary>
    /// Converts this real-world zoned date/time to a fantasy date using the specified calendar service and local system zone.
    /// </summary>
    /// <param name="realDate">The real-world zoned date/time to convert.</param>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="calendarId">The ID of the target calendar.</param>
    /// <returns>The equivalent fantasy date in the specified calendar.</returns>
    public static FantasyDate ToFantasyDateLocal(this ZonedDateTime realDate, ICalendarService calendarService, string calendarId)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        // Convert to local system zone first, then to fantasy date
        var localZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
        var localDateTime = realDate.ToInstant().InZone(localZone);
        return calendarService.FromRealWorld(localDateTime, calendarId);
    }

    /// <summary>
    /// Gets the current fantasy date for the specified calendar using the system time.
    /// </summary>
    /// <param name="calendarService">The calendar service to use.</param>
    /// <param name="calendarId">The ID of the calendar.</param>
    /// <returns>The current fantasy date in the specified calendar.</returns>
    public static FantasyDate GetNow(this ICalendarService calendarService, string calendarId)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        return calendarService.Now(calendarId);
    }

    /// <summary>
    /// Creates a ZonedDateTime from a fantasy date using the specified calendar service and time zone.
    /// </summary>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="fantasyDate">The fantasy date to convert.</param>
    /// <param name="calendarId">The ID of the calendar containing the fantasy date.</param>
    /// <param name="zone">The time zone for the resulting real-world date.</param>
    /// <returns>The equivalent real-world zoned date/time.</returns>
    public static ZonedDateTime FromFantasyDate(this ICalendarService calendarService, FantasyDate fantasyDate, string calendarId, DateTimeZone zone)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));
        if (zone == null) throw new ArgumentNullException(nameof(zone));

        return calendarService.ToRealWorld(fantasyDate, calendarId, zone);
    }

    /// <summary>
    /// Creates a ZonedDateTime from a fantasy date using the specified calendar service and UTC time zone.
    /// </summary>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="fantasyDate">The fantasy date to convert.</param>
    /// <param name="calendarId">The ID of the calendar containing the fantasy date.</param>
    /// <returns>The equivalent real-world zoned date/time in UTC.</returns>
    public static ZonedDateTime FromFantasyDateUtc(this ICalendarService calendarService, FantasyDate fantasyDate, string calendarId)
    {
        return calendarService.FromFantasyDate(fantasyDate, calendarId, DateTimeZone.Utc);
    }

    /// <summary>
    /// Creates a ZonedDateTime from a fantasy date using the specified calendar service and local system time zone.
    /// </summary>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="fantasyDate">The fantasy date to convert.</param>
    /// <param name="calendarId">The ID of the calendar containing the fantasy date.</param>
    /// <returns>The equivalent real-world zoned date/time in the local time zone.</returns>
    public static ZonedDateTime FromFantasyDateLocal(this ICalendarService calendarService, FantasyDate fantasyDate, string calendarId)
    {
        var localZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
        return calendarService.FromFantasyDate(fantasyDate, calendarId, localZone);
    }

    /// <summary>
    /// Creates an Instant from a fantasy date using the specified calendar service.
    /// </summary>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="fantasyDate">The fantasy date to convert.</param>
    /// <param name="calendarId">The ID of the calendar containing the fantasy date.</param>
    /// <returns>The equivalent instant in UTC.</returns>
    public static Instant FromFantasyDateToInstant(this ICalendarService calendarService, FantasyDate fantasyDate, string calendarId)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        return calendarService.FromFantasyDateUtc(fantasyDate, calendarId).ToInstant();
    }

    /// <summary>
    /// Gets the current fantasy date for the specified calendar using UTC time.
    /// </summary>
    /// <param name="calendarService">The calendar service to use.</param>
    /// <param name="calendarId">The ID of the calendar.</param>
    /// <returns>The current fantasy date in the specified calendar.</returns>
    public static FantasyDate GetNowUtc(this ICalendarService calendarService, string calendarId)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        // Get current UTC instant and convert to fantasy date
        var now = SystemClock.Instance.GetCurrentInstant();
        var utcDateTime = now.InUtc();
        return calendarService.FromRealWorld(utcDateTime, calendarId);
    }

    /// <summary>
    /// Gets the current fantasy date for the specified calendar using local system time.
    /// </summary>
    /// <param name="calendarService">The calendar service to use.</param>
    /// <param name="calendarId">The ID of the calendar.</param>
    /// <returns>The current fantasy date in the specified calendar.</returns>
    public static FantasyDate GetNowLocal(this ICalendarService calendarService, string calendarId)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        // Get current instant and convert to local time, then to fantasy date
        var now = SystemClock.Instance.GetCurrentInstant();
        var localZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
        var localDateTime = now.InZone(localZone);
        return calendarService.FromRealWorld(localDateTime, calendarId);
    }
}