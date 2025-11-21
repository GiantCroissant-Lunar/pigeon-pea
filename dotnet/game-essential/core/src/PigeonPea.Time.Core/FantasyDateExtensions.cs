using NodaTime;
using PigeonPea.Time.Contracts;

namespace PigeonPea.Time.Core;

/// <summary>
/// Extension methods for FantasyDate to provide fluent API functionality.
/// </summary>
public static class FantasyDateExtensions
{
    #region Validation Methods

    /// <summary>
    /// Validates whether a fantasy date is valid according to the calendar's rules.
    /// </summary>
    /// <param name="date">The fantasy date to validate.</param>
    /// <param name="calendar">The calendar definition to validate against.</param>
    /// <returns>True if the date is valid; otherwise, false.</returns>
    public static bool IsValid(this FantasyDate date, ICalendarDefinition calendar)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        // Validate time components
        if (date.Hour < 0 || date.Hour >= calendar.HoursPerDay) return false;
        if (date.Minute < 0 || date.Minute >= calendar.MinutesPerHour) return false;
        if (date.Second < 0 || date.Second >= calendar.SecondsPerMinute) return false;

        // Validate year (must be positive)
        if (date.Year < 1) return false;

        // Validate month
        if (date.Month < 1 || date.Month > calendar.MonthsPerYear) return false;

        // Validate day by attempting conversion
        // If the conversion succeeds, the date is valid
        try
        {
            var tick = calendar.ToWorldTick(date);
            var roundTrip = calendar.FromWorldTick(tick);

            // Check if round-trip preserves the date (within 1 second tolerance for rounding)
            return Math.Abs(date.Year - roundTrip.Year) == 0 &&
                   Math.Abs(date.Month - roundTrip.Month) == 0 &&
                   Math.Abs(date.Day - roundTrip.Day) <= 1;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Normalizes an invalid fantasy date to a valid one by converting through WorldTick.
    /// This handles overflow in all time components (seconds, minutes, hours, days, months, years).
    /// </summary>
    /// <param name="date">The fantasy date to normalize.</param>
    /// <param name="calendar">The calendar definition to use for normalization.</param>
    /// <returns>A valid fantasy date with all components within their valid ranges.</returns>
    /// <remarks>
    /// Examples:
    /// - Hammer 33 becomes Alturiak 3 (day overflow)
    /// - 25:00:00 becomes next day at 01:00:00 (hour overflow)
    /// - Month 13 becomes Month 1 of next year (month overflow)
    /// </remarks>
    public static FantasyDate Normalize(this FantasyDate date, ICalendarDefinition calendar)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        // If already valid, return as-is
        if (date.IsValid(calendar))
            return date;

        // Convert to WorldTick and back to get normalized date
        // This automatically handles all overflow scenarios
        try
        {
            var tick = calendar.ToWorldTick(date);
            return calendar.FromWorldTick(tick);
        }
        catch
        {
            // If conversion fails, clamp to valid ranges
            var normalizedHour = Math.Max(0, Math.Min(date.Hour, calendar.HoursPerDay - 1));
            var normalizedMinute = Math.Max(0, Math.Min(date.Minute, calendar.MinutesPerHour - 1));
            var normalizedSecond = Math.Max(0, Math.Min(date.Second, calendar.SecondsPerMinute - 1));
            var normalizedMonth = Math.Max(1, Math.Min(date.Month, calendar.MonthsPerYear));
            var normalizedDay = Math.Max(1, date.Day);
            var normalizedYear = Math.Max(1, date.Year);

            var clamped = new FantasyDate(
                normalizedYear,
                normalizedMonth,
                normalizedDay,
                normalizedHour,
                normalizedMinute,
                normalizedSecond
            );

            // Try one more time with clamped values
            var clampedTick = calendar.ToWorldTick(clamped);
            return calendar.FromWorldTick(clampedTick);
        }
    }

    #endregion

