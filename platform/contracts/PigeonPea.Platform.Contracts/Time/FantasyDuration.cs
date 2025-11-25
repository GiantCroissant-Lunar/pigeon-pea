namespace PigeonPea.Platform.Contracts.Time;

/// <summary>
/// Represents a duration or time span in fantasy calendar units.
/// </summary>
public readonly record struct FantasyDuration(
    int Years,
    int Months,
    int Days,
    int Hours,
    int Minutes,
    int Seconds
)
{
    /// <summary>
    /// Gets a zero duration.
    /// </summary>
    public static FantasyDuration Zero => new(0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Calculates the duration between two fantasy dates using the specified calendar.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <param name="calendar">The calendar definition to use for calculations.</param>
    /// <returns>A FantasyDuration representing the time span between the dates.</returns>
    public static FantasyDuration Between(FantasyDate start, FantasyDate end, ICalendarDefinition calendar)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));

        // Convert both dates to ticks for accurate calculation
        var startTick = calendar.ToWorldTick(start);
        var endTick = calendar.ToWorldTick(end);
        var tickDifference = endTick.Value - startTick.Value;

        // Handle negative durations
        var isNegative = tickDifference < 0;
        var absoluteTicks = Math.Abs(tickDifference);

        // Calculate components from ticks
        var ticksPerSecond = calendar.TicksPerDay / (calendar.HoursPerDay * calendar.MinutesPerHour * calendar.SecondsPerMinute);
        var ticksPerMinute = calendar.TicksPerDay / (calendar.HoursPerDay * calendar.MinutesPerHour);
        var ticksPerHour = calendar.TicksPerDay / calendar.HoursPerDay;
        var ticksPerDay = calendar.TicksPerDay;

        var totalSeconds = absoluteTicks / ticksPerSecond;
        var seconds = (int)(totalSeconds % calendar.SecondsPerMinute);

        var totalMinutes = totalSeconds / calendar.SecondsPerMinute;
        var minutes = (int)(totalMinutes % calendar.MinutesPerHour);

        var totalHours = totalMinutes / calendar.MinutesPerHour;
        var hours = (int)(totalHours % calendar.HoursPerDay);

        var totalDays = (int)(totalHours / calendar.HoursPerDay);

        // For simplicity and correctness, we avoid calculating years and months from ticks
        // as month lengths can vary and we don't have DaysInMonth metadata.
        // We report the total duration in Days/Hours/Minutes/Seconds to avoid double counting.
        var years = 0;
        var months = 0;
        var days = totalDays;

        // Adjust for negative durations
        if (isNegative)
        {
            days = -days;
            hours = -hours;
            minutes = -minutes;
            seconds = -seconds;
        }

        return new FantasyDuration(years, months, days, hours, minutes, seconds);
    }

    /// <summary>
    /// Gets the total number of days in this duration (approximate).
    /// </summary>
    public double TotalDays => Days + (Hours / 24.0) + (Minutes / (24.0 * 60.0)) + (Seconds / (24.0 * 60.0 * 60.0));

    /// <summary>
    /// Gets the total number of hours in this duration (approximate).
    /// </summary>
    public double TotalHours => (Days * 24.0) + Hours + (Minutes / 60.0) + (Seconds / 3600.0);

    /// <summary>
    /// Gets the total number of seconds in this duration.
    /// </summary>
    public double TotalSeconds => (Days * 24.0 * 3600.0) + (Hours * 3600.0) + (Minutes * 60.0) + Seconds;

    /// <summary>
    /// Returns a string representation of this duration.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();

        if (Years != 0) parts.Add($"{Years} year{(Math.Abs(Years) != 1 ? "s" : "")}");
        if (Months != 0) parts.Add($"{Months} month{(Math.Abs(Months) != 1 ? "s" : "")}");
        if (Days != 0) parts.Add($"{Days} day{(Math.Abs(Days) != 1 ? "s" : "")}");
        if (Hours != 0) parts.Add($"{Hours} hour{(Math.Abs(Hours) != 1 ? "s" : "")}");
        if (Minutes != 0) parts.Add($"{Minutes} minute{(Math.Abs(Minutes) != 1 ? "s" : "")}");
        if (Seconds != 0) parts.Add($"{Seconds} second{(Math.Abs(Seconds) != 1 ? "s" : "")}");

        return parts.Count > 0 ? string.Join(", ", parts) : "0 seconds";
    }
}
