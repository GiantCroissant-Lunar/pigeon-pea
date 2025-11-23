using System.Diagnostics;
using System.Runtime.InteropServices;
using PigeonPea.Plugins.Recording.FFmpeg.Configuration;

namespace PigeonPea.Plugins.Recording.FFmpeg.PlatformCapture;

/// <summary>
/// Linux-specific screen capture strategy using x11grab.
/// </summary>
public class LinuxCaptureStrategy : ICaptureStrategy
{
    private readonly FFmpegRecordingOptions _options;

    public LinuxCaptureStrategy(FFmpegRecordingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string BuildFFmpegArgs(string outputPath)
    {
        var args = new List<string>
        {
            "-f", "x11grab",
            "-framerate", _options.FrameRate.ToString(),
            "-video_size", GetVideoSize(),
            "-i", GetInputSource()
        };

        // Mouse following
        if (_options.FollowMouse)
        {
            args.AddRange(new[] { "-follow_mouse", "centered" });
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
        return $"Linux X11 Display: {_options.Display} (x11grab)";
    }

    public bool IsSupported()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    public IEnumerable<string> GetRequirements()
    {
        return new[]
        {
            "FFmpeg with x11grab support",
            "Linux operating system with X11 display server",
            "X11 display server must be running",
            "Sufficient permissions to access the display"
        };
    }

    private string GetVideoSize()
    {
        if (!string.IsNullOrWhiteSpace(_options.Resolution))
        {
            return _options.Resolution;
        }

        // Try to get screen resolution from xrandr or use default
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "xrandr",
                Arguments = "--current | grep '*' | cut -d' ' -f4",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd().Trim();
                if (!string.IsNullOrEmpty(output) && output.Contains('x'))
                {
                    return output.Split('x')[0] + "x" + output.Split('x')[1].Split(' ')[0];
                }
            }
        }
        catch
        {
            // Fallback to default if xrandr fails
        }

        return "1920x1080"; // Default resolution
    }

    private string GetInputSource()
    {
        var display = _options.Display ?? ":0.0";

        // Add offset if specified
        var inputSource = display;
        if (_options.OffsetX.HasValue && _options.OffsetY.HasValue)
        {
            var resolution = GetVideoSize();
            var width = int.Parse(resolution.Split('x')[0]);
            var height = int.Parse(resolution.Split('x')[1]);

            inputSource += $"+{_options.OffsetX.Value},{_options.OffsetY.Value}";
        }

        return inputSource;
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
