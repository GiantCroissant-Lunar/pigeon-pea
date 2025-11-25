using NodaTime;
using PigeonPea.Platform.Contracts.Time;

namespace PigeonPea.Platform.TimeManagement;

/// <summary>
/// Manages multiple calendars and provides conversion between fantasy and real-world time.
/// </summary>
public class CalendarService : ICalendarService
{
    private readonly WorldClock _worldClock;
    private readonly Dictionary<string, ICalendarDefinition> _calendars;
    private readonly Dictionary<string, CalendarBridge> _bridges;
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarService"/> class.
    /// </summary>
    /// <param name="worldClock">The world clock for time conversions.</param>
    public CalendarService(WorldClock worldClock)
    {
        _worldClock = worldClock ?? throw new ArgumentNullException(nameof(worldClock));
        _calendars = new Dictionary<string, ICalendarDefinition>(StringComparer.OrdinalIgnoreCase);
        _bridges = new Dictionary<string, CalendarBridge>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void RegisterCalendar(string id, ICalendarDefinition calendar)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(id));

        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar));

        lock (_lock)
        {
            if (_calendars.ContainsKey(id))
                throw new InvalidOperationException($"A calendar with ID '{id}' is already registered.");

            _calendars[id] = calendar;
            _bridges[id] = new CalendarBridge(_worldClock, calendar);
        }
    }

    /// <inheritdoc />
    public ICalendarDefinition GetCalendar(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(id));

        lock (_lock)
        {
            if (!_calendars.TryGetValue(id, out var calendar))
                throw new KeyNotFoundException($"No calendar with ID '{id}' is registered.");

            return calendar;
        }
    }

    /// <inheritdoc />
    public FantasyDate Convert(FantasyDate date, string fromCalendarId, string toCalendarId)
    {
        if (string.IsNullOrWhiteSpace(fromCalendarId))
            throw new ArgumentException("Source calendar ID cannot be null or whitespace.", nameof(fromCalendarId));

        if (string.IsNullOrWhiteSpace(toCalendarId))
            throw new ArgumentException("Target calendar ID cannot be null or whitespace.", nameof(toCalendarId));

        lock (_lock)
        {
            if (!_calendars.TryGetValue(fromCalendarId, out var fromCalendar))
                throw new KeyNotFoundException($"No calendar with ID '{fromCalendarId}' is registered.");

            if (!_calendars.TryGetValue(toCalendarId, out var toCalendar))
                throw new KeyNotFoundException($"No calendar with ID '{toCalendarId}' is registered.");

            // Convert through WorldTick as the intermediate representation
            WorldTick worldTick = fromCalendar.ToWorldTick(date);
            return toCalendar.FromWorldTick(worldTick);
        }
    }

    /// <inheritdoc />
    public ZonedDateTime ToRealWorld(FantasyDate date, string calendarId, DateTimeZone zone)
    {
        if (string.IsNullOrWhiteSpace(calendarId))
            throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        lock (_lock)
        {
            if (!_bridges.TryGetValue(calendarId, out var bridge))
                throw new KeyNotFoundException($"No calendar with ID '{calendarId}' is registered.");

            return bridge.ToRealWorld(date, zone);
        }
    }

    /// <inheritdoc />
    public FantasyDate FromRealWorld(ZonedDateTime realDate, string calendarId)
    {
        if (string.IsNullOrWhiteSpace(calendarId))
            throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        lock (_lock)
        {
            if (!_bridges.TryGetValue(calendarId, out var bridge))
                throw new KeyNotFoundException($"No calendar with ID '{calendarId}' is registered.");

            return bridge.FromRealWorld(realDate);
        }
    }

    /// <inheritdoc />
    public FantasyDate Now(string calendarId)
    {
        if (string.IsNullOrWhiteSpace(calendarId))
            throw new ArgumentException("Calendar ID cannot be null or whitespace.", nameof(calendarId));

        lock (_lock)
        {
            if (!_bridges.TryGetValue(calendarId, out var bridge))
                throw new KeyNotFoundException($"No calendar with ID '{calendarId}' is registered.");

            // Get current system time and convert to fantasy date
            Instant now = SystemClock.Instance.GetCurrentInstant();
            return bridge.FromInstant(now);
        }
    }

    /// <summary>
    /// Gets all registered calendar IDs.
    /// </summary>
    /// <returns>A read-only collection of registered calendar IDs.</returns>
    public IReadOnlyCollection<string> GetRegisteredCalendarIds()
    {
        lock (_lock)
        {
            return _calendars.Keys.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Checks if a calendar is registered.
    /// </summary>
    /// <param name="id">The calendar ID to check.</param>
    /// <returns>True if the calendar is registered, false otherwise.</returns>
    public bool IsCalendarRegistered(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        lock (_lock)
        {
            return _calendars.ContainsKey(id);
        }
    }
}
