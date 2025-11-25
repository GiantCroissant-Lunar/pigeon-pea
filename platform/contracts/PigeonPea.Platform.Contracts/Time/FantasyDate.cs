namespace PigeonPea.Platform.Contracts.Time;

/// <summary>
/// Represents a date and time in a specific fantasy calendar.
/// </summary>
public readonly record struct FantasyDate(
    int Year,
    int Month,
    int Day,
    int Hour,
    int Minute,
    int Second
) : IComparable<FantasyDate>
{
    public int CompareTo(FantasyDate other)
    {
        var yearComparison = Year.CompareTo(other.Year);
        if (yearComparison != 0) return yearComparison;
        var monthComparison = Month.CompareTo(other.Month);
        if (monthComparison != 0) return monthComparison;
        var dayComparison = Day.CompareTo(other.Day);
        if (dayComparison != 0) return dayComparison;
        var hourComparison = Hour.CompareTo(other.Hour);
        if (hourComparison != 0) return hourComparison;
        var minuteComparison = Minute.CompareTo(other.Minute);
        if (minuteComparison != 0) return minuteComparison;
        return Second.CompareTo(other.Second);
    }

    public override string ToString() => $"{Year}-{Month:D2}-{Day:D2} {Hour:D2}:{Minute:D2}:{Second:D2}";
    public static bool operator <(FantasyDate left, FantasyDate right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(FantasyDate left, FantasyDate right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(FantasyDate left, FantasyDate right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(FantasyDate left, FantasyDate right)
    {
        return left.CompareTo(right) >= 0;
    }
}
