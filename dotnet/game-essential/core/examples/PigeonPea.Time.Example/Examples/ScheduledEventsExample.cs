using NodaTime;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;

namespace PigeonPea.Time.Example.Examples;

/// <summary>
/// Demonstrates scheduling in-game events to real-world times.
/// Use Case 2 from RFC-015.
/// </summary>
public class ScheduledEventsExample
{
    private readonly ICalendarService _calendarService;

    public ScheduledEventsExample(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    public void Run()
    {
        Console.WriteLine("=== Scheduled Events Example ===\n");

        // Schedule Midsummer Festival (month 15 in Harptos)
        var festivalDate = new FantasyDate(1372, 15, 1, 0, 0, 0);
        ScheduleEvent("Festival of Midsummer", festivalDate, "America/Los_Angeles");

        // Schedule Highharvestide (month 11 in Harptos)
        var harvestDate = new FantasyDate(1372, 11, 1, 0, 0, 0);
        ScheduleEvent("Highharvestide Celebration", harvestDate, "America/New_York");

        // Schedule a quest deadline
        var questDeadline = new FantasyDate(1372, 2, 15, 23, 59, 59);
        ScheduleEvent("Quest Deadline: Rescue the Prince", questDeadline, "Europe/London");

        Console.WriteLine();
    }

    private void ScheduleEvent(string eventName, FantasyDate fantasyDate, string timeZoneId)
    {
        var userTimeZone = DateTimeZoneProviders.Tzdb[timeZoneId];
        var realStartTime = _calendarService.ToRealWorld(fantasyDate, "harptos", userTimeZone);

        Console.WriteLine($"Event: {eventName}");
        Console.WriteLine($"  Fantasy Date: {fantasyDate}");
        Console.WriteLine($"  Real-world time ({timeZoneId}): {realStartTime}");
        Console.WriteLine($"  Notification would be sent at: {realStartTime.ToInstant()}");
        Console.WriteLine();
    }
}
