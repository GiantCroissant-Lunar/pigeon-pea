using NodaTime;
using PigeonPea.Time.Contracts;

namespace PigeonPea.Time.Core;

/// <summary>
/// Handles the transformation chain between fantasy dates and real-world time.
/// Transformation: FantasyDate -> WorldTick -> WorldClock -> Instant -> ZonedDateTime
/// </summary>
public class CalendarBridge
{
    private readonly WorldClock _worldClock;
    private readonly ICalendarDefinition _calendarDefinition;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarBridge"/> class.
    /// </summary>
    /// <param name="worldClock">The world clock for tick-to-time conversion.</param>
    /// <param name="calendarDefinition">The calendar definition for date-to-tick conversion.</param>
    public CalendarBridge(WorldClock worldClock, ICalendarDefinition calendarDefinition)
    {
        _worldClock = worldClock ?? throw new ArgumentNullException(nameof(worldClock));
        _calendarDefinition = calendarDefinition ?? throw new ArgumentNullException(nameof(calendarDefinition));
    }

    /// <summary>
    /// Converts a fantasy date to a real-world zoned date/time.
    /// </summary>
    /// <param name="fantasyDate">The fantasy date to convert.</param>
    /// <param name="zone">The time zone for the resulting real-world date.</param>
    /// <returns>The equivalent real-world zoned date/time.</returns>
    public ZonedDateTime ToRealWorld(FantasyDate fantasyDate, DateTimeZone zone)
    {
        if (zone == null) throw new ArgumentNullException(nameof(zone));

        // FantasyDate -> WorldTick
        WorldTick worldTick = _calendarDefinition.ToWorldTick(fantasyDate);

        // WorldTick -> WorldClock -> Instant
        Instant instant = _worldClock.ToInstant(worldTick);

        // Instant -> ZonedDateTime
        return instant.InZone(zone);
    }

    /// <summary>
    /// Converts a real-world zoned date/time to a fantasy date.
    /// </summary>
    /// <param name="realDate">The real-world zoned date/time to convert.</param>
    /// <returns>The equivalent fantasy date.</returns>
    public FantasyDate FromRealWorld(ZonedDateTime realDate)
    {
        // ZonedDateTime -> Instant
        Instant instant = realDate.ToInstant();

        // Instant -> WorldClock -> WorldTick
        WorldTick worldTick = _worldClock.ToWorldTick(instant);

        // WorldTick -> FantasyDate
        return _calendarDefinition.FromWorldTick(worldTick);
    }

    /// <summary>
    /// Converts a fantasy date to an instant in UTC.
    /// </summary>
    /// <param name="fantasyDate">The fantasy date to convert.</param>
    /// <returns>The equivalent instant in UTC.</returns>
    public Instant ToInstant(FantasyDate fantasyDate)
    {
        // FantasyDate -> WorldTick
        WorldTick worldTick = _calendarDefinition.ToWorldTick(fantasyDate);

        // WorldTick -> WorldClock -> Instant
        return _worldClock.ToInstant(worldTick);
    }

    /// <summary>
    /// Converts an instant in UTC to a fantasy date.
    /// </summary>
    /// <param name="instant">The instant to convert.</param>
    /// <returns>The equivalent fantasy date.</returns>
    public FantasyDate FromInstant(Instant instant)
    {
        // Instant -> WorldClock -> WorldTick
        WorldTick worldTick = _worldClock.ToWorldTick(instant);

        // WorldTick -> FantasyDate
        return _calendarDefinition.FromWorldTick(worldTick);
    }
}
