using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PigeonPea.Plugins.Recording.FFmpeg;
using PigeonPea.Plugins.Recording.FFmpeg.Configuration;
using PigeonPea.Plugins.Recording.FFmpeg.PlatformCapture;

namespace PigeonPea.Plugins.Recording.FFmpeg.Tests;

[TestClass]
public class FFmpegRecordingTests
{
    private Mock<ILogger<FFmpegRecordingService>> _mockLogger;
    private FFmpegRecordingOptions _testOptions;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<FFmpegRecordingService>>();
        _testOptions = FFmpegRecordingOptions.GetPlatformDefaults();
    }

    [TestMethod]
    public void Constructor_ValidOptions_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var service = new FFmpegRecordingService(_mockLogger.Object, _testOptions);

        // Assert
        Assert.IsNotNull(service);
        Assert.IsFalse(service.IsRecording);
    }

    [TestMethod]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new FFmpegRecordingService(null!, _testOptions));
    }

    [TestMethod]
    public void Constructor_InvalidOptions_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidOptions = new FFmpegRecordingOptions
        {
            FrameRate = -1, // Invalid frame rate
            CRF = 100 // Invalid CRF
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new FFmpegRecordingService(_mockLogger.Object, invalidOptions));
    }

    [TestMethod]
    public void GetStrategyInfo_ShouldReturnPlatformSpecificInfo()
    {
        // Arrange
        var service = new FFmpegRecordingService(_mockLogger.Object, _testOptions);

        // Act
        var strategyInfo = service.GetStrategyInfo();

        // Assert
        Assert.IsNotNull(strategyInfo);
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.IsTrue(strategyInfo.Contains("Windows"));
            Assert.IsTrue(strategyInfo.Contains("gdigrab"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.IsTrue(strategyInfo.Contains("Linux"));
            Assert.IsTrue(strategyInfo.Contains("x11grab"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.IsTrue(strategyInfo.Contains("macOS"));
            Assert.IsTrue(strategyInfo.Contains("avfoundation"));
        }
    }

    [TestMethod]
    public void GetRequirements_ShouldReturnNonEmptyList()
    {
        // Arrange
        var service = new FFmpegRecordingService(_mockLogger.Object, _testOptions);

        // Act
        var requirements = service.GetRequirements();

        // Assert
        Assert.IsNotNull(requirements);
        Assert.IsTrue(requirements.Any());
        Assert.IsTrue(requirements.Any(r => r.Contains("FFmpeg")));
    }

    [TestMethod]
    public void StartAsync_NullOutputPath_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new FFmpegRecordingService(_mockLogger.Object, _testOptions);

        // Act & Assert
        Assert.ThrowsExceptionAsync<ArgumentException>(async () => 
            await service.StartAsync(null!));
    }

    [TestMethod]
    public void StartAsync_EmptyOutputPath_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new FFmpegRecordingService(_mockLogger.Object, _testOptions);

        // Act & Assert
        Assert.ThrowsExceptionAsync<ArgumentException>(async () => 
            await service.StartAsync(""));
    }

    [TestMethod]
    public void StartStopAsync_ValidSequence_ShouldUpdateIsRecording()
    {
        // Arrange
        if (!FFmpegRecordingService.IsFFmpegAvailable())
        {
            Assert.Inconclusive("FFmpeg is not available on this system");
            return;
        }

        var tempFile = Path.GetTempFileName();
        try
        {
            var service = new FFmpegRecordingService(_mockLogger.Object, _testOptions);

            // Act - Start
            var startTask = service.StartAsync(tempFile);
            Thread.Sleep(100); // Give time for process to start
            Assert.IsTrue(service.IsRecording);

            // Act - Stop
            var stopTask = service.StopAsync();
            Task.WaitAll(new[] { startTask, stopTask }, TimeSpan.FromSeconds(10));
            
            // Assert
            Assert.IsFalse(service.IsRecording);
            Assert.IsTrue(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [TestMethod]
    public void StartAsync_AlreadyRecording_ShouldThrowInvalidOperationException()
    {
        // Arrange
        if (!FFmpegRecordingService.IsFFmpegAvailable())
        {
            Assert.Inconclusive("FFmpeg is not available on this system");
            return;
        }

        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        
        try
        {
            var service = new FFmpegRecordingService(_mockLogger.Object, _testOptions);
            
            // Act - Start first recording
            var startTask1 = service.StartAsync(tempFile1);
            Thread.Sleep(100);
            Assert.IsTrue(service.IsRecording);

            // Act & Assert - Try to start second recording
            Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
                await service.StartAsync(tempFile2));

            // Cleanup
            service.StopAsync().Wait(TimeSpan.FromSeconds(5));
        }
        finally
        {
            foreach (var file in new[] { tempFile1, tempFile2 })
            {
                if (File.Exists(file))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }

    [TestMethod]
    public async Task StopAsync_NotRecording_ShouldNotThrow()
    {
        // Arrange
        var service = new FFmpegRecordingService(_mockLogger.Object, _testOptions);

        // Act & Assert
        await service.StopAsync();
        Assert.IsFalse(service.IsRecording);
    }

    [TestMethod]
    public void Dispose_WhileRecording_ShouldStopRecording()
    {
        // Arrange
        if (!FFmpegRecordingService.IsFFmpegAvailable())
        {
            Assert.Inconclusive("FFmpeg is not available on this system");
            return;
        }

        var tempFile = Path.GetTempFileName();
        try
        {
            var service = new FFmpegRecordingService(_mockLogger.Object, _testOptions);
            var startTask = service.StartAsync(tempFile);
            Thread.Sleep(100);
            Assert.IsTrue(service.IsRecording);

            // Act
            service.Dispose();

            // Assert
            Assert.IsFalse(service.IsRecording);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}

[TestClass]
public class FFmpegRecordingOptionsTests
{
    [TestMethod]
    public void GetPlatformDefaults_ShouldReturnValidOptions()
    {
        // Act
        var options = FFmpegRecordingOptions.GetPlatformDefaults();

        // Assert
        Assert.IsNotNull(options);
        Assert.IsTrue(options.IsValid());
        Assert.IsTrue(options.FrameRate > 0);
        Assert.IsTrue(options.CRF >= 0 && options.CRF <= 51);
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.VideoCodec));
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.Preset));
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.PixelFormat));
    }

    [TestMethod]
    public void IsValid_ValidOptions_ShouldReturnTrue()
    {
        // Arrange
        var options = new FFmpegRecordingOptions
        {
            FrameRate = 30,
            CRF = 23,
            VideoCodec = "libx264",
            Preset = "medium",
            PixelFormat = "yuv420p"
        };

        // Act & Assert
        Assert.IsTrue(options.IsValid());
    }

    [TestMethod]
    public void IsValid_InvalidFrameRate_ShouldReturnFalse()
    {
        // Arrange
        var options = new FFmpegRecordingOptions
        {
            FrameRate = -1
        };

        // Act & Assert
        Assert.IsFalse(options.IsValid());
    }

    [TestMethod]
    public void IsValid_InvalidCRF_ShouldReturnFalse()
    {
        // Arrange
        var options = new FFmpegRecordingOptions
        {
            CRF = 100 // Out of valid range (0-51)
        };

        // Act & Assert
        Assert.IsFalse(options.IsValid());
    }

    [TestMethod]
    public void IsValid_InvalidPreset_ShouldReturnFalse()
    {
        // Arrange
        var options = new FFmpegRecordingOptions
        {
            Preset = "invalid_preset"
        };

        // Act & Assert
        Assert.IsFalse(options.IsValid());
    }

    [TestMethod]
    public void GetDescription_ShouldReturnHumanReadableString()
    {
        // Arrange
        var options = new FFmpegRecordingOptions
        {
            FrameRate = 60,
            VideoCodec = "libx264",
            Preset = "fast",
            CRF = 18
        };

        // Act
        var description = options.GetDescription();

        // Assert
        Assert.IsNotNull(description);
        Assert.IsTrue(description.Contains("60fps"));
        Assert.IsTrue(description.Contains("libx264"));
        Assert.IsTrue(description.Contains("fast"));
        Assert.IsTrue(description.Contains("18"));
    }

    [TestMethod]
    public void IsFFmpegAvailable_ShouldReturnConsistentResult()
    {
        // Act
        var result1 = FFmpegRecordingService.IsFFmpegAvailable();
        var result2 = FFmpegRecordingService.IsFFmpegAvailable();

        // Assert
        Assert.AreEqual(result1, result2);
    }

    [TestMethod]
    public void GetFFmpegVersion_WhenAvailable_ShouldReturnVersionString()
    {
        // Act
        var version = FFmpegRecordingService.GetFFmpegVersion();

        // Assert
        if (FFmpegRecordingService.IsFFmpegAvailable())
        {
            Assert.IsNotNull(version);
            Assert.IsTrue(version!.Contains("ffmpeg"));
            Assert.IsTrue(version.Contains("version"));
        }
        else
        {
            Assert.IsNull(version);
        }
    }
}

