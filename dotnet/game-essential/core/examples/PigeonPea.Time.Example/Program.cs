using NodaTime;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;
using PigeonPea.Plugin.Time.Harptos;
using PigeonPea.Time.Example.Examples;

namespace PigeonPea.Time.Example;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  PigeonPea Fantasy Calendar Time System - Examples         ║");
        Console.WriteLine("║  RFC-015: Fantasy Calendar to Real-World Transformation    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Setup
        var worldClock = new WorldClock(
            realEpoch: Instant.FromUtc(2025, 1, 1, 0, 0),
            realSecondsPerGameSecond: 1.0  // Real-time for examples
        );

        var calendarService = new CalendarService(worldClock);
        calendarService.RegisterCalendar("harptos", new HarptosCalendar());

        var harptosCalendar = new HarptosCalendar();

        // Run examples
        RunExample(1, "Event Logging", () =>
        {
            var example = new EventLoggingExample(calendarService, harptosCalendar);
            example.Run();
        });

        RunExample(2, "Scheduled Events", () =>
        {
            var example = new ScheduledEventsExample(calendarService);
            example.Run();
        });

        RunExample(3, "Time Scaling", () =>
        {
            var example = new TimeScalingExample();
            example.Run();
        });

        RunExample(4, "Multi-Calendar Conversion", () =>
        {
            var example = new MultiCalendarExample(calendarService);
            example.Run();
        });

        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  All examples completed successfully!                      ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static void RunExample(int number, string name, Action example)
    {
        Console.WriteLine($"┌─ Example {number}: {name} ─────────────────────────────────────────┐");
        Console.WriteLine();

        try
        {
            example();
            Console.WriteLine($"└─ Example {number} Complete ──────────────────────────────────────────┘");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine($"└─ Example {number} Failed ────────────────────────────────────────────┘");
        }

        Console.WriteLine();
    }
}
