namespace PigeonPea.Time.Contracts;

/// <summary>
/// Defines the rules for a fantasy calendar system.
/// </summary>
public interface ICalendarDefinition
{
    /// <summary>
    /// The name of the calendar (e.g., "Harptos", "Gregorian").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Converts an absolute world tick to a date in this calendar.
    /// </summary>
    FantasyDate FromWorldTick(WorldTick tick);

    /// <summary>
    /// Converts a date in this calendar to an absolute world tick.
    /// </summary>
    WorldTick ToWorldTick(FantasyDate date);

    /// <summary>
    /// Gets the number of ticks per day in this calendar.
    /// </summary>
    long TicksPerDay { get; }

    /// <summary>
    /// Gets the number of hours in a day for this calendar.
    /// Default is 24 for most calendars.
    /// </summary>
    int HoursPerDay => 24;

    /// <summary>
    /// Gets the number of minutes in an hour for this calendar.
    /// Default is 60 for most calendars.
    /// </summary>
    int MinutesPerHour => 60;

    /// <summary>
    /// Gets the number of seconds in a minute for this calendar.
    /// Default is 60 for most calendars.
    /// </summary>
    int SecondsPerMinute => 60;

    /// <summary>
    /// Gets the number of months in a year for this calendar.
    /// Default is 12 for most calendars.
    /// Note: Some calendars like Harptos have special days that aren't part of months.
    /// </summary>
    int MonthsPerYear => 12;
}
