using NodaTime;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;

namespace PigeonPea.Time.Example.Examples;

/// <summary>
/// Demonstrates logging game events with both fantasy and real-world timestamps.
/// Use Case 1 from RFC-015.
/// </summary>
public class EventLoggingExample
{
    private readonly ICalendarService _calendarService;
    private readonly ICalendarDefinition _calendar;

    public EventLoggingExample(ICalendarService calendarService, ICalendarDefinition calendar)
    {
        _calendarService = calendarService;
        _calendar = calendar;
    }

    public void Run()
    {
        Console.WriteLine("=== Event Logging Example ===\n");

        // Simulate player defeating a boss
        var fantasyDate = new FantasyDate(1372, 1, 15, 14, 30, 0);
        var realDate = _calendarService.ToRealWorld(fantasyDate, "harptos", DateTimeZone.Utc);

        LogEvent("Boss Defeated", "Ancient Red Dragon slain by party", fantasyDate, realDate);

        // Simulate quest completion
        var questDate = new FantasyDate(1372, 1, 16, 10, 15, 30);
        var questRealDate = _calendarService.ToRealWorld(questDate, "harptos", DateTimeZone.Utc);

        LogEvent("Quest Completed", "The Lost Mine of Phandelver", questDate, questRealDate);

        // Simulate item discovery
        var itemDate = new FantasyDate(1372, 1, 17, 18, 45, 0);
        var itemRealDate = _calendarService.ToRealWorld(itemDate, "harptos", DateTimeZone.Utc);

        LogEvent("Legendary Item Found", "Sword of Zariel discovered", itemDate, itemRealDate);

        Console.WriteLine();
    }

    private void LogEvent(string eventName, string details, FantasyDate fantasyDate, ZonedDateTime realDate)
    {
        Console.WriteLine($"[{fantasyDate}] {eventName}");
        Console.WriteLine($"  Details: {details}");
        Console.WriteLine($"  Real-world time: {realDate}");
        Console.WriteLine();
    }
}
