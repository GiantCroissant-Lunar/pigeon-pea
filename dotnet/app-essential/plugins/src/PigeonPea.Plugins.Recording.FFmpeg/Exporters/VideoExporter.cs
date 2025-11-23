using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Recording.Models;

namespace PigeonPea.Plugins.Recording.FFmpeg.Exporters;

/// <summary>
/// Video format exporter using FFmpeg for format conversion and post-processing.
/// </summary>
public class VideoExporter
{
    private readonly ILogger<VideoExporter> _logger;

    public VideoExporter(ILogger<VideoExporter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Converts a video file to a different format.
    /// </summary>
    /// <param name="inputPath">Path to the input video file.</param>
    /// <param name="outputPath">Path for the output video file.</param>
    /// <param name="targetFormat">Target recording format.</param>
    /// <param name="quality">Quality settings (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the conversion operation.</returns>
    public async Task ConvertFormatAsync(
        string inputPath,
        string outputPath,
        RecordingFormat targetFormat,
        VideoQuality? quality = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentException("Input path cannot be null or empty", nameof(inputPath));

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Input file not found", inputPath);

        try
        {
            var args = BuildConversionArgs(inputPath, outputPath, targetFormat, quality);
            _logger.LogInformation("Converting video from {Input} to {Output} with format {Format}",
                inputPath, outputPath, targetFormat);

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start FFmpeg process for conversion");
            }

            // Log conversion progress
            var logTask = Task.Run(async () =>
            {
                try
                {
                    while (!process.WaitForExit(1000) && !cancellationToken.IsCancellationRequested)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        if (!string.IsNullOrEmpty(error))
                        {
                            _logger.LogDebug("FFmpeg conversion: {Error}", error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during conversion logging");
                }
            });

            await process.WaitForExitAsync(cancellationToken);
            await logTask;

            if (process.ExitCode != 0)
            {
                var errorOutput = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Video conversion failed with exit code {process.ExitCode}: {errorOutput}");
            }

            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException("Conversion completed but output file was not created");
            }

            _logger.LogInformation("Video conversion completed successfully: {Output}", outputPath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Video conversion was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert video from {Input} to {Output}", inputPath, outputPath);
            throw;
        }
    }

    /// <summary>
    /// Extracts audio from a video file.
    /// </summary>
    /// <param name="inputPath">Path to the input video file.</param>
    /// <param name="outputPath">Path for the output audio file.</param>
    /// <param name="audioFormat">Audio format (mp3, wav, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the extraction operation.</returns>
    public async Task ExtractAudioAsync(
        string inputPath,
        string outputPath,
        string audioFormat = "mp3",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentException("Input path cannot be null or empty", nameof(inputPath));

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Input file not found", inputPath);

        try
        {
            var args = $"-i \"{inputPath}\" -vn -acodec libmp3lame -ab 192k \"{outputPath}\"";
            if (audioFormat.ToLowerInvariant() != "mp3")
            {
                args = $"-i \"{inputPath}\" -vn -acodec pcm_s16le \"{outputPath}\"";
            }

            _logger.LogInformation("Extracting audio from {Input} to {Output} in format {Format}",
                inputPath, outputPath, audioFormat);

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start FFmpeg process for audio extraction");
            }

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var errorOutput = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Audio extraction failed with exit code {process.ExitCode}: {errorOutput}");
            }

            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException("Audio extraction completed but output file was not created");
            }

            _logger.LogInformation("Audio extraction completed successfully: {Output}", outputPath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Audio extraction was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract audio from {Input} to {Output}", inputPath, outputPath);
            throw;
        }
    }

    /// <summary>
    /// Gets video file information.
    /// </summary>
    /// <param name="filePath">Path to the video file.</param>
    /// <returns>Video information or null if file cannot be analyzed.</returns>
    public async Task<VideoInfo?> GetVideoInfoAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Video file not found", filePath);

        try
        {
            var args = $"-i \"{filePath}\" -hide_banner";

            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Failed to get video info for {File}: {Error}", filePath, output);
                return null;
            }

            return ParseVideoInfo(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video info for {File}", filePath);
            return null;
        }
    }

    private string BuildConversionArgs(string inputPath, string outputPath, RecordingFormat targetFormat, VideoQuality? quality)
    {
        var args = $"-i \"{inputPath}\"";

        // Add quality settings if specified
        if (quality.HasValue)
        {
            args += quality.Value switch
            {
                VideoQuality.Low => " -crf 35 -preset fast",
                VideoQuality.Medium => " -crf 25 -preset medium",
                VideoQuality.High => " -crf 18 -preset slow",
                VideoQuality.Ultra => " -crf 15 -preset veryslow",
                _ => ""
            };
        }

        // Add format-specific settings
        args += targetFormat switch
        {
            RecordingFormat.Mp4 => " -c:v libx264 -c:a aac -movflags +faststart",
            RecordingFormat.Webm => " -c:v libvpx-vp9 -c:a libopus",
            _ => ""
        };

        args += $" \"{outputPath}\"";
        return args;
    }

    private VideoInfo? ParseVideoInfo(string ffprobeOutput)
    {
        try
        {
            var info = new VideoInfo();

            // Parse duration
            var durationMatch = System.Text.RegularExpressions.Regex.Match(ffprobeOutput, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
            if (durationMatch.Success)
            {
                var hours = int.Parse(durationMatch.Groups[1].Value);
                var minutes = int.Parse(durationMatch.Groups[2].Value);
                var seconds = double.Parse(durationMatch.Groups[3].Value);
                info.Duration = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
            }

            // Parse video stream info
            var videoMatch = System.Text.RegularExpressions.Regex.Match(ffprobeOutput, @"Stream #\d+:\d+.*: Video: (\w+).*?(\d{3,5})x(\d{3,5})");
            if (videoMatch.Success)
            {
                info.Codec = videoMatch.Groups[1].Value;
                info.Width = int.Parse(videoMatch.Groups[2].Value);
                info.Height = int.Parse(videoMatch.Groups[3].Value);
            }

            // Parse frame rate
            var fpsMatch = System.Text.RegularExpressions.Regex.Match(ffprobeOutput, @"(\d+\.?\d*) fps");
            if (fpsMatch.Success)
            {
                info.FrameRate = double.Parse(fpsMatch.Groups[1].Value);
            }

            // Parse bitrate
            var bitrateMatch = System.Text.RegularExpressions.Regex.Match(ffprobeOutput, @"bitrate: (\d+) kb/s");
            if (bitrateMatch.Success)
            {
                info.Bitrate = int.Parse(bitrateMatch.Groups[1].Value) * 1000; // Convert to bits per second
            }

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse video info from FFprobe output");
            return null;
        }
    }
}

/// <summary>
/// Video quality settings for conversion.
/// </summary>
public enum VideoQuality
{
    Low,
    Medium,
    High,
    Ultra
}

/// <summary>
/// Information about a video file.
/// </summary>
public class VideoInfo
{
    public string? Codec { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; }
    public TimeSpan Duration { get; set; }
    public int Bitrate { get; set; }
    public long FileSize { get; set; }

    public string Resolution => $"{Width}x{Height}";
    public double AspectRatio => Width > 0 ? (double)Height / Width : 0;
}
