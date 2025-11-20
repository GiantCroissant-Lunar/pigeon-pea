using System.Text.Json.Serialization;
using PigeonPea.Time.Contracts;

namespace PigeonPea.Plugin.Time.Configurable;

public class ConfigurableCalendar : ICalendarDefinition
{
    public string Name { get; }
    public long TicksPerDay { get; }

    private readonly CalendarConfig _config;

    public ConfigurableCalendar(CalendarConfig config)
    {
        _config = config;
        Name = config.Name;
        TicksPerDay = config.TicksPerDay;
    }

    public FantasyDate FromWorldTick(WorldTick tick)
    {
        long totalTicks = tick.Value;
        long totalDays = totalTicks / TicksPerDay;

        long ticksInDay = totalTicks % TicksPerDay;

        int second = (int)(ticksInDay % 60);
        int minute = (int)((ticksInDay / 60) % 60);
        int hour = (int)((ticksInDay / 3600));

        int year = 1;
        while (true)
        {
            int daysInThisYear = GetDaysInYear(year);
            if (totalDays < daysInThisYear) break;
            totalDays -= daysInThisYear;
            year++;
        }

        int dayOfYear = (int)totalDays;

        int month = 1;
        foreach (var monthConfig in _config.Months)
        {
            int daysInMonth = monthConfig.Days;
            if (IsLeapYear(year) && monthConfig.LeapDayRule != null)
            {
                daysInMonth += 1;
            }

            if (dayOfYear < daysInMonth)
            {
                return new FantasyDate(year, month, dayOfYear + 1, hour, minute, second);
            }

            dayOfYear -= daysInMonth;
            month++;
        }

        return new FantasyDate(year, 1, 1, 0, 0, 0);
    }

    public WorldTick ToWorldTick(FantasyDate date)
    {
        long totalDays = 0;

        for (int y = 1; y < date.Year; y++)
        {
            totalDays += GetDaysInYear(y);
        }

        for (int m = 0; m < date.Month - 1; m++)
        {
            var monthConfig = _config.Months[m];
            totalDays += monthConfig.Days;
            if (IsLeapYear(date.Year) && monthConfig.LeapDayRule != null)
            {
                totalDays += 1;
            }
        }

        totalDays += date.Day - 1;

        long totalTicks = totalDays * TicksPerDay
                          + date.Hour * 3600
                          + date.Minute * 60
                          + date.Second;

        return new WorldTick(totalTicks);
    }

    private int GetDaysInYear(int year)
    {
        int days = _config.Months.Sum(m => m.Days);
        if (IsLeapYear(year))
        {
            days += _config.Months.Count(m => m.LeapDayRule != null);
        }
        return days;
    }

    private bool IsLeapYear(int year)
    {
        if (_config.LeapRule == null) return false;
        return (year % _config.LeapRule.Interval) == 0;
    }
}

public class CalendarConfig
{
    public string Name { get; set; } = "Custom";
    public long TicksPerDay { get; set; } = 86400;
    public List<MonthConfig> Months { get; set; } = new();
    public LeapRule? LeapRule { get; set; }
}

public class MonthConfig
{
    public string Name { get; set; } = "";
    public int Days { get; set; }
    public LeapDayRule? LeapDayRule { get; set; }
}

public class LeapRule
{
    public int Interval { get; set; }
}

public class LeapDayRule
{
    // Marker to indicate this month gets an extra day
}
