---
canonical: true
created: '2025-11-19'
doc_id: RFC-00015
doc_type: rfc
related:
- RFC-00013
status: draft
summary: Design a system to transform fantasy calendar dates to real-world calendar
  dates using NodaTime, enabling event logging, scheduling, and configurable time
  progression for game worlds
supersedes: []
tags:
- time
- calendar
- nodatime
- transformation
- scheduling
- logging
title: Fantasy Calendar to Real-World Time Transformation
updated: '2025-11-19'
---




# RFC-015: Fantasy Calendar to Real-World Time Transformation

- **Status:** Draft
- **Author:** Claude Agent
- **Date:** 2025-11-19
- **Supersedes:** N/A
- **Related:** RFC-013 (Plugin Architecture Refinement)

## Summary

Design and implement a system to transform fantasy calendar dates (Harptos, custom calendars) to real-world calendar dates using NodaTime, enabling:
- Event logging with real-world timestamps
- Scheduled in-game events mapped to real-world times
- Configurable time progression (real-time, accelerated, or slowed)
- Multi-calendar support for different in-game cultures

## Motivation

### Current State

The fantasy calendar system (`PigeonPea.Time`) provides:
- ✅ `WorldTick`: Absolute game time representation
- ✅ `ICalendarDefinition`: Fantasy calendar interface
- ✅ `WorldClock`: Basic NodaTime integration
- ✅ Multiple calendar implementations (Harptos, Configurable)

### Problems

1. **No Calendar Bridge**: No way to convert fantasy dates to real-world dates
2. **Limited WorldClock Usage**: `WorldClock` exists but lacks integration with calendars
3. **No Calendar Registry**: Cannot manage multiple calendars simultaneously
4. **No Formatting**: No human-readable display of fantasy dates
5. **No Time Zone Support**: Cannot handle different player time zones

### Goals

1. **Bidirectional Transformation**: Fantasy date ↔ Real-world date
2. **Time Scaling**: Control game time speed (1×, 10×, 60×, etc.)
3. **Multi-Calendar Support**: Convert between different fantasy calendars
4. **Time Zone Awareness**: Handle player locations correctly
5. **Event Scheduling**: Map in-game events to real-world times

## Architecture Overview

### Transformation Flow

```
┌─────────────────────────────────────────────────────────────┐
│ Fantasy Calendar Date                                        │
│ (e.g., Harptos Year 1372, Hammer 15, 14:30)                 │
└────────────────────────┬────────────────────────────────────┘
                         ↓ ICalendarDefinition.ToWorldTick()
┌─────────────────────────────────────────────────────────────┐
│ WorldTick (Absolute Game Time)                              │
│ (e.g., tick 500000000)                                      │
└────────────────────────┬────────────────────────────────────┘
                         ↓ WorldClock.ToInstant()
┌─────────────────────────────────────────────────────────────┐
│ NodaTime.Instant (UTC Timestamp)                            │
│ (e.g., 2025-01-15T10:23:45Z)                                │
└────────────────────────┬────────────────────────────────────┘
                         ↓ Instant.InZone()
┌─────────────────────────────────────────────────────────────┐
│ NodaTime.ZonedDateTime (Local Time)                         │
│ (e.g., 2025-01-15 02:23:45 PST)                             │
└─────────────────────────────────────────────────────────────┘
```

### Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ PigeonPea.Time.Contracts (Tier 1)                           │
├─────────────────────────────────────────────────────────────┤
│ - ICalendarDefinition                                       │
│ - ICalendarService (NEW)                                    │
│ - WorldTick                                                  │
│ - FantasyDate                                                │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ PigeonPea.Time.Core (Core Logic)                            │
├─────────────────────────────────────────────────────────────┤
│ - WorldClock (existing)                                     │
│ - CalendarBridge (NEW)                                      │
│ - CalendarService (NEW)                                     │
│ - FantasyDateFormatter (NEW)                                │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Plugins (Tier 3)                                            │
├─────────────────────────────────────────────────────────────┤
│ - PigeonPea.Plugin.Time.Harptos                             │
│ - PigeonPea.Plugin.Time.Configurable                        │
└─────────────────────────────────────────────────────────────┘
```

## Detailed Design

### 1. CalendarBridge

Connects fantasy calendars to real-world time.

```csharp
namespace PigeonPea.Time.Core;

