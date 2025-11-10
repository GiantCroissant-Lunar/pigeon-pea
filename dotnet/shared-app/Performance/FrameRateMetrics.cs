using System.Diagnostics;

namespace PigeonPea.Shared.Performance;

/// <summary>
/// Tracks and calculates frame rate metrics including FPS, frame times, and percentiles.
/// </summary>
public class FrameRateMetrics
{
    private readonly List<double> _frameTimes = new();
    private readonly Stopwatch _stopwatch = new();
    private double _lastFrameTime;
    private int _frameCount;

    /// <summary>
    /// Gets the total number of frames recorded.
    /// </summary>
    public int FrameCount => _frameCount;

    /// <summary>
    /// Gets the average frames per second.
    /// </summary>
    public double AverageFPS
    {
        get
        {
            if (_frameCount == 0 || _stopwatch.Elapsed.TotalSeconds == 0)
                return 0;
            return _frameCount / _stopwatch.Elapsed.TotalSeconds;
        }
    }

    /// <summary>
    /// Gets the minimum frames per second.
    /// </summary>
    public double MinFPS
    {
        get
        {
            if (_frameTimes.Count == 0)
                return 0;
            var maxFrameTime = _frameTimes.Max();
            return maxFrameTime > 0 ? 1000.0 / maxFrameTime : 0;
        }
    }

    /// <summary>
    /// Gets the maximum frames per second.
    /// </summary>
    public double MaxFPS
    {
        get
        {
            if (_frameTimes.Count == 0)
                return 0;
            var minFrameTime = _frameTimes.Min();
            return minFrameTime > 0 ? 1000.0 / minFrameTime : 0;
        }
    }

    /// <summary>
    /// Gets the average frame time in milliseconds.
    /// </summary>
    public double AverageFrameTime
    {
        get
        {
            if (_frameTimes.Count == 0)
                return 0;
            return _frameTimes.Average();
        }
    }

    /// <summary>
    /// Gets the minimum frame time in milliseconds.
    /// </summary>
    public double MinFrameTime
    {
        get
        {
            if (_frameTimes.Count == 0)
                return 0;
            return _frameTimes.Min();
        }
    }

    /// <summary>
    /// Gets the maximum frame time in milliseconds.
    /// </summary>
    public double MaxFrameTime
    {
        get
        {
            if (_frameTimes.Count == 0)
                return 0;
            return _frameTimes.Max();
        }
    }

    /// <summary>
    /// Records a frame and calculates its frame time.
    /// Starts the stopwatch on the first frame.
    /// </summary>
    public void RecordFrame()
    {
        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();
            _lastFrameTime = _stopwatch.Elapsed.TotalMilliseconds;
            _frameCount++;
            return;
        }

        var currentTime = _stopwatch.Elapsed.TotalMilliseconds;
        var frameTime = currentTime - _lastFrameTime;
        _frameTimes.Add(frameTime);
        _lastFrameTime = currentTime;
        _frameCount++;
    }

    /// <summary>
    /// Calculates the specified percentile of frame times in milliseconds.
    /// </summary>
    /// <param name="percentile">The percentile to calculate (0-100).</param>
    /// <returns>The frame time at the specified percentile in milliseconds.</returns>
    public double GetPercentile(double percentile)
    {
        if (percentile < 0 || percentile > 100)
            throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0 and 100.");

        if (_frameTimes.Count == 0)
            return 0;

        var sorted = _frameTimes.OrderBy(x => x).ToList();
        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lowerIndex = (int)Math.Floor(index);
        var upperIndex = (int)Math.Ceiling(index);

        if (lowerIndex == upperIndex)
            return sorted[lowerIndex];

        var lowerValue = sorted[lowerIndex];
        var upperValue = sorted[upperIndex];
        var weight = index - lowerIndex;
        return lowerValue + (upperValue - lowerValue) * weight;
    }

    /// <summary>
    /// Gets the 50th percentile (median) frame time in milliseconds.
    /// </summary>
    public double P50 => GetPercentile(50);

    /// <summary>
    /// Gets the 95th percentile frame time in milliseconds.
    /// </summary>
    public double P95 => GetPercentile(95);

    /// <summary>
    /// Gets the 99th percentile frame time in milliseconds.
    /// </summary>
    public double P99 => GetPercentile(99);

    /// <summary>
    /// Resets all metrics and clears recorded data.
    /// </summary>
    public void Reset()
    {
        _frameTimes.Clear();
        _stopwatch.Reset();
        _lastFrameTime = 0;
        _frameCount = 0;
    }
}
