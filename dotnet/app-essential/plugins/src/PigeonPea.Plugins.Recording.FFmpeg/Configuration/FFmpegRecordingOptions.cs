using System.Runtime.InteropServices;

namespace PigeonPea.Plugins.Recording.FFmpeg.Configuration;

/// <summary>
/// Configuration options for FFmpeg video recording.
/// </summary>
public class FFmpegRecordingOptions
{
    // Capture settings
    /// <summary>
    /// Frame rate for recording (frames per second).
    /// </summary>
    public int FrameRate { get; set; } = 30;
    
    /// <summary>
    /// Video resolution (e.g., "1920x1080").
    /// </summary>
    public string? Resolution { get; set; }
    
    /// <summary>
    /// Whether to show the mouse cursor in the recording.
    /// </summary>
    public bool ShowCursor { get; set; } = true;
    
    // Windows-specific
    /// <summary>
    /// Whether to capture the entire desktop (Windows only).
    /// </summary>
    public bool CaptureDesktop { get; set; } = true;
    
    /// <summary>
    /// Window title to capture (Windows only, when CaptureDesktop is false).
    /// </summary>
    public string? WindowTitle { get; set; }
    
    /// <summary>
    /// X offset for capture area (Windows only).
    /// </summary>
    public int? OffsetX { get; set; }
    
    /// <summary>
    /// Y offset for capture area (Windows only).
    /// </summary>
    public int? OffsetY { get; set; }
    
    // Linux-specific
    /// <summary>
    /// X11 display to capture (Linux only).
    /// </summary>
    public string? Display { get; set; } = ":0.0";
    
    /// <summary>
    /// Whether to follow the mouse cursor (Linux only).
    /// </summary>
    public bool FollowMouse { get; set; } = false;
    
    // macOS-specific
    /// <summary>
    /// Screen device index to capture (macOS only).
    /// </summary>
    public int ScreenDevice { get; set; } = 1;
    
    /// <summary>
    /// Whether to capture the mouse (macOS only).
    /// </summary>
    public bool CaptureMouse { get; set; } = true;
    
    /// <summary>
    /// Whether to capture mouse clicks (macOS only).
    /// </summary>
    public bool CaptureClicks { get; set; } = false;
    
    // Encoding settings
    /// <summary>
    /// Video codec to use for encoding.
    /// </summary>
    public string VideoCodec { get; set; } = "libx264";
    
    /// <summary>
    /// Encoding preset (ultrafast, fast, medium, slow).
    /// </summary>
    public string Preset { get; set; } = "medium";
    
    /// <summary>
    /// Constant Rate Factor (0-51, lower = better quality).
    /// </summary>
    public int CRF { get; set; } = 23;
    
    /// <summary>
    /// Pixel format for output.
    /// </summary>
    public string PixelFormat { get; set; } = "yuv420p";
    
    // Output settings
    /// <summary>
    /// Maximum recording duration in seconds.
    /// </summary>
    public int? MaxDuration { get; set; }
    
    /// <summary>
    /// Whether to show FFmpeg output in console.
    /// </summary>
    public bool ShowFFmpegOutput { get; set; } = false;
    
    /// <summary>
    /// Custom FFmpeg arguments to append.
    /// </summary>
    public string[]? CustomArgs { get; set; }
    
    /// <summary>
    /// Gets platform-specific default options.
    /// </summary>
    /// <returns>Default options optimized for the current platform.</returns>
    public static FFmpegRecordingOptions GetPlatformDefaults()
    {
        var options = new FFmpegRecordingOptions();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows defaults
            options.CaptureDesktop = true;
            options.ShowCursor = true;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux defaults
            options.Display = ":0.0";
            options.FollowMouse = false;
            options.Resolution = "1920x1080";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS defaults
            options.ScreenDevice = 1;
            options.CaptureMouse = true;
            options.CaptureClicks = false;
        }
        
        return options;
    }
    
    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <returns>True if options are valid, false otherwise.</returns>
    public bool IsValid()
    {
        // Validate frame rate
        if (FrameRate <= 0 || FrameRate > 120)
            return false;
        
        // Validate CRF range
        if (CRF < 0 || CRF > 51)
            return false;
        
        // Validate preset
        var validPresets = new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow" };
        if (!validPresets.Contains(Preset))
            return false;
        
        // Validate platform-specific options
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (!CaptureDesktop && string.IsNullOrWhiteSpace(WindowTitle))
                return false;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (string.IsNullOrWhiteSpace(Display))
                return false;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
        if (ScreenDevice < 0)
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets a description of the current configuration.
    /// </summary>
    /// <returns>Human-readable configuration description.</returns>
    public string GetDescription()
    {
        var description = $"FFmpeg Recording: {FrameRate}fps, {VideoCodec} (preset: {Preset}, CRF: {CRF})";
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            description += CaptureDesktop ? " [Desktop]" : $" [Window: {WindowTitle}]";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            description += $" [Display: {Display}]";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            description += $" [Screen: {ScreenDevice}]";
        }
        
        return description;
    }
}