public class CalendarBridge
{
    private readonly ICalendarDefinition _calendar;
    private readonly WorldClock _worldClock;

    public CalendarBridge(ICalendarDefinition calendar, WorldClock worldClock)
    {
        _calendar = calendar;
        _worldClock = worldClock;
    }

    /// <summary>
    /// Converts a fantasy date to a real-world zoned date/time.
    /// </summary>
    public ZonedDateTime ToRealWorld(FantasyDate fantasyDate, DateTimeZone zone)
    {
        var tick = _calendar.ToWorldTick(fantasyDate);
        var instant = _worldClock.ToInstant(tick);
        return instant.InZone(zone);
    }

    /// <summary>
    /// Converts a real-world date/time to a fantasy date.
    /// </summary>
    public FantasyDate ToFantasy(ZonedDateTime realDate)
    {
        var instant = realDate.ToInstant();
        var tick = _worldClock.ToWorldTick(instant);
        return _calendar.FromWorldTick(tick);
    }

    /// <summary>
    /// Gets the current fantasy date based on system time.
    /// </summary>
    public FantasyDate Now()
    {
        var instant = SystemClock.Instance.GetCurrentInstant();
        var tick = _worldClock.ToWorldTick(instant);
        return _calendar.FromWorldTick(tick);
    }
}
```

### 2. ICalendarService

Manages multiple calendars and conversions.

```csharp
namespace PigeonPea.Time.Contracts;

public interface ICalendarService
{
    /// <summary>
    /// Registers a calendar with a unique identifier.
    /// </summary>
    void RegisterCalendar(string id, ICalendarDefinition calendar);

    /// <summary>
    /// Gets a calendar by its identifier.
    /// </summary>
    ICalendarDefinition GetCalendar(string id);

    /// <summary>
    /// Converts a date from one fantasy calendar to another.
    /// </summary>
    FantasyDate Convert(FantasyDate date, string fromCalendarId, string toCalendarId);

    /// <summary>
    /// Converts a fantasy date to real-world time.
    /// </summary>
    ZonedDateTime ToRealWorld(FantasyDate date, string calendarId, DateTimeZone zone);

    /// <summary>
    /// Converts real-world time to a fantasy date.
    /// </summary>
    FantasyDate FromRealWorld(ZonedDateTime realDate, string calendarId);

    /// <summary>
    /// Gets the current fantasy date for a calendar.
    /// </summary>
    FantasyDate Now(string calendarId);
}
```

### 3. CalendarService Implementation

```csharp
namespace PigeonPea.Time.Core;

public class CalendarService : ICalendarService
{
    private readonly ConcurrentDictionary<string, ICalendarDefinition> _calendars = new();
    private readonly WorldClock _worldClock;

    public CalendarService(WorldClock worldClock)
    {
        _worldClock = worldClock;
    }

    public void RegisterCalendar(string id, ICalendarDefinition calendar)
    {
        if (!_calendars.TryAdd(id, calendar))
            throw new InvalidOperationException($"Calendar '{id}' already registered");
    }

    public ICalendarDefinition GetCalendar(string id)
    {
        if (!_calendars.TryGetValue(id, out var calendar))
            throw new KeyNotFoundException($"Calendar '{id}' not found");
        return calendar;
    }

    public FantasyDate Convert(FantasyDate date, string fromCalendarId, string toCalendarId)
    {
        var fromCalendar = GetCalendar(fromCalendarId);
        var toCalendar = GetCalendar(toCalendarId);

        // Convert via WorldTick
        var tick = fromCalendar.ToWorldTick(date);
        return toCalendar.FromWorldTick(tick);
    }

