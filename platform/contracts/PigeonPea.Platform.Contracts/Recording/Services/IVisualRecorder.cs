using System.Threading.Tasks;

namespace PigeonPea.Platform.Contracts.Recording.Services;

/// <summary>
/// Visual recording interface for capturing UI output (Asciinema or FFmpeg).
/// </summary>
public interface IVisualRecorder
{
    /// <summary>
    /// Starts visual recording to the specified output path.
    /// </summary>
    /// <param name="outputPath">Destination path for the visual recording.</param>
    Task StartAsync(string outputPath);

    /// <summary>
    /// Stops the current visual recording session.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets whether the visual recorder is currently recording.
    /// </summary>
    bool IsRecording { get; }
}