[TestClass]
public class WindowsCaptureStrategyTests
{
    [TestMethod]
    public void IsSupported_OnWindows_ShouldReturnTrue()
    {
        // Arrange
        var options = new FFmpegRecordingOptions();
        var strategy = new WindowsCaptureStrategy(options);

        // Act & Assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.IsTrue(strategy.IsSupported());
        }
        else
        {
            Assert.IsFalse(strategy.IsSupported());
        }
    }

    [TestMethod]
    public void BuildFFmpegArgs_DesktopCapture_ShouldContainExpectedParameters()
    {
        // Arrange
        var options = new FFmpegRecordingOptions
        {
            CaptureDesktop = true,
            FrameRate = 30,
            VideoCodec = "libx264",
            Preset = "medium",
            CRF = 23
        };
        var strategy = new WindowsCaptureStrategy(options);

        // Act
        var args = strategy.BuildFFmpegArgs("output.mp4");

        // Assert
        Assert.IsTrue(args.Contains("-f gdigrab"));
        Assert.IsTrue(args.Contains("-framerate 30"));
        Assert.IsTrue(args.Contains("-i desktop"));
        Assert.IsTrue(args.Contains("-c:v libx264"));
        Assert.IsTrue(args.Contains("-preset medium"));
        Assert.IsTrue(args.Contains("-crf 23"));
        Assert.IsTrue(args.Contains("output.mp4"));
    }

    [TestMethod]
    public void BuildFFmpegArgs_WindowCapture_ShouldContainWindowTitle()
    {
        // Arrange
        var options = new FFmpegRecordingOptions
        {
            CaptureDesktop = false,
            WindowTitle = "Test Window",
            FrameRate = 30
        };
        var strategy = new WindowsCaptureStrategy(options);

        // Act
        var args = strategy.BuildFFmpegArgs("output.mp4");

        // Assert
        Assert.IsTrue(args.Contains("-i title=Test Window"));
    }

    [TestMethod]
    public void GetCaptureMethodName_ShouldReturnCorrectName()
    {
        // Arrange
        var options = new FFmpegRecordingOptions { CaptureDesktop = true };
        var strategy = new WindowsCaptureStrategy(options);

        // Act
        var methodName = strategy.GetCaptureMethodName();

        // Assert
        Assert.IsTrue(methodName.Contains("Windows"));
        Assert.IsTrue(methodName.Contains("gdigrab"));
    }
}