    public ZonedDateTime ToRealWorld(FantasyDate date, string calendarId, DateTimeZone zone)
    {
        var calendar = GetCalendar(calendarId);
        var bridge = new CalendarBridge(calendar, _worldClock);
        return bridge.ToRealWorld(date, zone);
    }

    public FantasyDate FromRealWorld(ZonedDateTime realDate, string calendarId)
    {
        var calendar = GetCalendar(calendarId);
        var bridge = new CalendarBridge(calendar, _worldClock);
        return bridge.ToFantasy(realDate);
    }

    public FantasyDate Now(string calendarId)
    {
        var calendar = GetCalendar(calendarId);
        var bridge = new CalendarBridge(calendar, _worldClock);
        return bridge.Now();
    }
}
```

### 4. FantasyDateFormatter

Human-readable formatting for fantasy dates.

```csharp
namespace PigeonPea.Time.Core;

public class FantasyDateFormatter
{
    private readonly ICalendarDefinition _calendar;
    private readonly Dictionary<int, string> _monthNames;

    public FantasyDateFormatter(
        ICalendarDefinition calendar,
        Dictionary<int, string>? monthNames = null)
    {
        _calendar = calendar;
        _monthNames = monthNames ?? new Dictionary<int, string>();
    }

    public string Format(FantasyDate date, string format = "YYYY-MM-DD HH:mm:ss")
    {
        var result = format
            .Replace("YYYY", date.Year.ToString())
            .Replace("MM", date.Month.ToString("D2"))
            .Replace("DD", date.Day.ToString("D2"))
            .Replace("HH", date.Hour.ToString("D2"))
            .Replace("mm", date.Minute.ToString("D2"))
            .Replace("ss", date.Second.ToString("D2"));

        // Replace month name if available
        if (_monthNames.TryGetValue(date.Month, out var monthName))
        {
            result = result.Replace("MMMM", monthName);
        }

        return result;
    }
}
```

## Use Cases

### Use Case 1: Event Logging

**Scenario:** Player defeats a boss at Harptos Year 1372, Hammer 15, 14:30

```csharp
var service = GetCalendarService();
var fantasyDate = new FantasyDate(1372, 1, 15, 14, 30, 0);
var realDate = service.ToRealWorld(fantasyDate, "harptos", DateTimeZone.Utc);

logger.LogInformation(
    "Boss defeated at {FantasyDate} (real: {RealDate})",
    fantasyDate,
    realDate);
// Output: "Boss defeated at 1372-01-15 14:30:00 (real: 2025-01-15 10:23:45 UTC)"
```

### Use Case 2: Scheduled Events

**Scenario:** Festival starts at Harptos Midsummer (month 15)

```csharp
var festivalDate = new FantasyDate(1372, 15, 1, 0, 0, 0);
var userTimeZone = DateTimeZoneProviders.Tzdb["America/Los_Angeles"];
var realStartTime = service.ToRealWorld(festivalDate, "harptos", userTimeZone);

notificationService.Schedule(
    realStartTime,
    "Festival of Midsummer begins!");
// Notification at: 2025-06-21 08:00 PST
```

### Use Case 3: Time-Based Progression

**Scenario:** Game runs at 60× speed (1 real minute = 1 game hour)

```csharp
// Setup
var epoch = Instant.FromUtc(2025, 1, 1, 0, 0);
var clock = new WorldClock(epoch, realSecondsPerGameSecond: 1.0 / 60.0);
var service = new CalendarService(clock);
service.RegisterCalendar("harptos", new HarptosCalendar());

// After 10 real minutes
var currentDate = service.Now("harptos");
// Result: 10 game hours have passed
```

### Use Case 4: Multi-Calendar Conversion

**Scenario:** Convert Harptos date to Elven calendar

```csharp
service.RegisterCalendar("harptos", new HarptosCalendar());
service.RegisterCalendar("elven", new ElvenCalendar());

