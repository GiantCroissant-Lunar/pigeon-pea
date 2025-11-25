using NodaTime;
using PigeonPea.Platform.Contracts.Time;

namespace PigeonPea.Platform.TimeManagement;

/// <summary>
/// Handles conversion between real-world time (NodaTime.Instant) and game-world time (WorldTick).
/// </summary>
public class WorldClock
{
    private readonly Instant _realEpoch;
    private readonly double _realSecondsPerGameSecond;
    private readonly long _ticksPerGameSecond;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldClock"/> class.
    /// </summary>
    /// <param name="realEpoch">The real-world time that corresponds to WorldTick.Zero.</param>
    /// <param name="realSecondsPerGameSecond">How many real seconds pass for each game second.
    /// 1.0 = realtime. 0.1 = 10x speed. 60.0 = 1 game second takes 1 real minute (slow).</param>
    /// <param name="ticksPerGameSecond">How many WorldTicks are in one game second. Default 1.</param>
    public WorldClock(Instant realEpoch, double realSecondsPerGameSecond = 1.0, long ticksPerGameSecond = 1)
    {
        _realEpoch = realEpoch;
        _realSecondsPerGameSecond = realSecondsPerGameSecond;
        _ticksPerGameSecond = ticksPerGameSecond;
    }

    /// <summary>
    /// Converts a real-world Instant to a game-world WorldTick.
    /// </summary>
    public WorldTick ToWorldTick(Instant instant)
    {
        Duration durationSinceEpoch = instant - _realEpoch;
        double totalRealSeconds = durationSinceEpoch.TotalSeconds;

        double totalGameSeconds = totalRealSeconds / _realSecondsPerGameSecond;
        long totalTicks = (long)(totalGameSeconds * _ticksPerGameSecond);

        return new WorldTick(totalTicks);
    }

    /// <summary>
    /// Converts a game-world WorldTick to a real-world Instant.
    /// </summary>
    public Instant ToInstant(WorldTick tick)
    {
        double totalGameSeconds = (double)tick.Value / _ticksPerGameSecond;
        double totalRealSeconds = totalGameSeconds * _realSecondsPerGameSecond;

        return _realEpoch + Duration.FromSeconds(totalRealSeconds);
    }
}
