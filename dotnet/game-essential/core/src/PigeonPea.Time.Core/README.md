# PigeonPea.Time.Core

Core implementation and utilities for the fantasy calendar time system.

## Overview

This package provides the core implementation of the fantasy calendar system, including:

- **CalendarService** - Multi-calendar management
- **CalendarBridge** - Fantasy ↔ Real-world time conversion
- **FantasyDateFormatter** - Human-readable date formatting
- **Extension Methods** - Fluent API for date operations
- **WorldClock** - Time scaling and progression

## Installation

```bash
dotnet add package PigeonPea.Time.Core
dotnet add package PigeonPea.Time.Contracts
```

## Core Components

### CalendarService

Thread-safe service for managing multiple calendars.

```csharp
var worldClock = new WorldClock(
    epoch: Instant.FromUtc(2025, 1, 1, 0, 0),
    realSecondsPerGameSecond: 1.0  // Real-time
);

var service = new CalendarService(worldClock);
service.RegisterCalendar("harptos", new HarptosCalendar());
service.RegisterCalendar("gregorian", new GregorianCalendar());
```

**Features:**
- Case-insensitive calendar IDs
- Thread-safe registration and access
- Calendar discovery (`GetRegisteredCalendarIds()`, `IsCalendarRegistered()`)

### CalendarBridge

Handles the transformation chain between fantasy dates and real-world time.

```
FantasyDate → WorldTick → Instant → ZonedDateTime
```

```csharp
var bridge = new CalendarBridge(worldClock, harptosCalendar);

// Fantasy → Real
var realDate = bridge.ToRealWorld(fantasyDate, DateTimeZone.Utc);

// Real → Fantasy
var fantasyDate = bridge.FromRealWorld(realDate);
```

### FantasyDateFormatter

Flexible pattern-based formatting for fantasy dates.

```csharp
var formatter = new FantasyDateFormatter(harptosCalendar);

// Custom pattern
var formatted = formatter.Format(date, "MMMM dd, yyyy HH:mm", harptosMonthNames);
// Output: "Hammer 15, 1372 14:30"

// Convenience methods
formatter.FormatDefault(date);      // "1372-01-15 14:30:00"
formatter.FormatLongDate(date);     // "Hammer 15, 1372"
formatter.FormatShortDate(date);    // "01/15/1372"
formatter.FormatTime(date);         // "14:30:00"
formatter.FormatReadable(date);     // "Hammer 15, 1372 14:30"
```

**Supported Patterns:**
- `yyyy`, `yy`, `y` - Year
- `MMMM`, `MMM`, `MM`, `M` - Month
- `dd`, `d` - Day
- `HH`, `H` - Hour
- `mm`, `m` - Minute
- `ss`, `s` - Second

### WorldClock

Controls game time progression and scaling.

```csharp
// Real-time (1:1)
var realTimeClock = new WorldClock(
    epoch: Instant.FromUtc(2025, 1, 1, 0, 0),
    realSecondsPerGameSecond: 1.0
);

// 60× speed (1 real minute = 1 game hour)
var acceleratedClock = new WorldClock(
    epoch: Instant.FromUtc(2025, 1, 1, 0, 0),
    realSecondsPerGameSecond: 1.0 / 60.0
);

// Convert between WorldTick and Instant
var instant = clock.ToInstant(worldTick);
var tick = clock.ToWorldTick(instant);
```

## Extension Methods

### FantasyDateExtensions

Calendar-aware date arithmetic:

```csharp
var date = new FantasyDate(1372, 1, 28, 14, 30, 0);

// Add time (calendar-aware, handles overflow)
var tomorrow = date.AddDays(calendar, 1);
var nextWeek = date.AddDays(calendar, 7);
var nextMonth = date.AddMonths(calendar, 1);
var nextYear = date.AddYears(calendar, 1);

// Time arithmetic
var later = date.AddHours(calendar, 5);
var soon = date.AddMinutes(calendar, 30);

// Negative values (subtraction)
var yesterday = date.AddDays(calendar, -1);
```