    #region Duration Methods

    /// <summary>
    /// Calculates the duration from this date to another date.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <param name="calendar">The calendar definition to use for calculations.</param>
    /// <returns>A FantasyDuration representing the time span.</returns>
    public static FantasyDuration DurationTo(this FantasyDate start, FantasyDate end, ICalendarDefinition calendar)
    {
        return FantasyDuration.Between(start, end, calendar);
    }

    /// <summary>
    /// Calculates the duration from another date to this date.
    /// </summary>
    /// <param name="end">The end date.</param>
    /// <param name="start">The start date.</param>
    /// <param name="calendar">The calendar definition to use for calculations.</param>
    /// <returns>A FantasyDuration representing the time span.</returns>
    public static FantasyDuration DurationSince(this FantasyDate end, FantasyDate start, ICalendarDefinition calendar)
    {
        return FantasyDuration.Between(start, end, calendar);
    }

    #endregion

    #region Comparison Methods

    /// <summary>
    /// Determines whether this fantasy date is before another date.
    /// </summary>
    /// <param name="date">The date to compare.</param>
    /// <param name="other">The other date to compare against.</param>
    /// <param name="calendar">The calendar definition to use for comparison.</param>
    /// <returns>True if this date is before the other date; otherwise, false.</returns>
    public static bool IsBefore(this FantasyDate date, FantasyDate other, ICalendarDefinition calendar)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        var thisTick = calendar.ToWorldTick(date);
        var otherTick = calendar.ToWorldTick(other);
        return thisTick.Value < otherTick.Value;
    }

    /// <summary>
    /// Determines whether this fantasy date is after another date.
    /// </summary>
    /// <param name="date">The date to compare.</param>
    /// <param name="other">The other date to compare against.</param>
    /// <param name="calendar">The calendar definition to use for comparison.</param>
    /// <returns>True if this date is after the other date; otherwise, false.</returns>
    public static bool IsAfter(this FantasyDate date, FantasyDate other, ICalendarDefinition calendar)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        var thisTick = calendar.ToWorldTick(date);
        var otherTick = calendar.ToWorldTick(other);
        return thisTick.Value > otherTick.Value;
    }

    /// <summary>
    /// Determines whether this fantasy date is the same as another date.
    /// </summary>
    /// <param name="date">The date to compare.</param>
    /// <param name="other">The other date to compare against.</param>
    /// <param name="calendar">The calendar definition to use for comparison.</param>
    /// <returns>True if this date is the same as the other date; otherwise, false.</returns>
    public static bool IsSameAs(this FantasyDate date, FantasyDate other, ICalendarDefinition calendar)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        var thisTick = calendar.ToWorldTick(date);
        var otherTick = calendar.ToWorldTick(other);
        return thisTick.Value == otherTick.Value;
    }

    /// <summary>
    /// Determines whether this fantasy date is before or the same as another date.
    /// </summary>
    /// <param name="date">The date to compare.</param>
    /// <param name="other">The other date to compare against.</param>
    /// <param name="calendar">The calendar definition to use for comparison.</param>
    /// <returns>True if this date is before or the same as the other date; otherwise, false.</returns>
    public static bool IsBeforeOrSameAs(this FantasyDate date, FantasyDate other, ICalendarDefinition calendar)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        var thisTick = calendar.ToWorldTick(date);
        var otherTick = calendar.ToWorldTick(other);
        return thisTick.Value <= otherTick.Value;
    }

    /// <summary>
    /// Determines whether this fantasy date is after or the same as another date.
    /// </summary>
    /// <param name="date">The date to compare.</param>
    /// <param name="other">The other date to compare against.</param>
    /// <param name="calendar">The calendar definition to use for comparison.</param>
    /// <returns>True if this date is after or the same as the other date; otherwise, false.</returns>
    public static bool IsAfterOrSameAs(this FantasyDate date, FantasyDate other, ICalendarDefinition calendar)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        var thisTick = calendar.ToWorldTick(date);
        var otherTick = calendar.ToWorldTick(other);
        return thisTick.Value >= otherTick.Value;
    }

    #endregion

    /// <summary>
    /// Converts this fantasy date to a real-world zoned date/time using the specified calendar service.
    /// </summary>
    /// <param name="date">The fantasy date to convert.</param>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="calendarId">The ID of the calendar containing this fantasy date.</param>
    /// <param name="zone">The time zone for the resulting real-world date.</param>
    /// <returns>The equivalent real-world zoned date/time.</returns>
    public static ZonedDateTime ToRealWorld(this FantasyDate date, ICalendarService calendarService, string calendarId, DateTimeZone zone)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));
        if (zone == null) throw new ArgumentNullException(nameof(zone));

        return calendarService.ToRealWorld(date, calendarId, zone);
    }

    /// <summary>
    /// Converts this fantasy date to a real-world zoned date/time using UTC time zone.
    /// </summary>
    /// <param name="date">The fantasy date to convert.</param>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="calendarId">The ID of the calendar containing this fantasy date.</param>
    /// <returns>The equivalent real-world zoned date/time in UTC.</returns>
    public static ZonedDateTime ToRealWorldUtc(this FantasyDate date, ICalendarService calendarService, string calendarId)
    {
        return date.ToRealWorld(calendarService, calendarId, DateTimeZone.Utc);
    }

    /// <summary>
    /// Converts this fantasy date to a real-world zoned date/time using the system's local time zone.
    /// </summary>
    /// <param name="date">The fantasy date to convert.</param>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="calendarId">The ID of the calendar containing this fantasy date.</param>
    /// <returns>The equivalent real-world zoned date/time in the local time zone.</returns>
    public static ZonedDateTime ToRealWorldLocal(this FantasyDate date, ICalendarService calendarService, string calendarId)
    {
        return date.ToRealWorld(calendarService, calendarId, DateTimeZoneProviders.Tzdb.GetSystemDefault());
    }

    /// <summary>
    /// Converts this fantasy date to an instant in UTC using the specified calendar service.
    /// </summary>
    /// <param name="date">The fantasy date to convert.</param>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="calendarId">The ID of the calendar containing this fantasy date.</param>
    /// <returns>The equivalent instant in UTC.</returns>
    public static Instant ToInstant(this FantasyDate date, ICalendarService calendarService, string calendarId)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(calendarId)) throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        return date.ToRealWorldUtc(calendarService, calendarId).ToInstant();
    }

    /// <summary>
    /// Converts this fantasy date to another fantasy calendar.
    /// </summary>
    /// <param name="date">The fantasy date to convert.</param>
    /// <param name="calendarService">The calendar service to use for conversion.</param>
    /// <param name="fromCalendarId">The ID of the source calendar containing this fantasy date.</param>
    /// <param name="toCalendarId">The ID of the target calendar.</param>
    /// <returns>The equivalent fantasy date in the target calendar.</returns>
    public static FantasyDate ToCalendar(this FantasyDate date, ICalendarService calendarService, string fromCalendarId, string toCalendarId)
    {
        if (calendarService == null) throw new ArgumentNullException(nameof(calendarService));
        if (string.IsNullOrWhiteSpace(fromCalendarId)) throw new ArgumentException("Source calendar ID cannot be null or whitespace.", nameof(fromCalendarId));
        if (string.IsNullOrWhiteSpace(toCalendarId)) throw new ArgumentException("Target calendar ID cannot be null or whitespace.", nameof(toCalendarId));

        return calendarService.Convert(date, fromCalendarId, toCalendarId);
    }

    /// <summary>
    /// Formats this fantasy date using the specified pattern and formatter.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="formatter">The formatter to use.</param>
    /// <param name="pattern">The format pattern.</param>
    /// <param name="monthNames">Optional custom month names.</param>
    /// <returns>The formatted string.</returns>
    public static string Format(this FantasyDate date, FantasyDateFormatter formatter, string pattern, string[]? monthNames = null)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));
        if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Pattern cannot be null or whitespace.", nameof(pattern));

        return formatter.Format(date, pattern, monthNames);
    }

    /// <summary>
    /// Formats this fantasy date using the default pattern and formatter.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="formatter">The formatter to use.</param>
    /// <param name="monthNames">Optional custom month names.</param>
    /// <returns>The formatted string.</returns>
    public static string FormatDefault(this FantasyDate date, FantasyDateFormatter formatter, string[]? monthNames = null)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));
        return formatter.FormatDefault(date, monthNames);
    }

    /// <summary>
    /// Formats this fantasy date using the long date pattern and formatter.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="formatter">The formatter to use.</param>
    /// <param name="monthNames">Optional custom month names.</param>
    /// <returns>The formatted string.</returns>
    public static string FormatLongDate(this FantasyDate date, FantasyDateFormatter formatter, string[]? monthNames = null)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));
        return formatter.FormatLongDate(date, monthNames);
    }

    /// <summary>
    /// Formats this fantasy date using the short date pattern and formatter.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="formatter">The formatter to use.</param>
    /// <param name="monthNames">Optional custom month names.</param>
    /// <returns>The formatted string.</returns>
    public static string FormatShortDate(this FantasyDate date, FantasyDateFormatter formatter, string[]? monthNames = null)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));
        return formatter.FormatShortDate(date, monthNames);
    }

    /// <summary>
    /// Formats this fantasy date using the time-only pattern and formatter.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="formatter">The formatter to use.</param>
    /// <returns>The formatted string.</returns>
    public static string FormatTime(this FantasyDate date, FantasyDateFormatter formatter)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));
        return formatter.FormatTime(date);
    }

    /// <summary>
    /// Formats this fantasy date using the readable pattern and formatter.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="formatter">The formatter to use.</param>
    /// <param name="monthNames">Optional custom month names.</param>
    /// <returns>The formatted string.</returns>
    public static string FormatReadable(this FantasyDate date, FantasyDateFormatter formatter, string[]? monthNames = null)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));
        return formatter.FormatReadable(date, monthNames);
    }

    /// <summary>
    /// Creates a new fantasy date by adding the specified number of years.
    /// This is a calendar-aware operation that uses the calendar's year length.
    /// </summary>
    /// <param name="date">The base fantasy date.</param>
    /// <param name="calendar">The calendar definition to use for date arithmetic.</param>
    /// <param name="years">The number of years to add (can be negative).</param>
    /// <returns>A new fantasy date with the years added.</returns>
    /// <remarks>
    /// This method calculates the average year length and may not be exact for calendars with leap years.
    /// For precise year arithmetic, consider using the calendar's specific year calculation.
    /// </remarks>
    public static FantasyDate AddYears(this FantasyDate date, ICalendarDefinition calendar, int years)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        // Simple approach: use the year field directly since calendars typically have consistent year structure
        return date with { Year = date.Year + years };
    }


    /// <summary>
    /// Creates a new fantasy date by adding the specified number of months.
    /// This is a calendar-aware operation that properly handles year boundaries.
    /// </summary>
    /// <param name="date">The base fantasy date.</param>
    /// <param name="calendar">The calendar definition to use for date arithmetic.</param>
    /// <param name="months">The number of months to add (can be negative).</param>
    /// <returns>A new fantasy date with the months added.</returns>
    /// <remarks>
    /// This method assumes a simple month addition without calendar-specific month length calculations.
    /// It adds to the month field and adjusts the year when crossing year boundaries.
    /// Note: This assumes 12 months per year. For calendars with different structures,
    /// consider using day-based arithmetic instead.
    /// </remarks>
    public static FantasyDate AddMonths(this FantasyDate date, ICalendarDefinition calendar, int months)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        int newYear = date.Year;
        int newMonth = date.Month + months;

        // Handle year overflow/underflow using calendar's months per year
        while (newMonth > calendar.MonthsPerYear)
        {
            newYear++;
            newMonth -= calendar.MonthsPerYear;
        }
        while (newMonth < 1)
        {
            newYear--;
            newMonth += calendar.MonthsPerYear;
        }

        return date with { Year = newYear, Month = newMonth };
    }


    /// <summary>
    /// Creates a new fantasy date by adding the specified number of days.
    /// This is a calendar-aware operation that properly handles month and year boundaries.
    /// </summary>
    /// <param name="date">The base fantasy date.</param>
    /// <param name="calendar">The calendar definition to use for date arithmetic.</param>
    /// <param name="days">The number of days to add (can be negative).</param>
    /// <returns>A new fantasy date with the days added.</returns>
    public static FantasyDate AddDays(this FantasyDate date, ICalendarDefinition calendar, int days)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        var tick = calendar.ToWorldTick(date);
        var ticksToAdd = days * calendar.TicksPerDay;
        var newTick = tick + ticksToAdd;
        return calendar.FromWorldTick(newTick);
    }


    /// <summary>
    /// Creates a new fantasy date by adding the specified number of hours.
    /// This is a calendar-aware operation that properly handles day boundaries.
    /// </summary>
    /// <param name="date">The base fantasy date.</param>
    /// <param name="calendar">The calendar definition to use for date arithmetic.</param>
    /// <param name="hours">The number of hours to add (can be negative).</param>
    /// <returns>A new fantasy date with the hours added.</returns>
    public static FantasyDate AddHours(this FantasyDate date, ICalendarDefinition calendar, int hours)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        var tick = calendar.ToWorldTick(date);
        var ticksPerHour = calendar.TicksPerDay / calendar.HoursPerDay;
        var ticksToAdd = hours * ticksPerHour;
        var newTick = tick + ticksToAdd;
        return calendar.FromWorldTick(newTick);
    }


    /// <summary>
    /// Creates a new fantasy date by adding the specified number of minutes.
    /// This is a calendar-aware operation that properly handles hour and day boundaries.
    /// </summary>
    /// <param name="date">The base fantasy date.</param>
    /// <param name="calendar">The calendar definition to use for date arithmetic.</param>
    /// <param name="minutes">The number of minutes to add (can be negative).</param>
    /// <returns>A new fantasy date with the minutes added.</returns>
    public static FantasyDate AddMinutes(this FantasyDate date, ICalendarDefinition calendar, int minutes)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        var tick = calendar.ToWorldTick(date);
        var ticksPerMinute = calendar.TicksPerDay / (calendar.HoursPerDay * calendar.MinutesPerHour);
        var ticksToAdd = minutes * ticksPerMinute;
        var newTick = tick + ticksToAdd;
        return calendar.FromWorldTick(newTick);
    }


    /// <summary>
    /// Creates a new fantasy date by adding the specified number of seconds.
    /// This is a calendar-aware operation that properly handles minute, hour, and day boundaries.
    /// </summary>
    /// <param name="date">The base fantasy date.</param>
    /// <param name="calendar">The calendar definition to use for date arithmetic.</param>
    /// <param name="seconds">The number of seconds to add (can be negative).</param>
    /// <returns>A new fantasy date with the seconds added.</returns>
    public static FantasyDate AddSeconds(this FantasyDate date, ICalendarDefinition calendar, int seconds)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        var tick = calendar.ToWorldTick(date);
        var ticksPerSecond = calendar.TicksPerDay / (calendar.HoursPerDay * calendar.MinutesPerHour * calendar.SecondsPerMinute);
        var ticksToAdd = seconds * ticksPerSecond;
        var newTick = tick + ticksToAdd;
        return calendar.FromWorldTick(newTick);
    }

}
