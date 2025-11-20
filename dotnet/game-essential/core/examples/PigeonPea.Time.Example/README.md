# PigeonPea.Time.Example

Example console application demonstrating the Fantasy Calendar Time System from RFC-015.

## Overview

This application demonstrates all four use cases from RFC-015:
1. **Event Logging** - Log game events with fantasy and real-world timestamps
2. **Scheduled Events** - Schedule in-game events to real-world times
3. **Time Scaling** - Control game time speed (real-time, accelerated, slowed)
4. **Multi-Calendar Conversion** - Convert dates between different fantasy calendars

## Running the Examples

```bash
cd examples/PigeonPea.Time.Example
dotnet run
```

## Example Output

```
╔════════════════════════════════════════════════════════════╗
║  PigeonPea Fantasy Calendar Time System - Examples         ║
║  RFC-015: Fantasy Calendar to Real-World Transformation    ║
╚════════════════════════════════════════════════════════════╝

┌─ Example 1: Event Logging ─────────────────────────────────┐

=== Event Logging Example ===

[1372-01-15 14:30:00] Boss Defeated
  Details: Ancient Red Dragon slain by party
  Real-world time: 2025-01-15T10:23:45Z

...
```

## Examples Included

### 1. Event Logging (`EventLoggingExample.cs`)

Demonstrates logging player actions with both fantasy dates and real-world timestamps:
- Boss defeats
- Quest completions
- Item discoveries

**Key Features:**
- Dual timestamp logging (fantasy + real-world)
- Useful for game analytics and debugging

### 2. Scheduled Events (`ScheduledEventsExample.cs`)

Shows how to schedule in-game events to real-world times:
- Festival notifications
- Quest deadlines
- Time zone support for different players

**Key Features:**
- Multi-timezone support
- Real-world notification scheduling
- Player-specific timing

### 3. Time Scaling (`TimeScalingExample.cs`)

Demonstrates different time progression speeds:
- Real-time (1:1)
- 10× speed
- 60× speed (1 real minute = 1 game hour)
- 1440× speed (1 real minute = 1 game day)

**Key Features:**
- Configurable time multipliers
- Elapsed time calculations
- Useful for testing and different gameplay modes

### 4. Multi-Calendar Conversion (`MultiCalendarExample.cs`)

Shows conversion between different fantasy calendars:
- Harptos (Forgotten Realms)
- Elven calendar (custom example)
- Round-trip conversion validation

**Key Features:**
- Calendar-to-calendar conversion
- Same moment across different calendars
- Conversion accuracy verification

## Code Structure

```
PigeonPea.Time.Example/
├── Program.cs                          # Main entry point
├── Examples/
│   ├── EventLoggingExample.cs         # Use Case 1
│   ├── ScheduledEventsExample.cs      # Use Case 2
│   ├── TimeScalingExample.cs          # Use Case 3
│   └── MultiCalendarExample.cs        # Use Case 4
└── README.md
```

## Dependencies

- `PigeonPea.Time.Contracts` - Core interfaces
- `PigeonPea.Time.Core` - Implementation
- `PigeonPea.Plugin.Time.Harptos` - Harptos calendar
- `PigeonPea.Plugin.Time.Configurable` - Custom calendars
- `NodaTime` - Real-world time handling

## Learning Resources

- [RFC-015: Fantasy Calendar to Real-World Time Transformation](../../../../docs/rfcs/015-fantasy-calendar-real-world-transformation.md)
- [PigeonPea.Time.Contracts README](../../src/PigeonPea.Time.Contracts/README.md)
- [PigeonPea.Time.Core README](../../src/PigeonPea.Time.Core/README.md)

## Extending the Examples

To add your own examples:

1. Create a new class in `Examples/`
2. Implement your example logic
3. Add it to `Program.cs` using `RunExample()`

Example:

```csharp
RunExample(5, "My Custom Example", () =>
{
    var example = new MyCustomExample(calendarService);
    example.Run();
});
```
