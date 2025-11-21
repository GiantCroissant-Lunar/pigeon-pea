using PigeonPea.Time.Contracts;

namespace PigeonPea.Time.Core;

/// <summary>
/// Handles formatting of fantasy dates into strings using customizable patterns.
/// </summary>
public class FantasyDateFormatter
{
    private readonly ICalendarDefinition _calendarDefinition;

    /// <summary>
    /// Initializes a new instance of the <see cref="FantasyDateFormatter"/> class.
    /// </summary>
    /// <param name="calendarDefinition">The calendar definition for formatting context.</param>
    public FantasyDateFormatter(ICalendarDefinition calendarDefinition)
    {
        _calendarDefinition = calendarDefinition ?? throw new ArgumentNullException(nameof(calendarDefinition));
    }

    /// <summary>
    /// Formats a fantasy date using the specified pattern.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="pattern">The format pattern (e.g., "yyyy-MM-dd HH:mm:ss", "MMMM dd, yyyy").</param>
    /// <param name="monthNames">Optional custom month names. If null, default numeric months are used.</param>
    /// <returns>The formatted string representation of the date.</returns>
    public string Format(FantasyDate date, string pattern, string[]? monthNames = null)
    {
        if (date == null) throw new ArgumentNullException(nameof(date));
        if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Pattern cannot be null or whitespace.", nameof(pattern));

        var result = pattern;

        // Replace year patterns
        result = result.Replace("yyyy", date.Year.ToString("D4"));
        result = result.Replace("yy", (date.Year % 100).ToString("D2"));
        result = result.Replace("y", date.Year.ToString());

        // Replace month name patterns (longer patterns first)
        if (monthNames != null && monthNames.Length > 0)
        {
            int monthIndex = Math.Max(0, Math.Min(date.Month - 1, monthNames.Length - 1));
            result = result.Replace("MMMM", monthNames[monthIndex]);

            // For abbreviated month names, take first 3 characters if available
            string abbreviated = monthNames[monthIndex].Length >= 3
                ? monthNames[monthIndex].Substring(0, 3)
                : monthNames[monthIndex];
            result = result.Replace("MMM", abbreviated);
        }
        else
        {
            // Default to numeric month names if no custom names provided
            result = result.Replace("MMMM", $"Month {date.Month}");
            result = result.Replace("MMM", $"M{date.Month}");
        }

        // Replace month patterns (shorter patterns after longer ones)
        result = result.Replace("MM", date.Month.ToString("D2"));
        result = result.Replace("M", date.Month.ToString());

        // Replace day patterns
        result = result.Replace("dd", date.Day.ToString("D2"));
        result = result.Replace("d", date.Day.ToString());

        // Replace hour patterns
        result = result.Replace("HH", date.Hour.ToString("D2"));
        result = result.Replace("H", date.Hour.ToString());

        // Replace minute patterns
        result = result.Replace("mm", date.Minute.ToString("D2"));
        result = result.Replace("m", date.Minute.ToString());

        // Replace second patterns
        result = result.Replace("ss", date.Second.ToString("D2"));
        result = result.Replace("s", date.Second.ToString());

        return result;
    }

    /// <summary>
    /// Formats a fantasy date using the default ISO-like pattern.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="monthNames">Optional custom month names.</param>
    /// <returns>The formatted string using "yyyy-MM-dd HH:mm:ss" pattern.</returns>
    public string FormatDefault(FantasyDate date, string[]? monthNames = null)
    {
        return Format(date, "yyyy-MM-dd HH:mm:ss", monthNames);
    }

    /// <summary>
    /// Formats a fantasy date using a long date pattern.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="monthNames">Optional custom month names.</param>
    /// <returns>The formatted string using "MMMM dd, yyyy" pattern.</returns>
    public string FormatLongDate(FantasyDate date, string[]? monthNames = null)
    {
        return Format(date, "MMMM dd, yyyy", monthNames);
    }

    /// <summary>
    /// Formats a fantasy date using a short date pattern.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="monthNames">Optional custom month names.</param>
    /// <returns>The formatted string using "MM/dd/yyyy" pattern.</returns>
    public string FormatShortDate(FantasyDate date, string[]? monthNames = null)
    {
        return Format(date, "MM/dd/yyyy", monthNames);
    }

    /// <summary>
    /// Formats a fantasy date using a time-only pattern.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <returns>The formatted string using "HH:mm:ss" pattern.</returns>
    public string FormatTime(FantasyDate date)
    {
        return Format(date, "HH:mm:ss");
    }

    /// <summary>
    /// Formats a fantasy date using a readable pattern.
    /// </summary>
    /// <param name="date">The fantasy date to format.</param>
    /// <param name="monthNames">Optional custom month names.</param>
    /// <returns>The formatted string using "MMMM dd, yyyy HH:mm" pattern.</returns>
    public string FormatReadable(FantasyDate date, string[]? monthNames = null)
    {
        return Format(date, "MMMM dd, yyyy HH:mm", monthNames);
    }
}