var harptosDate = new FantasyDate(1372, 1, 15, 14, 30, 0);
var elvenDate = service.Convert(harptosDate, "harptos", "elven");
// Elven calendar shows equivalent date
```

## Implementation Strategy

### Phase 1: Core Components (Week 1)

1. **Create `CalendarBridge`** in `PigeonPea.Time.Core`
   - Implement `ToRealWorld()`, `ToFantasy()`, `Now()`
   - Write unit tests

2. **Create `ICalendarService`** in `PigeonPea.Time.Contracts`
   - Define interface

3. **Implement `CalendarService`** in `PigeonPea.Time.Core`
   - Thread-safe calendar registration
   - Conversion logic
   - Write unit tests

### Phase 2: Formatting & Utilities (Week 2)

1. **Create `FantasyDateFormatter`**
   - Support basic format patterns
   - Add Harptos month names
   - Write unit tests

2. **Add Extension Methods**
   - `FantasyDate.ToRealWorld()`
   - `ZonedDateTime.ToFantasy()`

### Phase 3: Integration & Documentation (Week 3)

1. **Update Documentation**
   - API documentation
   - Usage examples
   - Migration guide

2. **Integration Examples**
   - Console app demo
   - Event logging example

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void CalendarBridge_RoundTrip_PreservesDate()
{
    var epoch = Instant.FromUtc(2025, 1, 1, 0, 0);
    var clock = new WorldClock(epoch);
    var harptos = new HarptosCalendar();
    var bridge = new CalendarBridge(harptos, clock);

    var original = new FantasyDate(1372, 1, 15, 14, 30, 0);
    var real = bridge.ToRealWorld(original, DateTimeZone.Utc);
    var converted = bridge.ToFantasy(real);

    Assert.Equal(original, converted);
}

[Fact]
public void CalendarService_Convert_BetweenCalendars()
{
    var service = CreateCalendarService();
    service.RegisterCalendar("harptos", new HarptosCalendar());
    service.RegisterCalendar("custom", new CustomCalendar());

    var harptosDate = new FantasyDate(1372, 1, 15, 14, 30, 0);
    var customDate = service.Convert(harptosDate, "harptos", "custom");

    // Verify conversion is consistent
    var backToHarptos = service.Convert(customDate, "custom", "harptos");
    Assert.Equal(harptosDate, backToHarptos);
}
```

### Integration Tests

```csharp
[Fact]
public async Task EventScheduling_WorksWithRealTime()
{
    var service = CreateCalendarService();
    var festivalDate = new FantasyDate(1372, 15, 1, 0, 0, 0);
    var realDate = service.ToRealWorld(festivalDate, "harptos", DateTimeZone.Utc);

    // Schedule event
    var scheduler = new EventScheduler();
    await scheduler.ScheduleAt(realDate, () => TriggerFestival());

    // Verify event triggers at correct time
    Assert.True(scheduler.HasPendingEvent(realDate));
}
```

## Success Criteria

- [ ] `CalendarBridge` converts fantasy dates to real-world dates
- [ ] `CalendarService` manages multiple calendars
- [ ] Round-trip conversion preserves dates
- [ ] Multi-calendar conversion works correctly
- [ ] Time scaling (1×, 10×, 60×) functions properly
- [ ] Time zone support works for different locations
- [ ] Formatting produces human-readable output
- [ ] All unit tests pass
- [ ] Integration tests demonstrate real-world usage

## Future Enhancements

1. **Pause/Resume Game Time**
   - Ability to pause time progression
   - Save/load time state

2. **Historical Time Tracking**
   - Record time progression history
   - Support time travel mechanics

3. **In-Game Time Zones**
   - Different time zones within the game world
   - Offset calculations

4. **Calendar Events**
   - Recurring events (daily, weekly, monthly)
   - Holiday/festival definitions

## References

- [NodaTime Documentation](https://nodatime.org/)
- [RFC-013: Plugin Architecture Refinement](./013-plugin-architecture-refinement-tiered.md)
- [Calendar of Harptos (Forgotten Realms)](https://forgottenrealms.fandom.com/wiki/Calendar_of_Harptos)
