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

    // Cached values for performance
    private double _minFrameTime = double.MaxValue;
    private double _maxFrameTime = double.MinValue;
    private double _sumFrameTimes;
    private List<double>? _sortedFrameTimes;

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
            if (_frameTimes.Count == 0)
                return 0;
            return _sumFrameTimes > 0
                ? (_frameTimes.Count / (_sumFrameTimes / 1000.0))
                : 0;
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
            return _maxFrameTime > 0 ? 1000.0 / _maxFrameTime : 0;
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
            return _minFrameTime > 0 ? 1000.0 / _minFrameTime : 0;
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
            return _sumFrameTimes / _frameTimes.Count;
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
            return _minFrameTime;
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
            return _maxFrameTime;
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
        RecordFrame(TimeSpan.FromMilliseconds(frameTime));
        _lastFrameTime = currentTime;
    }

    /// <summary>
    /// Records a frame using a supplied frame duration. Useful for deterministic testing scenarios.
    /// </summary>
    /// <param name="frameDuration">Duration of the frame (time between frames).</param>
    public void RecordFrame(TimeSpan frameDuration)
    {
        if (frameDuration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(frameDuration));

        if (!_stopwatch.IsRunning)
            _stopwatch.Start();

        var frameTime = frameDuration.TotalMilliseconds;
        if (frameTime == 0)
            frameTime = double.Epsilon;

        _frameTimes.Add(frameTime);
        _frameCount++;

        _sumFrameTimes += frameTime;
        if (frameTime < _minFrameTime)
            _minFrameTime = frameTime;
        if (frameTime > _maxFrameTime)
            _maxFrameTime = frameTime;

        _sortedFrameTimes = null;
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

        // Cache the sorted list to avoid repeated sorting
        _sortedFrameTimes ??= _frameTimes.OrderBy(x => x).ToList();

        var index = (percentile / 100.0) * (_sortedFrameTimes.Count - 1);
        var lowerIndex = (int)Math.Floor(index);
        var upperIndex = (int)Math.Ceiling(index);

        if (lowerIndex == upperIndex)
            return _sortedFrameTimes[lowerIndex];

        var lowerValue = _sortedFrameTimes[lowerIndex];
        var upperValue = _sortedFrameTimes[upperIndex];
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

        // Reset cached values
        _minFrameTime = double.MaxValue;
        _maxFrameTime = double.MinValue;
        _sumFrameTimes = 0;
        _sortedFrameTimes = null;
    }
}
