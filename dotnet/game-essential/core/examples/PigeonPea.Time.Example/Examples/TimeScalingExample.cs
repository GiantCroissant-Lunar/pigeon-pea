using NodaTime;
using PigeonPea.Time.Contracts;
using PigeonPea.Time.Core;

namespace PigeonPea.Time.Example.Examples;

/// <summary>
/// Demonstrates time-based progression with different time scales.
/// Use Case 3 from RFC-015.
/// </summary>
public class TimeScalingExample
{
    public void Run()
    {
        Console.WriteLine("=== Time Scaling Example ===\n");

        // Real-time (1:1)
        DemonstrateTimeScale("Real-time", 1.0);

        // 10× speed (1 real minute = 10 game minutes)
        DemonstrateTimeScale("10× Speed", 10.0);

        // 60× speed (1 real minute = 1 game hour)
        DemonstrateTimeScale("60× Speed", 60.0);

        // 1440× speed (1 real minute = 1 game day)
        DemonstrateTimeScale("1440× Speed (1 real min = 1 game day)", 1440.0);

        Console.WriteLine();
    }

    private void DemonstrateTimeScale(string description, double multiplier)
    {
        var epoch = Instant.FromUtc(2025, 1, 1, 0, 0);
        var clock = new WorldClock(realEpoch: epoch, realSecondsPerGameSecond: 1.0 / multiplier);
        var service = new CalendarService(clock);
        service.RegisterCalendar("harptos", new Plugin.Time.Harptos.HarptosCalendar());

        // Simulate 10 real minutes passing
        var tenMinutesLater = epoch.Plus(Duration.FromMinutes(10));
        var startDate = service.FromRealWorld(epoch.InUtc(), "harptos");
        var endDate = service.FromRealWorld(tenMinutesLater.InUtc(), "harptos");

        Console.WriteLine($"{description}:");
        Console.WriteLine($"  Multiplier: {multiplier}×");
        Console.WriteLine($"  Start: {startDate}");
        Console.WriteLine($"  After 10 real minutes: {endDate}");
        Console.WriteLine($"  Game time elapsed: {CalculateElapsed(startDate, endDate)}");
        Console.WriteLine();
    }

    private string CalculateElapsed(FantasyDate start, FantasyDate end)
    {
        var daysDiff = (end.Year - start.Year) * 365 + (end.Month - start.Month) * 30 + (end.Day - start.Day);
        var hoursDiff = end.Hour - start.Hour;
        var minutesDiff = end.Minute - start.Minute;

        if (daysDiff > 0)
            return $"{daysDiff} days, {hoursDiff} hours, {minutesDiff} minutes";
        if (hoursDiff > 0)
            return $"{hoursDiff} hours, {minutesDiff} minutes";
        return $"{minutesDiff} minutes";
    }
}
