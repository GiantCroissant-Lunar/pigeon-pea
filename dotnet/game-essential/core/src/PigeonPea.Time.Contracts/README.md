# PigeonPea.Time.Contracts

Core contracts and interfaces for the fantasy calendar time system.

## Overview

This package provides the foundational types and interfaces for working with fantasy calendar systems in game worlds. It enables:

- **Fantasy Calendar Definitions** - Define custom calendar systems (Harptos, Gregorian, etc.)
- **Real-World Time Mapping** - Convert between fantasy dates and real-world timestamps
- **Multi-Calendar Support** - Manage multiple calendars simultaneously
- **Time Scaling** - Control game time speed (real-time, accelerated, or slowed)

## Core Types

### FantasyDate

Represents a date and time in a fantasy calendar.

```csharp
public readonly record struct FantasyDate(
    int Year,
    int Month,
    int Day,
    int Hour,
    int Minute,
    int Second
);
```

**Example:**
```csharp
// Harptos Year 1372, Hammer 15, 2:30 PM
var date = new FantasyDate(1372, 1, 15, 14, 30, 0);
```

### WorldTick

Represents an absolute point in time in the game world.

```csharp
public readonly struct WorldTick
{
    public long Value { get; }
}
```

**Purpose:** Provides a calendar-independent time representation for conversions.

### ICalendarDefinition

Defines the rules for a fantasy calendar system.

```csharp
public interface ICalendarDefinition
{
    string Name { get; }
    FantasyDate FromWorldTick(WorldTick tick);
    WorldTick ToWorldTick(FantasyDate date);
    long TicksPerDay { get; }
}
```

**Implementations:**
- `HarptosCalendar` - Forgotten Realms calendar (365 days, 12 months + 5 special days)
- `ConfigurableCalendar` - Custom calendar via configuration

### ICalendarService

Manages multiple calendars and provides conversion services.

```csharp
public interface ICalendarService
{
    void RegisterCalendar(string id, ICalendarDefinition calendar);
    ICalendarDefinition GetCalendar(string id);
    FantasyDate Convert(FantasyDate date, string fromCalendarId, string toCalendarId);
    ZonedDateTime ToRealWorld(FantasyDate date, string calendarId, DateTimeZone zone);
    FantasyDate FromRealWorld(ZonedDateTime realDate, string calendarId);
    FantasyDate Now(string calendarId);
}
```

## Quick Start

### 1. Register a Calendar

```csharp
var worldClock = new WorldClock(
    epoch: Instant.FromUtc(2025, 1, 1, 0, 0),
    realSecondsPerGameSecond: 1.0 / 60.0  // 60× speed
);

var calendarService = new CalendarService(worldClock);
calendarService.RegisterCalendar("harptos", new HarptosCalendar());
```

### 2. Convert Fantasy Date to Real-World Time

```csharp
var fantasyDate = new FantasyDate(1372, 1, 15, 14, 30, 0);
var realDate = calendarService.ToRealWorld(
    fantasyDate, 
    "harptos", 
    DateTimeZone.Utc
);

Console.WriteLine($"Festival starts at {realDate}");
// Output: Festival starts at 2025-01-15T10:23:45Z
```

### 3. Get Current Fantasy Date

```csharp
var currentDate = calendarService.Now("harptos");
Console.WriteLine($"Current date: {currentDate}");
// Output: Current date: 1372-01-15 14:30:00
```

### 4. Convert Between Calendars

```csharp
calendarService.RegisterCalendar("gregorian", new GregorianCalendar());

var harptosDate = new FantasyDate(1372, 1, 15, 14, 30, 0);
var gregorianDate = calendarService.Convert(
    harptosDate, 
    "harptos", 
    "gregorian"
);
```

## Use Cases

### Event Logging

Log player actions with fantasy dates:

```csharp
var eventTime = calendarService.Now("harptos");
logger.LogInformation(
    "Boss defeated at {FantasyDate} (real: {RealDate})",
    eventTime,
    calendarService.ToRealWorld(eventTime, "harptos", DateTimeZone.Utc)
);
```

### Scheduled Events

Schedule in-game events to real-world times:

```csharp
var festivalDate = new FantasyDate(1372, 15, 1, 0, 0, 0); // Midsummer
var realStartTime = calendarService.ToRealWorld(
    festivalDate, 
    "harptos", 
    DateTimeZoneProviders.Tzdb["America/Los_Angeles"]
);

notificationService.Schedule(realStartTime, "Festival of Midsummer begins!");
```

### Time-Based Progression

Run game at accelerated speed:

```csharp
// 60× speed: 1 real minute = 1 game hour
var clock = new WorldClock(
    Instant.FromUtc(2025, 1, 1, 0, 0),
    realSecondsPerGameSecond: 1.0 / 60.0
);

// After 10 real minutes, 10 game hours have passed
```

## Architecture

```
┌─────────────────────────────────┐
│  ICalendarService (Contract)    │
└────────────┬────────────────────┘
             │
             ↓
┌─────────────────────────────────┐
│  CalendarService (Core)         │
│  ├─ CalendarBridge              │
│  ├─ FantasyDateFormatter        │
│  └─ WorldClock                  │
└────────────┬────────────────────┘
             │
             ↓
┌─────────────────────────────────┐
│  Plugins                        │
│  ├─ HarptosCalendar             │
│  └─ ConfigurableCalendar        │
└─────────────────────────────────┘
```

## Related Packages

- **PigeonPea.Time.Core** - Core implementation and utilities
- **PigeonPea.Plugin.Time.Harptos** - Harptos calendar implementation
- **PigeonPea.Plugin.Time.Configurable** - Configurable calendar implementation

## See Also

- [PigeonPea.Time.Core README](../PigeonPea.Time.Core/README.md)
- [RFC-015: Fantasy Calendar to Real-World Time Transformation](../../../../docs/rfcs/015-fantasy-calendar-real-world-transformation.md)
