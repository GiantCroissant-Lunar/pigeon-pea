using System.Diagnostics;
using System.Runtime.InteropServices;
using PigeonPea.Plugin.Recording.FFmpeg.Configuration;

namespace PigeonPea.Plugin.Recording.FFmpeg.PlatformCapture;

/// <summary>
/// macOS-specific screen capture strategy using avfoundation.
/// </summary>
public class MacCaptureStrategy : ICaptureStrategy
{
    private readonly FFmpegRecordingOptions _options;

    public MacCaptureStrategy(FFmpegRecordingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string BuildFFmpegArgs(string outputPath)
    {
        var args = new List<string>
        {
            "-f", "avfoundation",
            "-framerate", _options.FrameRate.ToString(),
            "-i", GetInputSource()
        };

        // Mouse capture options
        if (_options.CaptureMouse)
        {
            args.AddRange(new[] { "-capture_cursor", "1" });
        }

        if (_options.CaptureClicks)
        {
            args.AddRange(new[] { "-capture_mouse_clicks", "1" });
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
        return $"macOS Screen {_options.ScreenDevice} (avfoundation)";
    }

    public bool IsSupported()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    public IEnumerable<string> GetRequirements()
    {
        return new[]
        {
            "FFmpeg with avfoundation support",
            "macOS operating system",
            "Screen recording permissions in System Preferences",
            "Microphone permissions (if recording audio)"
        };
    }

    private string GetInputSource()
    {
        var deviceIndex = _options.ScreenDevice;

        // Validate device index by checking available devices
        var availableDevices = GetAvailableDevices();
        if (!availableDevices.Contains(deviceIndex))
        {
            throw new InvalidOperationException($"Screen device {deviceIndex} is not available. Available devices: {string.Join(", ", availableDevices)}");
        }

        return deviceIndex.ToString();
    }

    private List<int> GetAvailableDevices()
    {
        var devices = new List<int>();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-f avfoundation -list_devices true -i dummy",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Parse output to find display devices
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("AVFoundation") && line.Contains("Capture screen"))
                    {
                        // Extract device index from line like: "[AVFoundation input device @ 0x7f8b1c000000] [0] Capture screen 0"
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"\[(\d+)\]");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out var index))
                        {
                            devices.Add(index);
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback to common device indices if detection fails
            devices.AddRange(new[] { 0, 1, 2 });
        }

        return devices.Any() ? devices : new List<int> { 1 }; // Default to device 1 if none found
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
