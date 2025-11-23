using PigeonPea.Plugins.Recording.FFmpeg.Configuration;

namespace PigeonPea.Plugins.Recording.FFmpeg.PlatformCapture;

/// <summary>
/// Interface for platform-specific screen capture strategies.
/// </summary>
public interface ICaptureStrategy
{
    /// <summary>
    /// Builds FFmpeg command line arguments for the current platform.
    /// </summary>
    /// <param name="outputPath">Output file path for the recording.</param>
    /// <returns>Complete FFmpeg command line arguments.</returns>
    string BuildFFmpegArgs(string outputPath);
    
    /// <summary>
    /// Gets the name of the capture method used.
    /// </summary>
    /// <returns>Human-readable capture method name.</returns>
    string GetCaptureMethodName();
    
    /// <summary>
    /// Validates that the strategy can be used on the current platform.
    /// </summary>
    /// <returns>True if the strategy is supported, false otherwise.</returns>
    bool IsSupported();
    
    /// <summary>
    /// Gets platform-specific requirements or dependencies.
    /// </summary>
    /// <returns>List of requirements for this capture method.</returns>
    IEnumerable<string> GetRequirements();
}
