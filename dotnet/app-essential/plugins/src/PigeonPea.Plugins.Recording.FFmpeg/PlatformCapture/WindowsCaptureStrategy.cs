using System.Runtime.InteropServices;
using PigeonPea.Plugins.Recording.FFmpeg.Configuration;

namespace PigeonPea.Plugins.Recording.FFmpeg.PlatformCapture;

/// <summary>
/// Windows-specific screen capture strategy using gdigrab.
/// </summary>
public class WindowsCaptureStrategy : ICaptureStrategy
{
    private readonly FFmpegRecordingOptions _options;

    public WindowsCaptureStrategy(FFmpegRecordingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string BuildFFmpegArgs(string outputPath)
    {
        var args = new List<string>
        {
            "-f", "gdigrab",
            "-framerate", _options.FrameRate.ToString(),
            "-i", GetInputSource()
        };

        // Add offset for partial screen capture
        if (_options.OffsetX.HasValue && _options.OffsetY.HasValue)
        {
            args.AddRange(new[]
            {
                "-offset_x", _options.OffsetX.Value.ToString(),
                "-offset_y", _options.OffsetY.Value.ToString()
            });
        }

        // Mouse cursor visibility
        if (!_options.ShowCursor)
        {
            args.AddRange(new[] { "-draw_mouse", "0" });
        }

        // Add encoding options
        AddEncodingOptions(args);

        // Add custom arguments
        if (_options.CustomArgs?.Length > 0)
        {
            args.AddRange(_options.CustomArgs);
        }

        // Output path
        args.Add($"\"{outputPath}\"");

        return string.Join(" ", args);
    }

    public string GetCaptureMethodName()
    {
        return _options.CaptureDesktop ? "Windows Desktop (gdigrab)" : $"Windows Window: {_options.WindowTitle}";
    }

    public bool IsSupported()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public IEnumerable<string> GetRequirements()
    {
        return new[]
        {
            "FFmpeg with gdigrab support",
            "Windows operating system",
            "For window capture: Window title must be exact match"
        };
    }

    private string GetInputSource()
    {
        if (_options.CaptureDesktop)
        {
            return "desktop";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_options.WindowTitle))
            {
                throw new InvalidOperationException("WindowTitle must be specified when CaptureDesktop is false");
            }
            return $"title={_options.WindowTitle}";
        }
    }

    private void AddEncodingOptions(List<string> args)
    {
        args.AddRange(new[]
        {
            "-c:v", _options.VideoCodec,
            "-preset", _options.Preset,
            "-crf", _options.CRF.ToString(),
            "-pix_fmt", _options.PixelFormat
        });

        // Maximum duration
        if (_options.MaxDuration.HasValue)
        {
            args.AddRange(new[] { "-t", _options.MaxDuration.Value.ToString() });
        }
    }
}