**Conversion extensions:**

```csharp
// To real-world
var realDate = fantasyDate.ToRealWorld(calendarService, "harptos", DateTimeZone.Utc);
var realDateLocal = fantasyDate.ToRealWorldLocal(calendarService, "harptos");

// To another calendar
var gregorianDate = fantasyDate.ToCalendar(calendarService, "harptos", "gregorian");

// Formatting
var formatted = fantasyDate.FormatReadable(formatter, monthNames);
```

### NodaTimeExtensions

Convert real-world time to fantasy dates:

```csharp
// From ZonedDateTime
var fantasyDate = zonedDateTime.ToFantasyDate(calendarService, "harptos");

// From Instant
var fantasyDate = instant.ToFantasyDate(calendarService, "harptos");

// Get current fantasy date
var now = calendarService.GetNow("harptos");
var nowUtc = calendarService.GetNowUtc("harptos");
var nowLocal = calendarService.GetNowLocal("harptos");
```

## Complete Examples

### Example 1: Event Logging System

```csharp
public class GameEventLogger
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger _logger;

    public void LogEvent(string eventName, string details)
    {
        var fantasyDate = _calendarService.Now("harptos");
        var realDate = _calendarService.ToRealWorld(
            fantasyDate, 
            "harptos", 
            DateTimeZone.Utc
        );

        _logger.LogInformation(
            "[{FantasyDate}] {EventName}: {Details} (Real: {RealDate})",
            fantasyDate,
            eventName,
            details,
            realDate
        );
    }
}

// Usage:
logger.LogEvent("Boss Defeated", "Ancient Red Dragon");
// Output: [1372-01-15 14:30:00] Boss Defeated: Ancient Red Dragon (Real: 2025-01-15T10:23:45Z)
```

### Example 2: Quest Timer

```csharp
public class QuestTimer
{
    private readonly ICalendarService _calendarService;
    private readonly ICalendarDefinition _calendar;

    public FantasyDate CalculateDeadline(FantasyDate startDate, int daysAllowed)
    {
        return startDate.AddDays(_calendar, daysAllowed);
    }

    public bool IsOverdue(FantasyDate deadline)
    {
        var now = _calendarService.Now("harptos");
        var nowTick = _calendar.ToWorldTick(now);
        var deadlineTick = _calendar.ToWorldTick(deadline);
        
        return nowTick > deadlineTick;
    }
}

// Usage:
var questStart = new FantasyDate(1372, 1, 15, 0, 0, 0);
var deadline = timer.CalculateDeadline(questStart, 7); // 7 days
// Result: 1372-01-22 00:00:00 (properly handles month boundaries)
```

### Example 3: Festival Scheduler

```csharp
public class FestivalScheduler
{
    private readonly ICalendarService _calendarService;
    private readonly INotificationService _notifications;

    public void ScheduleMidsummerFestival(int year)
    {
        // Midsummer is month 15 in Harptos
        var festivalDate = new FantasyDate(year, 15, 1, 0, 0, 0);
        
        // Convert to player's local time
        var playerTimeZone = DateTimeZoneProviders.Tzdb["America/Los_Angeles"];
        var realStartTime = _calendarService.ToRealWorld(
            festivalDate,
            "harptos",
            playerTimeZone
        );

        _notifications.Schedule(
            realStartTime,
            "The Festival of Midsummer begins!",
            "Join the celebration in Waterdeep!"
        );
    }
}
```

### Example 4: Time-Accelerated Gameplay

