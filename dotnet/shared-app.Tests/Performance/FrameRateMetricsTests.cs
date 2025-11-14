using PigeonPea.Shared.Performance;
using Xunit;

namespace PigeonPea.Shared.Tests.Performance;

/// <summary>
/// Unit tests for FrameRateMetrics class.
/// </summary>
public class FrameRateMetricsTests
{
    [Fact]
    public void ConstructorInitializesWithZeroValues()
    {
        // Arrange & Act
        var metrics = new FrameRateMetrics();

        // Assert
        Assert.Equal(0, metrics.FrameCount);
        Assert.Equal(0, metrics.AverageFPS);
        Assert.Equal(0, metrics.MinFPS);
        Assert.Equal(0, metrics.MaxFPS);
        Assert.Equal(0, metrics.AverageFrameTime);
        Assert.Equal(0, metrics.MinFrameTime);
        Assert.Equal(0, metrics.MaxFrameTime);
        Assert.Equal(0, metrics.P50);
        Assert.Equal(0, metrics.P95);
        Assert.Equal(0, metrics.P99);
    }

    [Fact]
    public void RecordFrameFirstFrameIncrementsFrameCount()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act
        metrics.RecordFrame();

        // Assert
        Assert.Equal(1, metrics.FrameCount);
    }

    [Fact]
    public void RecordFrameMultipleFramesIncrementsFrameCount()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act
        metrics.RecordFrame();
        Thread.Sleep(16); // ~60 FPS
        metrics.RecordFrame();
        Thread.Sleep(16);
        metrics.RecordFrame();

        // Assert
        Assert.Equal(3, metrics.FrameCount);
    }

    [Fact]
    public void AverageFPSWithFramesCalculatesCorrectly()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Simulate ~60 FPS for 5 frames (4 intervals)
        for (int i = 0; i < 5; i++)
        {
            metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        }

        // Assert - Should be approximately 60 FPS (with some tolerance for timing)
        // Note: 5 frames = 4 intervals, so actual FPS will be slightly lower
        Assert.InRange(metrics.AverageFPS, 58, 65);
    }

    [Fact]
    public void MinFPSWithFramesReturnsLowestFPS()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with varying times
        metrics.RecordFrame(TimeSpan.FromMilliseconds(10));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(50));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(10));

        // Assert - MinFPS should correspond to the longest frame time (50ms = ~20 FPS)
        Assert.True(metrics.MinFPS < 25, $"MinFPS was {metrics.MinFPS}, expected < 25");
    }

    [Fact]
    public void MaxFPSWithFramesReturnsHighestFPS()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with varying times
        metrics.RecordFrame(TimeSpan.FromMilliseconds(50));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(10));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(50));

        // Assert - MaxFPS should correspond to the shortest frame time (10ms = ~100 FPS)
        Assert.True(metrics.MaxFPS > 80, $"MaxFPS was {metrics.MaxFPS}, expected > 80");
    }

    [Fact]
    public void AverageFrameTimeWithFramesCalculatesCorrectly()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with consistent timing
        for (int i = 0; i < 5; i++)
        {
            metrics.RecordFrame(TimeSpan.FromMilliseconds(20));
        }

        // Assert - Average should be around 20ms (with some tolerance)
        Assert.InRange(metrics.AverageFrameTime, 19, 21);
    }

    [Fact]
    public void MinFrameTimeWithFramesReturnsShortestTime()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with varying times
        metrics.RecordFrame(TimeSpan.FromMilliseconds(50));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(5));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(30));

        // Assert
        Assert.True(metrics.MinFrameTime < 10, $"MinFrameTime was {metrics.MinFrameTime}, expected < 10");
    }

    [Fact]
    public void MaxFrameTimeWithFramesReturnsLongestTime()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with varying times
        metrics.RecordFrame(TimeSpan.FromMilliseconds(10));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(60));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(20));

        // Assert
        Assert.True(metrics.MaxFrameTime > 50, $"MaxFrameTime was {metrics.MaxFrameTime}, expected > 50");
    }

    [Fact]
    public void GetPercentileWithInvalidPercentileThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.GetPercentile(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.GetPercentile(101));
    }

    [Fact]
    public void GetPercentileWithNoFramesReturnsZero()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act & Assert
        Assert.Equal(0, metrics.GetPercentile(50));
        Assert.Equal(0, metrics.GetPercentile(95));
        Assert.Equal(0, metrics.GetPercentile(99));
    }

    [Fact]
    public void GetPercentileWithFramesCalculatesCorrectly()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with predictable times
        metrics.RecordFrame(TimeSpan.FromMilliseconds(10));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(20));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(30));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(40));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(50));

        // Assert - With times roughly 10, 20, 30, 40, 50ms
        // P50 should be around 30ms
        Assert.InRange(metrics.P50, 20, 40);
        // P95 should be closer to 50ms
        Assert.InRange(metrics.P95, 40, 60);
    }

    [Fact]
    public void P50ReturnsMedianFrameTime()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act
        for (int i = 0; i < 10; i++)
        {
            metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        }

        // Assert
        Assert.True(metrics.P50 > 0);
        Assert.True(metrics.P50 <= metrics.MaxFrameTime);
        Assert.True(metrics.P50 >= metrics.MinFrameTime);
    }

    [Fact]
    public void P95Returns95thPercentileFrameTime()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act
        for (int i = 0; i < 10; i++)
        {
            metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        }

        // Assert
        Assert.True(metrics.P95 > 0);
        Assert.True(metrics.P95 >= metrics.P50);
        Assert.True(metrics.P95 <= metrics.MaxFrameTime);
    }

    [Fact]
    public void P99Returns99thPercentileFrameTime()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act
        for (int i = 0; i < 10; i++)
        {
            metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        }

        // Assert
        Assert.True(metrics.P99 > 0);
        Assert.True(metrics.P99 >= metrics.P95);
        Assert.True(metrics.P99 <= metrics.MaxFrameTime);
    }

    [Fact]
    public void ResetClearsAllMetrics()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Record some frames
        for (int i = 0; i < 5; i++)
        {
            metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        }

        // Verify we have data
        Assert.True(metrics.FrameCount > 0);
        Assert.True(metrics.AverageFPS > 0);

        // Act
        metrics.Reset();

        // Assert
        Assert.Equal(0, metrics.FrameCount);
        Assert.Equal(0, metrics.AverageFPS);
        Assert.Equal(0, metrics.MinFPS);
        Assert.Equal(0, metrics.MaxFPS);
        Assert.Equal(0, metrics.AverageFrameTime);
        Assert.Equal(0, metrics.MinFrameTime);
        Assert.Equal(0, metrics.MaxFrameTime);
        Assert.Equal(0, metrics.P50);
        Assert.Equal(0, metrics.P95);
        Assert.Equal(0, metrics.P99);
    }

    [Fact]
    public void ResetAllowsNewMeasurementsAfterReset()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Record and reset
        metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        metrics.Reset();

        // Act - Record new frames
        metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(16));

        // Assert
        Assert.Equal(2, metrics.FrameCount);
        Assert.True(metrics.AverageFPS > 0);
    }

    [Fact]
    public void RecordFrameWithManyFramesCalculatesAccurateMetrics()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Simulate 100 frames at ~60 FPS
        for (int i = 0; i < 100; i++)
        {
            metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        }

        // Assert
        Assert.Equal(100, metrics.FrameCount);
        Assert.InRange(metrics.AverageFPS, 58, 65);
        Assert.True(metrics.MinFPS > 0);
        Assert.True(metrics.MaxFPS >= metrics.MinFPS);
        Assert.True(metrics.P50 > 0);
        Assert.True(metrics.P95 >= metrics.P50);
        Assert.True(metrics.P99 >= metrics.P95);
    }

    [Fact]
    public void FrameRateMetricsHandlesVariableFrameRates()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Simulate variable frame rates
        metrics.RecordFrame(TimeSpan.FromMilliseconds(8));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(33));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(16));
        metrics.RecordFrame(TimeSpan.FromMilliseconds(8));

        // Assert
        Assert.Equal(5, metrics.FrameCount);
        Assert.True(metrics.MaxFPS >= metrics.MinFPS);
        Assert.True(metrics.MinFPS > 0);
        Assert.InRange(metrics.AverageFrameTime, 5, 35);
    }
}
