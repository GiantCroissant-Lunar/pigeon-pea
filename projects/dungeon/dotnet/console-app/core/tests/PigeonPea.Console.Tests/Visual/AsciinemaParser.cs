using System.Text.Json;

namespace PigeonPea.Console.Tests.Visual;

/// <summary>
/// Parser for asciinema recording files (v2 format).
/// </summary>
public class AsciinemaParser
{
    /// <summary>
    /// Header information from the asciinema recording.
    /// </summary>
    public class Header
    {
        /// <summary>
        /// Gets or sets the format version (should be 2).
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the terminal width in columns.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the terminal height in rows.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the recording was created.
        /// </summary>
        public long? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the terminal environment type.
        /// </summary>
        public string? Env { get; set; }
    }

    private readonly List<Frame> _frames = new();
    private Header? _header;

    /// <summary>
    /// Gets the header information from the parsed recording.
    /// </summary>
    public Header? RecordingHeader => _header;

    /// <summary>
    /// Gets all frames from the parsed recording.
    /// </summary>
    public IReadOnlyList<Frame> Frames => _frames.AsReadOnly();

    /// <summary>
    /// Parses an asciinema recording from a file.
    /// </summary>
    /// <param name="filePath">Path to the asciinema recording file.</param>
    /// <returns>A new AsciinemaParser instance with the parsed data.</returns>
    public static AsciinemaParser ParseFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        return Parse(lines);
    }

    /// <summary>
    /// Parses an asciinema recording from lines of text.
    /// </summary>
    /// <param name="lines">Lines from an asciinema recording file.</param>
    /// <returns>A new AsciinemaParser instance with the parsed data.</returns>
    public static AsciinemaParser Parse(IEnumerable<string> lines)
    {
        var parser = new AsciinemaParser();
        bool isFirstLine = true;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (isFirstLine)
            {
                // Parse header
                parser._header = ParseHeader(line);
                isFirstLine = false;
            }
            else
            {
                // Parse frame event
                var frame = ParseFrame(line);
                if (frame != null)
                {
                    parser._frames.Add(frame);
                }
            }
        }

        return parser;
    }

    /// <summary>
    /// Finds the frame at or closest to the specified timestamp.
    /// </summary>
    /// <param name="timestamp">The target timestamp in seconds.</param>
    /// <returns>The frame at or closest to the specified timestamp, or null if no frames exist.</returns>
    public Frame? GetFrameAtTimestamp(double timestamp)
    {
        if (_frames.Count == 0)
        {
            return null;
        }

        // Binary search for the frame with the largest timestamp <= target timestamp
        int low = 0;
        int high = _frames.Count - 1;
        int resultIndex = -1;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (_frames[mid].Timestamp <= timestamp)
            {
                resultIndex = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        if (resultIndex == -1)
        {
            // Timestamp is before the first frame, return the first frame as per existing logic
            return _frames[0];
        }

        return _frames[resultIndex];
    }

    /// <summary>
    /// Gets all frames within a time range.
    /// </summary>
    /// <param name="startTime">Start of the time range in seconds.</param>
    /// <param name="endTime">End of the time range in seconds.</param>
    /// <returns>A collection of frames within the specified time range.</returns>
    public IEnumerable<Frame> GetFramesInRange(double startTime, double endTime)
    {
        return _frames.SkipWhile(f => f.Timestamp < startTime)
                      .TakeWhile(f => f.Timestamp <= endTime);
    }

    /// <summary>
    /// Builds the cumulative content up to a specific timestamp.
    /// This accumulates all content from frames up to the given timestamp.
    /// </summary>
    /// <param name="timestamp">The target timestamp in seconds.</param>
    /// <returns>The accumulated content up to the specified timestamp.</returns>
    public string GetAccumulatedContentAtTimestamp(double timestamp)
    {
        var contentToJoin = _frames
            .TakeWhile(f => f.Timestamp <= timestamp)
            .Select(f => f.Content);

        return string.Join(string.Empty, contentToJoin);
    }

    private static Header ParseHeader(string line)
    {
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        var header = new Header
        {
            Version = root.GetProperty("version").GetInt32(),
            Width = root.GetProperty("width").GetInt32(),
            Height = root.GetProperty("height").GetInt32()
        };

        if (root.TryGetProperty("timestamp", out var timestamp))
        {
            header.Timestamp = timestamp.GetInt64();
        }

        if (root.TryGetProperty("env", out var env))
        {
            if (env.ValueKind == JsonValueKind.Object)
            {
                header.Env = env.ToString();
            }
        }

        return header;
    }

    private static Frame? ParseFrame(string line)
    {
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        // Asciinema v2 format: [timestamp, event_type, data]
        if (root.GetArrayLength() < 3)
        {
            return null;
        }

        var timestamp = root[0].GetDouble();
        var eventType = root[1].GetString();

        // We're only interested in "o" (output) events
        if (eventType != "o")
        {
            return null;
        }

        var content = root[2].GetString() ?? string.Empty;

        return new Frame
        {
            Timestamp = timestamp,
            Content = content
        };
    }
}
