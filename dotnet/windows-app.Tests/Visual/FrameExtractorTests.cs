using Xunit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PigeonPea.Windows.Tests.Visual;

/// <summary>
/// Unit tests for the <see cref="FrameExtractor"/> class.
/// </summary>
public class FrameExtractorTests : IDisposable
{
    private readonly string _testOutputDir;
    private readonly List<string> _createdFiles = new();
    private readonly List<string> _createdDirectories = new();

    public FrameExtractorTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"FrameExtractorTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);
        _createdDirectories.Add(_testOutputDir);
    }

    public void Dispose()
    {
        // Clean up test files
        foreach (var file in _createdFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                // Log cleanup errors to not hide potential issues
                Console.WriteLine($"[TEST CLEANUP] Failed to delete file {file}: {ex.Message}");
            }
        }

        // Clean up test directories
        foreach (var dir in _createdDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch (Exception ex)
            {
                // Log cleanup errors to not hide potential issues
                Console.WriteLine($"[TEST CLEANUP] Failed to delete directory {dir}: {ex.Message}");
            }
        }
    }

    [Fact]
    public void FrameRate_DefaultsToOne()
    {
        // Arrange & Act
        var extractor = new FrameExtractor();

        // Assert
        Assert.Equal(1, extractor.FrameRate);
    }

    [Fact]
    public void FrameRate_CanBeModified()
    {
        // Arrange
        var extractor = new FrameExtractor();

        // Act
        extractor.FrameRate = 5;

        // Assert
        Assert.Equal(5, extractor.FrameRate);
    }

    [Fact]
    public void FFmpegPath_DefaultsToNull()
    {
        // Arrange & Act
        var extractor = new FrameExtractor();

        // Assert
        Assert.Null(extractor.FFmpegPath);
    }

    [Fact]
    public void FFmpegPath_CanBeSet()
    {
        // Arrange
        var extractor = new FrameExtractor();
        var customPath = "/usr/local/bin/ffmpeg";

        // Act
        extractor.FFmpegPath = customPath;

        // Assert
        Assert.Equal(customPath, extractor.FFmpegPath);
    }

    [Fact]
    public async Task ExtractFrames_WithNonExistentVideo_ThrowsArgumentException()
    {
        // Arrange
        var extractor = new FrameExtractor();
        var nonExistentPath = Path.Combine(_testOutputDir, "nonexistent.mp4");
        var outputDir = Path.Combine(_testOutputDir, "frames");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => extractor.ExtractFrames(nonExistentPath, outputDir));

        Assert.Contains("Video file not found", exception.Message);
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task ExtractFrames_WithValidVideo_ExtractsFrames()
    {
        // Arrange
        var extractor = new FrameExtractor { FrameRate = 1 };
        var videoPath = CreateTestVideo();
        var outputDir = Path.Combine(_testOutputDir, "extracted_frames");
        _createdDirectories.Add(outputDir);

        // Act
        var frames = await extractor.ExtractFrames(videoPath, outputDir);

        // Assert
        Assert.NotEmpty(frames);
        Assert.All(frames, frame =>
        {
            Assert.True(File.Exists(frame), $"Frame file should exist: {frame}");
            Assert.EndsWith(".png", frame);
        });
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task ExtractFrames_CreatesOutputDirectory_WhenItDoesNotExist()
    {
        // Arrange
        var extractor = new FrameExtractor();
        var videoPath = CreateTestVideo();
        var outputDir = Path.Combine(_testOutputDir, "new_directory", "frames");
        _createdDirectories.Add(outputDir);

        // Act
        var frames = await extractor.ExtractFrames(videoPath, outputDir);

        // Assert
        Assert.True(Directory.Exists(outputDir));
        Assert.NotEmpty(frames);
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task ExtractFrames_WithCustomPattern_UsesPattern()
    {
        // Arrange
        var extractor = new FrameExtractor();
        var videoPath = CreateTestVideo();
        var outputDir = Path.Combine(_testOutputDir, "custom_pattern");
        _createdDirectories.Add(outputDir);
        var customPattern = "test-frame-%04d.png";

        // Act
        var frames = await extractor.ExtractFrames(videoPath, outputDir, customPattern);

        // Assert
        Assert.NotEmpty(frames);
        Assert.All(frames, frame =>
        {
            Assert.Contains("test-frame-", Path.GetFileName(frame));
        });
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task ExtractFrames_WithDifferentFrameRates_ExtractsCorrectNumberOfFrames()
    {
        // Arrange
        var videoPath = CreateTestVideo(duration: 3); // 3 second video
        var outputDir1 = Path.Combine(_testOutputDir, "fps1");
        var outputDir2 = Path.Combine(_testOutputDir, "fps2");
        _createdDirectories.Add(outputDir1);
        _createdDirectories.Add(outputDir2);

        var extractor1 = new FrameExtractor { FrameRate = 1 };
        var extractor2 = new FrameExtractor { FrameRate = 2 };

        // Act
        var frames1 = await extractor1.ExtractFrames(videoPath, outputDir1, "frame1-%03d.png");
        var frames2 = await extractor2.ExtractFrames(videoPath, outputDir2, "frame2-%03d.png");

        // Assert
        // For a 3s video, 1 FPS should be ~3 frames, 2 FPS should be ~6 frames.
        // Allow some tolerance for ffmpeg behavior.
        Assert.InRange(frames1.Count, 2, 4);
        Assert.InRange(frames2.Count, 5, 7);
        Assert.True(frames2.Count > frames1.Count, "2 FPS should extract more frames than 1 FPS.");
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task ExtractFrames_ReturnsFramesInOrder()
    {
        // Arrange
        var extractor = new FrameExtractor { FrameRate = 2 };
        var videoPath = CreateTestVideo();
        var outputDir = Path.Combine(_testOutputDir, "ordered_frames");
        _createdDirectories.Add(outputDir);

        // Act
        var frames = await extractor.ExtractFrames(videoPath, outputDir);

        // Assert
        Assert.NotEmpty(frames);
        // Verify frames are in alphabetical/numerical order
        var sortedFrames = frames.OrderBy(f => f).ToList();
        Assert.Equal(sortedFrames, frames);
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task ExtractFrames_ProducesPngFiles()
    {
        // Arrange
        var extractor = new FrameExtractor();
        var videoPath = CreateTestVideo();
        var outputDir = Path.Combine(_testOutputDir, "png_check");
        _createdDirectories.Add(outputDir);

        // Act
        var frames = await extractor.ExtractFrames(videoPath, outputDir);

        // Assert
        Assert.NotEmpty(frames);
        foreach (var frame in frames)
        {
            // Verify file has PNG extension
            Assert.EndsWith(".png", frame);

            // Verify file can be loaded as PNG
            using var image = Image.Load(frame);
            Assert.NotNull(image);
        }
    }

    [Fact]
    public async Task IsFFmpegAvailable_ReturnsBoolean()
    {
        // Arrange
        var extractor = new FrameExtractor();

        // Act
        var isAvailable = await extractor.IsFFmpegAvailable();

        // Assert
        // Should return either true or false, not throw
        Assert.IsType<bool>(isAvailable);
    }

    [Fact]
    public async Task IsFFmpegAvailable_WithInvalidPath_ReturnsFalse()
    {
        // Arrange
        var extractor = new FrameExtractor
        {
            FFmpegPath = "/invalid/path/to/ffmpeg"
        };

        // Act
        var isAvailable = await extractor.IsFFmpegAvailable();

        // Assert
        Assert.False(isAvailable);
    }

    /// <summary>
    /// Creates a simple test video file for testing purposes.
    /// This is a helper method that would create a real video file.
    /// For CI environments, tests using this should be skipped if FFmpeg is not available.
    /// </summary>
    private string CreateTestVideo(int duration = 2, int width = 320, int height = 240)
    {
        var videoPath = Path.Combine(_testOutputDir, $"test_video_{Guid.NewGuid()}.mp4");
        _createdFiles.Add(videoPath);

        // Create a simple test video using FFmpeg
        // This creates a test pattern video
        var processStartInfo = new System.Diagnostics.ProcessStartInfo("ffmpeg")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        processStartInfo.ArgumentList.Add("-f");
        processStartInfo.ArgumentList.Add("lavfi");
        processStartInfo.ArgumentList.Add("-i");
        processStartInfo.ArgumentList.Add($"testsrc=duration={duration}:size={width}x{height}:rate=30");
        processStartInfo.ArgumentList.Add("-pix_fmt");
        processStartInfo.ArgumentList.Add("yuv420p");
        processStartInfo.ArgumentList.Add(videoPath);
        processStartInfo.ArgumentList.Add("-y");

        var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0 || !File.Exists(videoPath))
        {
            throw new InvalidOperationException("Failed to create test video. FFmpeg may not be installed.");
        }

        return videoPath;
    }
}
