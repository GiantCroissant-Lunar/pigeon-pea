using PigeonPea.Time.Contracts;

namespace PigeonPea.Plugin.Time.Harptos;

public class HarptosCalendar : ICalendarDefinition
{
    public string Name => "Harptos";
    public long TicksPerDay => 24 * 60 * 60; // Assuming 1 tick = 1 second

    private const int NormalYearDays = 365;

    public FantasyDate FromWorldTick(WorldTick tick)
    {
        long totalSeconds = tick.Value;
        long totalDays = totalSeconds / TicksPerDay;
        int second = (int)(totalSeconds % 60);
        int minute = (int)((totalSeconds / 60) % 60);
        int hour = (int)((totalSeconds / 3600) % 24);

        // Calculate Year
        // 4-year cycle: 365 + 365 + 365 + 366 = 1461 days
        long num4YearCycles = totalDays / 1461;
        long daysInCurrentCycle = totalDays % 1461;

        int yearInCycle = 0;
        int daysInYear = NormalYearDays;

        if (daysInCurrentCycle < 365) { yearInCycle = 0; daysInYear = 365; }
        else if (daysInCurrentCycle < 730) { yearInCycle = 1; daysInCurrentCycle -= 365; daysInYear = 365; }
        else if (daysInCurrentCycle < 1095) { yearInCycle = 2; daysInCurrentCycle -= 730; daysInYear = 365; }
        else { yearInCycle = 3; daysInCurrentCycle -= 1095; daysInYear = 366; }

        int year = (int)(num4YearCycles * 4 + yearInCycle) + 1; // Start year 1
        int dayOfYear = (int)daysInCurrentCycle; // 0-indexed

        var (month, day) = GetMonthAndDay(dayOfYear, yearInCycle == 3);

        return new FantasyDate(year, month, day, hour, minute, second);
    }

    public WorldTick ToWorldTick(FantasyDate date)
    {
        // Calculate days from years
        long yearIndex = date.Year - 1;
        long num4YearCycles = yearIndex / 4;
        int yearInCycle = (int)(yearIndex % 4);

        long totalDays = num4YearCycles * 1461;
        if (yearInCycle > 0) totalDays += 365;
        if (yearInCycle > 1) totalDays += 365;
        if (yearInCycle > 2) totalDays += 365;

        // Add days from current year
        bool isLeap = yearInCycle == 3;
        totalDays += GetDayOfYear(date.Month, date.Day, isLeap);

        long totalSeconds = totalDays * TicksPerDay
                            + date.Hour * 3600
                            + date.Minute * 60
                            + date.Second;

        return new WorldTick(totalSeconds);
    }

    private (int Month, int Day) GetMonthAndDay(int dayOfYear, bool isLeap)
    {
        if (dayOfYear < 30) return (1, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear == 0) return (13, 1);
        dayOfYear--;

        if (dayOfYear < 30) return (2, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear < 30) return (3, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear < 30) return (4, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear == 0) return (14, 1);
        dayOfYear--;

        if (dayOfYear < 30) return (5, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear < 30) return (6, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear < 30) return (7, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear == 0) return (15, 1);
        dayOfYear--;

        if (isLeap)
        {
            if (dayOfYear == 0) return (16, 1);
            dayOfYear--;
        }

        if (dayOfYear < 30) return (8, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear < 30) return (9, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear == 0) return (17, 1);
        dayOfYear--;

        if (dayOfYear < 30) return (10, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear < 30) return (11, dayOfYear + 1);
        dayOfYear -= 30;

        if (dayOfYear == 0) return (18, 1);
        dayOfYear--;

        if (dayOfYear < 30) return (12, dayOfYear + 1);

        return (1, 1);
    }

    private int GetDayOfYear(int month, int day, bool isLeap)
    {
        int days = 0;

        if (month == 1) return days + day - 1;
        days += 30;

        if (month == 13) return days;
        days += 1;

        if (month == 2) return days + day - 1;
        days += 30;

        if (month == 3) return days + day - 1;
        days += 30;

        if (month == 4) return days + day - 1;
        days += 30;

        if (month == 14) return days;
        days += 1;

        if (month == 5) return days + day - 1;
        days += 30;

        if (month == 6) return days + day - 1;
        days += 30;

        if (month == 7) return days + day - 1;
        days += 30;

        if (month == 15) return days;
        days += 1;

        if (isLeap)
        {
            if (month == 16) return days;
            days += 1;
        }

        if (month == 8) return days + day - 1;
        days += 30;

        if (month == 9) return days + day - 1;
        days += 30;

        if (month == 17) return days;
        days += 1;

        if (month == 10) return days + day - 1;
        days += 30;

        if (month == 11) return days + day - 1;
        days += 30;

        if (month == 18) return days;
        days += 1;

        if (month == 12) return days + day - 1;

        return days;
    }
}