```csharp
public class GameTimeManager
{
    private readonly CalendarService _calendarService;
    private WorldClock _worldClock;

    public void SetTimeScale(double multiplier)
    {
        // 1.0 = real-time
        // 60.0 = 60× speed (1 real minute = 1 game hour)
        // 0.5 = half speed
        
        var epoch = Instant.FromUtc(2025, 1, 1, 0, 0);
        _worldClock = new WorldClock(
            epoch,
            realSecondsPerGameSecond: 1.0 / multiplier
        );
        
        // Re-register calendars with new clock
        _calendarService = new CalendarService(_worldClock);
        _calendarService.RegisterCalendar("harptos", new HarptosCalendar());
    }

    public string GetCurrentGameTime()
    {
        var date = _calendarService.Now("harptos");
        var formatter = new FantasyDateFormatter(new HarptosCalendar());
        return formatter.FormatReadable(date, HarptosMonthNames);
    }
}

// Usage:
manager.SetTimeScale(60.0); // 60× speed
// After 10 real minutes: 10 game hours have passed
```

### Example 5: Multi-Calendar Conversion

```csharp
public class CalendarConverter
{
    private readonly ICalendarService _calendarService;

    public CalendarConverter()
    {
        var clock = new WorldClock(Instant.FromUtc(2025, 1, 1, 0, 0));
        _calendarService = new CalendarService(clock);
        
        _calendarService.RegisterCalendar("harptos", new HarptosCalendar());
        _calendarService.RegisterCalendar("elven", new ElvenCalendar());
        _calendarService.RegisterCalendar("dwarvish", new DwarvishCalendar());
    }

    public void ShowDateInAllCalendars(FantasyDate harptosDate)
    {
        Console.WriteLine($"Harptos: {harptosDate}");
        
        var elvenDate = _calendarService.Convert(harptosDate, "harptos", "elven");
        Console.WriteLine($"Elven:   {elvenDate}");
        
        var dwarvishDate = _calendarService.Convert(harptosDate, "harptos", "dwarvish");
        Console.WriteLine($"Dwarvish: {dwarvishDate}");
    }
}
```

## Best Practices

### 1. Use Dependency Injection

```csharp
services.AddSingleton<WorldClock>(sp => 
    new WorldClock(Instant.FromUtc(2025, 1, 1, 0, 0)));
services.AddSingleton<ICalendarService, CalendarService>();
```

### 2. Register Calendars at Startup

```csharp
public void ConfigureCalendars(ICalendarService service)
{
    service.RegisterCalendar("harptos", new HarptosCalendar());
    service.RegisterCalendar("gregorian", new GregorianCalendar());
    // Add more calendars as needed
}
```

### 3. Use Extension Methods for Readability

```csharp
// ✅ Good: Fluent and readable
var deadline = questStart
    .AddDays(calendar, 7)
    .AddHours(calendar, 12);

// ❌ Avoid: Verbose
var tick1 = calendar.ToWorldTick(questStart);
var tick2 = tick1 + (7 * calendar.TicksPerDay);
var tick3 = tick2 + (12 * calendar.TicksPerDay / 24);
var deadline = calendar.FromWorldTick(tick3);
```

### 4. Cache Formatters

```csharp
// Create once, reuse many times
private readonly FantasyDateFormatter _formatter = 
    new FantasyDateFormatter(new HarptosCalendar());
```

## Performance Considerations

- **CalendarService** is thread-safe and can be used as a singleton
- **CalendarBridge** instances are cached per calendar in CalendarService
- **Formatter** instances are lightweight and can be reused
- **WorldTick** arithmetic is fast (simple long addition)

## Related Packages

- **PigeonPea.Time.Contracts** - Core interfaces and types
- **PigeonPea.Plugin.Time.Harptos** - Harptos calendar implementation
- **PigeonPea.Plugin.Time.Configurable** - Custom calendar support

## See Also

- [PigeonPea.Time.Contracts README](../PigeonPea.Time.Contracts/README.md)
- [RFC-015: Fantasy Calendar to Real-World Time Transformation](../../../../docs/rfcs/015-fantasy-calendar-real-world-transformation.md)
- [Calendar-Aware Arithmetic Walkthrough](../../../../.gemini/antigravity/brain/6838a53f-07a4-4948-9a49-f7356e8f4205/calendar_aware_arithmetic_walkthrough.md)
