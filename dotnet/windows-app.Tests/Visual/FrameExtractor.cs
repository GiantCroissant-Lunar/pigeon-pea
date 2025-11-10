using System.Diagnostics;

namespace PigeonPea.Windows.Tests.Visual;

/// <summary>
/// Provides functionality to extract individual frames from video files using FFmpeg.
/// </summary>
public class FrameExtractor
{
    /// <summary>
    /// Gets or sets the frame rate for extraction (frames per second).
    /// Default is 1 FPS.
    /// </summary>
    public int FrameRate { get; set; } = 1;

    /// <summary>
    /// Gets or sets the path to the FFmpeg executable.
    /// If null, "ffmpeg" will be used (assumes it's in PATH).
    /// </summary>
    public string? FFmpegPath { get; set; }

    /// <summary>
    /// Extracts frames from a video file at the configured frame rate.
    /// </summary>
    /// <param name="videoPath">Path to the source video file.</param>
    /// <param name="outputDirectory">Directory where extracted frames will be saved.</param>
    /// <param name="outputPattern">Optional filename pattern for output files. Default is "frame-%03d.png".</param>
    /// <returns>A list of paths to the extracted frame files, ordered by frame number.</returns>
    /// <exception cref="ArgumentException">Thrown when video file doesn't exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when FFmpeg extraction fails.</exception>
    public async Task<List<string>> ExtractFrames(
        string videoPath,
        string outputDirectory,
        string? outputPattern = null)
    {
        if (!File.Exists(videoPath))
        {
            throw new ArgumentException($"Video file not found: {videoPath}", nameof(videoPath));
        }

        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputDirectory);

        // Use default pattern if not provided
        outputPattern ??= "frame-%03d.png";

        // Build output path
        var outputPath = Path.Combine(outputDirectory, outputPattern);

        // Run FFmpeg process
        var ffmpegExecutable = FFmpegPath ?? "ffmpeg";
        var processStartInfo = new ProcessStartInfo(ffmpegExecutable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        processStartInfo.ArgumentList.Add("-i");
        processStartInfo.ArgumentList.Add(videoPath);
        processStartInfo.ArgumentList.Add("-vf");
        processStartInfo.ArgumentList.Add($"fps={FrameRate}");
        processStartInfo.ArgumentList.Add(outputPath);
        processStartInfo.ArgumentList.Add("-y");

        using var process = new Process { StartInfo = processStartInfo };

        var errorOutput = new List<string>();
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorOutput.Add(e.Data);
            }
        };

        process.Start();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var errorMessage = string.Join(Environment.NewLine, errorOutput);
            throw new InvalidOperationException(
                $"FFmpeg frame extraction failed with exit code {process.ExitCode}. Error: {errorMessage}");
        }

        // Get all extracted frame files
        var frameFiles = GetExtractedFrames(outputDirectory, outputPattern);

        if (frameFiles.Count == 0)
        {
            throw new InvalidOperationException(
                "No frames were extracted. The video may be empty or the output pattern may be incorrect.");
        }

        return frameFiles;
    }

    /// <summary>
    /// Gets a list of extracted frame files from the output directory.
    /// </summary>
    /// <param name="outputDirectory">Directory containing the extracted frames.</param>
    /// <param name="outputPattern">The filename pattern used for extraction.</param>
    /// <returns>A sorted list of frame file paths.</returns>
    private List<string> GetExtractedFrames(string outputDirectory, string outputPattern)
    {
        // Convert FFmpeg pattern to file search pattern
        // e.g., "frame-%03d.png" becomes "frame-*.png"
        // Use regex to replace any sequential digit format specifier (%d, %03d, etc.) with wildcard
        var searchPattern = System.Text.RegularExpressions.Regex.Replace(outputPattern, @"%0?\d+d", "*");

        return Directory.GetFiles(outputDirectory, searchPattern)
            .OrderBy(f => f)
            .ToList();
    }

    /// <summary>
    /// Checks if FFmpeg is available on the system.
    /// </summary>
    /// <returns>True if FFmpeg is available, false otherwise.</returns>
    public async Task<bool> IsFFmpegAvailable()
    {
        try
        {
            var ffmpegExecutable = FFmpegPath ?? "ffmpeg";
            var processStartInfo = new ProcessStartInfo(ffmpegExecutable)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            processStartInfo.ArgumentList.Add("-version");

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
