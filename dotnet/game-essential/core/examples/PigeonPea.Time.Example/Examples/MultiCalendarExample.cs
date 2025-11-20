using NodaTime;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;

namespace PigeonPea.Time.Example.Examples;

/// <summary>
/// Demonstrates converting dates between different fantasy calendars.
/// Use Case 4 from RFC-015.
/// </summary>
public class MultiCalendarExample
{
    private readonly ICalendarService _calendarService;

    public MultiCalendarExample(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    public void Run()
    {
        Console.WriteLine("=== Multi-Calendar Conversion Example ===\n");

        // For this example, we'll demonstrate the concept with Harptos
        // In a real application, you would register multiple calendars
        Console.WriteLine("Demonstrating calendar conversion capabilities:");
        Console.WriteLine();

        var harptosDate = new FantasyDate(1372, 1, 15, 14, 30, 0);
        
        // Show the same moment in different representations
        Console.WriteLine("Same Moment in Different Representations:");
        var realDate = _calendarService.ToRealWorld(harptosDate, "harptos", DateTimeZone.Utc);
        Console.WriteLine($"  Fantasy (Harptos): {harptosDate}");
        Console.WriteLine($"  Real-world (UTC):  {realDate}");
        Console.WriteLine();

        // Demonstrate round-trip conversion
        var backToFantasy = _calendarService.FromRealWorld(realDate, "harptos");
        Console.WriteLine("Round-trip Conversion:");
        Console.WriteLine($"  Original:  {harptosDate}");
        Console.WriteLine($"  To Real:   {realDate}");
        Console.WriteLine($"  Back:      {backToFantasy}");
        Console.WriteLine($"  Match:     {harptosDate == backToFantasy}");
        Console.WriteLine();

        // Show how you would convert between calendars
        Console.WriteLine("Multi-Calendar Conversion Pattern:");
        Console.WriteLine("  1. Register multiple calendars with CalendarService");
        Console.WriteLine("  2. Use Convert(date, \"from-calendar\", \"to-calendar\")");
        Console.WriteLine("  3. All conversions go through WorldTick for accuracy");
        Console.WriteLine();
        Console.WriteLine("Example code:");
        Console.WriteLine("  service.RegisterCalendar(\"harptos\", new HarptosCalendar());");
        Console.WriteLine("  service.RegisterCalendar(\"elven\", new ElvenCalendar());");
        Console.WriteLine("  var elvenDate = service.Convert(harptosDate, \"harptos\", \"elven\");");
        Console.WriteLine();
    }
}
