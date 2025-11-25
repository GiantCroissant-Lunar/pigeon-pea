using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PigeonPea.Plugin.Recording.Asciinema.Strategies;

/// <summary>
/// Recording strategy that uses the native asciinema binary for recording.
/// Preferred strategy for Linux/macOS systems where asciinema is available.
/// </summary>
public sealed class AsciinemaBinaryRecorder : IRecordingStrategy
{
    private readonly ILogger _logger;
    private Process? _process;
    private bool _isRecording;

    public bool IsRecording => _isRecording;

    public AsciinemaBinaryRecorder(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if the asciinema binary is available on the system.
    /// </summary>
    /// <returns>True if asciinema is available and functional.</returns>
    public static bool IsAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "asciinema",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return false;

            process.WaitForExit(TimeSpan.FromSeconds(5).Milliseconds);
            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task StartAsync(string outputPath)
    {
        if (_isRecording)
        {
            _logger.LogWarning("Asciinema binary recorder is already recording");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "asciinema",
                Arguments = $"rec \"{outputPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            _process = Process.Start(psi);
            if (_process == null)
            {
                throw new InvalidOperationException("Failed to start asciinema process");
            }

            // Give the process a moment to start
            await Task.Delay(100);

            if (_process.HasExited)
            {
                var error = await _process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Asciinema process exited immediately: {error}");
            }

            _isRecording = true;
            _logger.LogInformation("Started asciinema binary recording to {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start asciinema binary recording");
            _process?.Dispose();
            _process = null;
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!_isRecording || _process == null)
        {
            _logger.LogWarning("Asciinema binary recorder is not recording");
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                // Send Ctrl+C to gracefully stop asciinema
                _process.StandardInput.Write("\x03");

                // Wait a bit for graceful shutdown
                await Task.Delay(500);

                if (!_process.HasExited)
                {
                    // Force kill if it didn't stop gracefully
                    _process.Kill(entireProcessTree: true);
                }

                await _process.WaitForExitAsync();
            }

            _logger.LogInformation("Stopped asciinema binary recording");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping asciinema binary recording");
            throw;
        }
        finally
        {
            _process?.Dispose();
            _process = null;
            _isRecording = false;
        }
    }

    public void Dispose()
    {
        if (_isRecording)
        {
            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing asciinema binary recorder");
            }
        }

        _process?.Dispose();
    }
}
