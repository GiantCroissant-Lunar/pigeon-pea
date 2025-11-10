using PigeonPea.Shared.Performance;
using Xunit;

namespace PigeonPea.Shared.Tests.Performance;

/// <summary>
/// Unit tests for FrameRateMetrics class.
/// </summary>
public class FrameRateMetricsTests
{
    [Fact]
    public void Constructor_InitializesWithZeroValues()
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
    public void RecordFrame_FirstFrame_IncrementsFrameCount()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act
        metrics.RecordFrame();

        // Assert
        Assert.Equal(1, metrics.FrameCount);
    }

    [Fact]
    public void RecordFrame_MultipleFrames_IncrementsFrameCount()
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
    public void AverageFPS_WithFrames_CalculatesCorrectly()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Simulate ~60 FPS for 5 frames
        for (int i = 0; i < 5; i++)
        {
            metrics.RecordFrame();
            Thread.Sleep(16); // ~16ms per frame = ~60 FPS
        }

        // Assert - Should be approximately 60 FPS (with some tolerance for timing)
        Assert.InRange(metrics.AverageFPS, 50, 70);
    }

    [Fact]
    public void MinFPS_WithFrames_ReturnsLowestFPS()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with varying times
        metrics.RecordFrame();
        Thread.Sleep(10); // Fast frame
        metrics.RecordFrame();
        Thread.Sleep(50); // Slow frame (lowest FPS)
        metrics.RecordFrame();
        Thread.Sleep(10); // Fast frame
        metrics.RecordFrame();

        // Assert - MinFPS should correspond to the longest frame time (50ms = ~20 FPS)
        Assert.True(metrics.MinFPS < 25, $"MinFPS was {metrics.MinFPS}, expected < 25");
    }

    [Fact]
    public void MaxFPS_WithFrames_ReturnsHighestFPS()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with varying times
        metrics.RecordFrame();
        Thread.Sleep(50); // Slow frame
        metrics.RecordFrame();
        Thread.Sleep(10); // Fast frame (highest FPS)
        metrics.RecordFrame();
        Thread.Sleep(50); // Slow frame
        metrics.RecordFrame();

        // Assert - MaxFPS should correspond to the shortest frame time (10ms = ~100 FPS)
        Assert.True(metrics.MaxFPS > 80, $"MaxFPS was {metrics.MaxFPS}, expected > 80");
    }

    [Fact]
    public void AverageFrameTime_WithFrames_CalculatesCorrectly()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with consistent timing
        for (int i = 0; i < 5; i++)
        {
            metrics.RecordFrame();
            Thread.Sleep(20); // 20ms per frame
        }

        // Assert - Average should be around 20ms (with some tolerance)
        Assert.InRange(metrics.AverageFrameTime, 15, 25);
    }

    [Fact]
    public void MinFrameTime_WithFrames_ReturnsShortestTime()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with varying times
        metrics.RecordFrame();
        Thread.Sleep(50); // Long frame
        metrics.RecordFrame();
        Thread.Sleep(5);  // Short frame
        metrics.RecordFrame();
        Thread.Sleep(30); // Medium frame
        metrics.RecordFrame();

        // Assert
        Assert.True(metrics.MinFrameTime < 10, $"MinFrameTime was {metrics.MinFrameTime}, expected < 10");
    }

    [Fact]
    public void MaxFrameTime_WithFrames_ReturnsLongestTime()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with varying times
        metrics.RecordFrame();
        Thread.Sleep(10); // Short frame
        metrics.RecordFrame();
        Thread.Sleep(60); // Long frame
        metrics.RecordFrame();
        Thread.Sleep(20); // Medium frame
        metrics.RecordFrame();

        // Assert
        Assert.True(metrics.MaxFrameTime > 50, $"MaxFrameTime was {metrics.MaxFrameTime}, expected > 50");
    }

    [Fact]
    public void GetPercentile_WithInvalidPercentile_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.GetPercentile(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => metrics.GetPercentile(101));
    }

    [Fact]
    public void GetPercentile_WithNoFrames_ReturnsZero()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act & Assert
        Assert.Equal(0, metrics.GetPercentile(50));
        Assert.Equal(0, metrics.GetPercentile(95));
        Assert.Equal(0, metrics.GetPercentile(99));
    }

    [Fact]
    public void GetPercentile_WithFrames_CalculatesCorrectly()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Record frames with predictable times
        metrics.RecordFrame();
        Thread.Sleep(10);
        metrics.RecordFrame();
        Thread.Sleep(20);
        metrics.RecordFrame();
        Thread.Sleep(30);
        metrics.RecordFrame();
        Thread.Sleep(40);
        metrics.RecordFrame();
        Thread.Sleep(50);
        metrics.RecordFrame();

        // Assert - With times roughly 10, 20, 30, 40, 50ms
        // P50 should be around 30ms
        Assert.InRange(metrics.P50, 20, 40);
        // P95 should be closer to 50ms
        Assert.InRange(metrics.P95, 40, 60);
    }

    [Fact]
    public void P50_ReturnsMedianFrameTime()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act
        for (int i = 0; i < 10; i++)
        {
            metrics.RecordFrame();
            Thread.Sleep(16);
        }

        // Assert
        Assert.True(metrics.P50 > 0);
        Assert.True(metrics.P50 <= metrics.MaxFrameTime);
        Assert.True(metrics.P50 >= metrics.MinFrameTime);
    }

    [Fact]
    public void P95_Returns95thPercentileFrameTime()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act
        for (int i = 0; i < 10; i++)
        {
            metrics.RecordFrame();
            Thread.Sleep(16);
        }

        // Assert
        Assert.True(metrics.P95 > 0);
        Assert.True(metrics.P95 >= metrics.P50);
        Assert.True(metrics.P95 <= metrics.MaxFrameTime);
    }

    [Fact]
    public void P99_Returns99thPercentileFrameTime()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act
        for (int i = 0; i < 10; i++)
        {
            metrics.RecordFrame();
            Thread.Sleep(16);
        }

        // Assert
        Assert.True(metrics.P99 > 0);
        Assert.True(metrics.P99 >= metrics.P95);
        Assert.True(metrics.P99 <= metrics.MaxFrameTime);
    }

    [Fact]
    public void Reset_ClearsAllMetrics()
    {
        // Arrange
        var metrics = new FrameRateMetrics();
        
        // Record some frames
        for (int i = 0; i < 5; i++)
        {
            metrics.RecordFrame();
            Thread.Sleep(16);
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
    public void Reset_AllowsNewMeasurementsAfterReset()
    {
        // Arrange
        var metrics = new FrameRateMetrics();
        
        // Record and reset
        metrics.RecordFrame();
        Thread.Sleep(16);
        metrics.RecordFrame();
        metrics.Reset();

        // Act - Record new frames
        metrics.RecordFrame();
        Thread.Sleep(16);
        metrics.RecordFrame();

        // Assert
        Assert.Equal(2, metrics.FrameCount);
        Assert.True(metrics.AverageFPS > 0);
    }

    [Fact]
    public void RecordFrame_WithManyFrames_CalculatesAccurateMetrics()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Simulate 100 frames at ~60 FPS
        for (int i = 0; i < 100; i++)
        {
            metrics.RecordFrame();
            Thread.Sleep(16);
        }

        // Assert
        Assert.Equal(100, metrics.FrameCount);
        Assert.InRange(metrics.AverageFPS, 50, 70);
        Assert.True(metrics.MinFPS > 0);
        Assert.True(metrics.MaxFPS > metrics.MinFPS);
        Assert.True(metrics.P50 > 0);
        Assert.True(metrics.P95 > metrics.P50);
        Assert.True(metrics.P99 >= metrics.P95);
    }

    [Fact]
    public void FrameRateMetrics_HandlesVariableFrameRates()
    {
        // Arrange
        var metrics = new FrameRateMetrics();

        // Act - Simulate variable frame rates
        metrics.RecordFrame();
        Thread.Sleep(8);   // 125 FPS
        metrics.RecordFrame();
        Thread.Sleep(16);  // 62.5 FPS
        metrics.RecordFrame();
        Thread.Sleep(33);  // 30 FPS
        metrics.RecordFrame();
        Thread.Sleep(16);  // 62.5 FPS
        metrics.RecordFrame();
        Thread.Sleep(8);   // 125 FPS
        metrics.RecordFrame();

        // Assert
        Assert.Equal(6, metrics.FrameCount);
        Assert.True(metrics.MaxFPS > metrics.MinFPS);
        Assert.True(metrics.MinFPS > 0);
        Assert.InRange(metrics.AverageFrameTime, 5, 35);
    }
}
