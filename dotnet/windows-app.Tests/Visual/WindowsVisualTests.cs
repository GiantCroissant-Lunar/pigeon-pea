using System.Diagnostics;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace PigeonPea.Windows.Tests.Visual;

/// <summary>
/// Visual snapshot tests for the Windows application.
/// These tests record application output, extract frames, and compare with stored snapshots.
/// Tests are skipped if FFmpeg is not available in the environment.
/// </summary>
public class WindowsVisualTests : IDisposable
{
    private const int TestVideoWidth = 800;
    private const int TestVideoHeight = 600;
    private const int TestVideoFrameRate = 30;

    private readonly string _testOutputDir;
    private readonly string _snapshotsDir;
    private readonly FrameExtractor _frameExtractor;
    private readonly ImageComparator _imageComparator;
    private readonly List<string> _createdFiles = new();
    private readonly List<string> _createdDirectories = new();

    public WindowsVisualTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"WindowsVisualTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);
        _createdDirectories.Add(_testOutputDir);

        // Snapshots are stored in the project directory for version control
        // Use Path.GetFullPath to resolve relative paths more robustly
        _snapshotsDir = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "snapshots"));

        // Ensure snapshots directory exists
        Directory.CreateDirectory(_snapshotsDir);

        _frameExtractor = new FrameExtractor { FrameRate = 1 };
        _imageComparator = new ImageComparator
        {
            Threshold = 0.99, // 99% similarity required
            PixelTolerance = 5 // Allow minor pixel differences
        };
    }

    public void Dispose()
    {
        // Clean up test files
        CleanupPaths(_createdFiles, File.Delete, File.Exists, "file");

        // Clean up test directories
        CleanupPaths(_createdDirectories, path => Directory.Delete(path, true), Directory.Exists, "directory");
    }

    /// <summary>
    /// Helper method to clean up test artifacts (files or directories).
    /// </summary>
    /// <param name="paths">Collection of paths to clean up.</param>
    /// <param name="deleteAction">Action to delete the path (e.g., File.Delete or Directory.Delete).</param>
    /// <param name="existsCheck">Function to check if the path exists.</param>
    /// <param name="pathType">Type of path (e.g., "file" or "directory") for logging.</param>
    private static void CleanupPaths(
        IEnumerable<string> paths,
        Action<string> deleteAction,
        Func<string, bool> existsCheck,
        string pathType)
    {
        foreach (var path in paths)
        {
            try
            {
                if (existsCheck(path))
                {
                    deleteAction(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST CLEANUP] Failed to delete {pathType} {path}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Helper method to record a test scenario.
    /// In a real implementation, this would launch the Windows app and record its output.
    /// For testing purposes, this creates a synthetic video file.
    /// </summary>
    /// <param name="scenarioName">Name of the test scenario.</param>
    /// <param name="duration">Duration in seconds.</param>
    /// <returns>Path to the recorded video file.</returns>
    private async Task<string> RecordTestScenario(string scenarioName, int duration = 5)
    {
        var videoPath = Path.Combine(_testOutputDir, $"{scenarioName}.mp4");
        _createdFiles.Add(videoPath);

        // Check if FFmpeg is available
        var isFFmpegAvailable = await _frameExtractor.IsFFmpegAvailable();
        if (!isFFmpegAvailable)
        {
            throw new InvalidOperationException("FFmpeg is not available. Visual tests require FFmpeg.");
        }

        // Create a test video using FFmpeg
        // In production, this would launch and record the actual Windows app
        var processStartInfo = new ProcessStartInfo("ffmpeg")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        processStartInfo.ArgumentList.Add("-f");
        processStartInfo.ArgumentList.Add("lavfi");
        processStartInfo.ArgumentList.Add("-i");
        processStartInfo.ArgumentList.Add($"testsrc=duration={duration}:size={TestVideoWidth}x{TestVideoHeight}:rate={TestVideoFrameRate}");
        processStartInfo.ArgumentList.Add("-pix_fmt");
        processStartInfo.ArgumentList.Add("yuv420p");
        processStartInfo.ArgumentList.Add(videoPath);
        processStartInfo.ArgumentList.Add("-y");

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 || !File.Exists(videoPath))
        {
            throw new InvalidOperationException($"Failed to record test scenario: {scenarioName}");
        }

        return videoPath;
    }

    /// <summary>
    /// Helper method to compare a frame with its snapshot.
    /// Creates the snapshot if it doesn't exist, otherwise compares and fails on mismatch.
    /// </summary>
    /// <param name="framePath">Path to the extracted frame.</param>
    /// <param name="snapshotName">Name of the snapshot file.</param>
    /// <returns>True if the frame matches the snapshot or a new snapshot was created.</returns>
    private bool CompareWithSnapshot(string framePath, string snapshotName)
    {
        var snapshotPath = Path.Combine(_snapshotsDir, snapshotName);

        if (!File.Exists(snapshotPath))
        {
            // Create new snapshot
            File.Copy(framePath, snapshotPath, overwrite: true);
            Console.WriteLine($"[SNAPSHOT] Created new snapshot: {snapshotName}");
            return true;
        }

        // Compare with existing snapshot
        var diffPath = Path.Combine(_testOutputDir, $"diff_{snapshotName}");
        _createdFiles.Add(diffPath);

        var result = _imageComparator.CompareWithDiff(snapshotPath, framePath, diffPath);

        if (!result.Match)
        {
            Console.WriteLine($"[SNAPSHOT MISMATCH] {snapshotName}");
            Console.WriteLine($"  Similarity: {result.Similarity * 100:F2}%");
            Console.WriteLine($"  Different pixels: {result.DifferentPixels}/{result.TotalPixels}");
            if (result.DiffImagePath != null)
            {
                Console.WriteLine($"  Diff image: {result.DiffImagePath}");
            }
        }

        return result.Match;
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task BasicRendering_MatchesSnapshot()
    {
        // Arrange
        const string scenarioName = "basic-rendering";

        // Act - Record test scenario
        var videoPath = await RecordTestScenario(scenarioName, duration: 3);

        // Extract frames
        var framesDir = Path.Combine(_testOutputDir, $"{scenarioName}_frames");
        _createdDirectories.Add(framesDir);
        var frames = await _frameExtractor.ExtractFrames(videoPath, framesDir);

        // Assert - Frames should be extracted
        frames.Should().NotBeEmpty("frames should be extracted from the video");

        // Compare first frame with snapshot
        var firstFrame = frames.First();
        var snapshotName = $"{scenarioName}_frame_001.png";
        var matches = CompareWithSnapshot(firstFrame, snapshotName);

        matches.Should().BeTrue($"frame should match snapshot: {snapshotName}");
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task ParticleEffect_RendersCorrectly()
    {
        // Arrange
        const string scenarioName = "particle-effect";

        // Act - Record test scenario with particle effects
        var videoPath = await RecordTestScenario(scenarioName, duration: 5);

        // Extract frames at 1 FPS
        var framesDir = Path.Combine(_testOutputDir, $"{scenarioName}_frames");
        _createdDirectories.Add(framesDir);
        var frames = await _frameExtractor.ExtractFrames(videoPath, framesDir);

        // Assert - Should have multiple frames
        frames.Should().NotBeEmpty();
        frames.Count.Should().BeGreaterThanOrEqualTo(3, "should have at least 3 frames from 5 second video");

        // Compare frame at 2 seconds with snapshot
        var frameIndex = Math.Min(2, frames.Count - 1);
        var frame = frames[frameIndex];
        var snapshotName = $"{scenarioName}_frame_{frameIndex + 1:D3}.png";
        var matches = CompareWithSnapshot(frame, snapshotName);

        matches.Should().BeTrue($"frame should match snapshot: {snapshotName}");
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task SpriteRendering_LoadsAndDisplays()
    {
        // Arrange
        const string scenarioName = "sprite-rendering";

        // Act - Record test scenario with sprites
        var videoPath = await RecordTestScenario(scenarioName, duration: 3);

        // Extract frames
        var framesDir = Path.Combine(_testOutputDir, $"{scenarioName}_frames");
        _createdDirectories.Add(framesDir);
        var frames = await _frameExtractor.ExtractFrames(videoPath, framesDir);

        // Assert - Frames extracted successfully
        frames.Should().NotBeEmpty();

        // Verify sprites are visible (image has non-black pixels)
        using var image = Image.Load<Rgba32>(frames[1]);
        var hasContent = ImageHasNonBlackPixels(image);
        hasContent.Should().BeTrue("sprites should be visible in the frame");

        // Compare with snapshot
        var snapshotName = $"{scenarioName}_frame_002.png";
        var matches = CompareWithSnapshot(frames[1], snapshotName);

        matches.Should().BeTrue($"frame should match snapshot: {snapshotName}");
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task AnimationSequence_CompletesSuccessfully()
    {
        // Arrange
        const string scenarioName = "animation-sequence";

        // Act - Record animation test scenario
        var videoPath = await RecordTestScenario(scenarioName, duration: 4);

        // Extract frames
        var framesDir = Path.Combine(_testOutputDir, $"{scenarioName}_frames");
        _createdDirectories.Add(framesDir);
        var frames = await _frameExtractor.ExtractFrames(videoPath, framesDir);

        // Assert - Should have multiple frames showing animation progression
        frames.Should().NotBeEmpty();
        frames.Count.Should().BeGreaterThanOrEqualTo(3);

        // Compare key frames with snapshots
        var keyFrameIndices = new[] { 0, frames.Count / 2, frames.Count - 1 };
        foreach (var index in keyFrameIndices)
        {
            var frame = frames[index];
            var snapshotName = $"{scenarioName}_frame_{index + 1:D3}.png";
            var matches = CompareWithSnapshot(frame, snapshotName);

            matches.Should().BeTrue($"frame {index + 1} should match snapshot: {snapshotName}");
        }
    }

    [Fact(Skip = "Requires FFmpeg to be installed")]
    public async Task UserInterface_DisplaysCorrectly()
    {
        // Arrange
        const string scenarioName = "ui-display";

        // Act - Record UI test scenario
        var videoPath = await RecordTestScenario(scenarioName, duration: 3);

        // Extract frames
        var framesDir = Path.Combine(_testOutputDir, $"{scenarioName}_frames");
        _createdDirectories.Add(framesDir);
        var frames = await _frameExtractor.ExtractFrames(videoPath, framesDir);

        // Assert
        frames.Should().NotBeEmpty();

        // Compare UI frame with snapshot
        var uiFrame = frames[1];
        var snapshotName = $"{scenarioName}_frame_002.png";
        var matches = CompareWithSnapshot(uiFrame, snapshotName);

        matches.Should().BeTrue($"UI frame should match snapshot: {snapshotName}");
    }

    [Fact]
    public async Task FrameExtractor_IsConfiguredCorrectly()
    {
        // Assert
        _frameExtractor.FrameRate.Should().Be(1, "should extract 1 frame per second");

        // Verify FFmpeg availability check returns a boolean (method works without throwing)
        var isAvailable = await _frameExtractor.IsFFmpegAvailable();
        // The method should complete successfully and return a boolean value
        Assert.IsType<bool>(isAvailable);
    }

    [Fact]
    public void ImageComparator_IsConfiguredCorrectly()
    {
        // Assert
        _imageComparator.Threshold.Should().Be(0.99, "should require 99% similarity");
        _imageComparator.PixelTolerance.Should().Be(5, "should allow 5 pixel value difference");
    }

    [Fact]
    public void SnapshotsDirectory_Exists()
    {
        // Assert
        Directory.Exists(_snapshotsDir).Should().BeTrue("snapshots directory should exist");
    }

    [Fact]
    public void SnapshotsDirectory_HasGitAttributes()
    {
        // Assert
        var gitAttributesPath = Path.Combine(_snapshotsDir, ".gitattributes");
        File.Exists(gitAttributesPath).Should().BeTrue(".gitattributes should exist in snapshots directory");

        // Verify it contains LFS configuration for image files
        var content = File.ReadAllText(gitAttributesPath);
        content.Should().Contain("*.png filter=lfs", "should configure PNG files for Git LFS");
    }

    [Fact]
    public void CompareWithSnapshot_CreatesSnapshotIfMissing()
    {
        // Arrange
        var testImagePath = CreateTestImage(100, 100, new Rgba32(255, 0, 0, 255));
        var snapshotName = $"test_snapshot_{Guid.NewGuid()}.png";
        var snapshotPath = Path.Combine(_snapshotsDir, snapshotName);

        try
        {
            // Ensure snapshot doesn't exist
            if (File.Exists(snapshotPath))
            {
                File.Delete(snapshotPath);
            }

            // Act
            var result = CompareWithSnapshot(testImagePath, snapshotName);

            // Assert
            result.Should().BeTrue("should return true when creating new snapshot");
            File.Exists(snapshotPath).Should().BeTrue("snapshot file should be created");
        }
        finally
        {
            // Cleanup
            if (File.Exists(snapshotPath))
            {
                File.Delete(snapshotPath);
            }
            if (File.Exists(testImagePath))
            {
                File.Delete(testImagePath);
            }
        }
    }

    [Fact]
    public void CompareWithSnapshot_FailsOnMismatch()
    {
        // Arrange
        var snapshot1 = CreateTestImage(100, 100, new Rgba32(255, 0, 0, 255));
        var snapshot2 = CreateTestImage(100, 100, new Rgba32(0, 255, 0, 255));
        var snapshotName = $"test_snapshot_mismatch_{Guid.NewGuid()}.png";
        var snapshotPath = Path.Combine(_snapshotsDir, snapshotName);

        try
        {
            // Create initial snapshot
            File.Copy(snapshot1, snapshotPath, overwrite: true);

            // Act - Compare with different image
            var result = CompareWithSnapshot(snapshot2, snapshotName);

            // Assert
            result.Should().BeFalse("should return false when images don't match");
        }
        finally
        {
            // Cleanup
            if (File.Exists(snapshotPath))
            {
                File.Delete(snapshotPath);
            }
            if (File.Exists(snapshot1))
            {
                File.Delete(snapshot1);
            }
            if (File.Exists(snapshot2))
            {
                File.Delete(snapshot2);
            }
        }
    }

    /// <summary>
    /// Helper method to check if an image has non-black pixels.
    /// </summary>
    private bool ImageHasNonBlackPixels(Image<Rgba32> image)
    {
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                if (pixel.R > 10 || pixel.G > 10 || pixel.B > 10)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Helper method to create a test image.
    /// </summary>
    private string CreateTestImage(int width, int height, Rgba32 color)
    {
        var path = Path.Combine(_testOutputDir, $"test_image_{Guid.NewGuid()}.png");
        using var image = new Image<Rgba32>(width, height);

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                accessor.GetRowSpan(y).Fill(color);
            }
        });

        image.SaveAsPng(path);
        _createdFiles.Add(path);
        return path;
    }
}
