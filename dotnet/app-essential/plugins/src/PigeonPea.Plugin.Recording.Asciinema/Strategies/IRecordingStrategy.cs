using System.Threading.Tasks;

namespace PigeonPea.Plugin.Recording.Asciinema.Strategies;

/// <summary>
/// Interface for different asciinema recording strategies.
/// </summary>
public interface IRecordingStrategy : IDisposable
{
    /// <summary>
    /// Gets whether the strategy is currently recording.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Starts recording to the specified output path.
    /// </summary>
    /// <param name="outputPath">Path where the recording should be saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(string outputPath);

    /// <summary>
    /// Stops the current recording session.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync();
}
