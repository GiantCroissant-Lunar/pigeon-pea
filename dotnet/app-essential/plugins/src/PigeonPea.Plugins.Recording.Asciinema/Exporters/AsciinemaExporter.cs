using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Plugins.Recording.Asciinema.Models;

namespace PigeonPea.Plugins.Recording.Asciinema.Exporters;

/// <summary>
/// Exports terminal frames to asciinema v2 format (.cast files).
/// </summary>
public sealed class AsciinemaExporter
{
    private readonly ILogger? _logger;

    public AsciinemaExporter(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Exports terminal frames to asciinema v2 format.
    /// </summary>
    /// <param name="frames">Collection of terminal frames to export.</param>
    /// <param name="outputPath">Output file path for the .cast file.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExportAsync(IReadOnlyList<TerminalFrame> frames, string outputPath)
    {
        if (frames == null || frames.Count == 0)
        {
            throw new ArgumentException("Frames collection cannot be null or empty", nameof(frames));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
        }

        try
        {
            // Ensure output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(outputPath, append: false, System.Text.Encoding.UTF8);

            // Write header (JSON line 1)
            await WriteHeaderAsync(writer, frames[0]);

            // Write events (JSON array per line)
            foreach (var frame in frames)
            {
                await WriteFrameAsync(writer, frame);
            }

            _logger?.LogInformation("Successfully exported {FrameCount} frames to {OutputPath}", frames.Count, outputPath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export frames to {OutputPath}", outputPath);
            throw;
        }
    }

    private async Task WriteHeaderAsync(StreamWriter writer, TerminalFrame firstFrame)
    {
        var header = new
        {
            version = 2,
            width = firstFrame.Width,
            height = firstFrame.Height,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            env = new
            {
                TERM = "xterm-256color",
                SHELL = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash"
            }
        };

        var headerJson = JsonSerializer.Serialize(header, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        await writer.WriteLineAsync(headerJson);
    }

    private async Task WriteFrameAsync(StreamWriter writer, TerminalFrame frame)
    {
        // Format: [timestamp, "o", content]
        var eventData = new object[]
        {
            Math.Round(frame.Timestamp, 3), // Round to 3 decimal places for precision
            "o", // output event type
            frame.Content
        };

        var eventJson = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        await writer.WriteLineAsync(eventJson);
    }

    /// <summary>
    /// Validates that a .cast file follows the asciinema v2 format.
    /// </summary>
    /// <param name="filePath">Path to the .cast file to validate.</param>
    /// <returns>True if the file is valid, false otherwise.</returns>
    public async Task<bool> ValidateCastFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length == 0) return false;

            // Validate header
            var header = JsonSerializer.Deserialize<JsonElement>(lines[0]);
            if (!header.TryGetProperty("version", out var versionProp) || versionProp.GetInt32() != 2)
            {
                return false;
            }

            // Validate frame format
            for (int i = 1; i < lines.Length; i++)
            {
                var frameArray = JsonSerializer.Deserialize<JsonElement>(lines[i]);
                if (frameArray.ValueKind != JsonValueKind.Array || frameArray.GetArrayLength() != 3)
                {
                    return false;
                }

                // Check timestamp
                if (frameArray[0].ValueKind != JsonValueKind.Number)
                {
                    return false;
                }

                // Check event type
                if (frameArray[1].ValueKind != JsonValueKind.String || frameArray[1].GetString() != "o")
                {
                    return false;
                }

                // Check content
                if (frameArray[2].ValueKind != JsonValueKind.String)
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to validate cast file {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Gets metadata about a .cast file.
    /// </summary>
    /// <param name="filePath">Path to the .cast file.</param>
    /// <returns>Metadata about the recording, or null if file is invalid.</returns>
    public async Task<AsciinemaMetadata?> GetMetadataAsync(string filePath)
    {
        if (!await ValidateCastFileAsync(filePath))
        {
            return null;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);

            // Parse header
            var header = JsonSerializer.Deserialize<JsonElement>(lines[0]);
            var durationSeconds = lines.Length > 1
                ? JsonSerializer.Deserialize<JsonElement>(lines[^1])[0].GetDouble()
                : 0.0;

            var metadata = new AsciinemaMetadata
            {
                Version = header.GetProperty("version").GetInt32(),
                Width = header.GetProperty("width").GetInt32(),
                Height = header.GetProperty("height").GetInt32(),
                Timestamp = header.GetProperty("timestamp").GetInt64(),
                FrameCount = lines.Length - 1,
                DurationSeconds = durationSeconds
            };

            return metadata;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to read metadata from cast file {FilePath}", filePath);
            return null;
        }
    }
}

/// <summary>
/// Metadata about an asciinema recording.
/// </summary>
public sealed class AsciinemaMetadata
{
    /// <summary>
    /// Asciinema format version (should be 2).
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Terminal width in columns.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Terminal height in rows.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Unix timestamp when recording was created.
    /// </summary>
    public long Timestamp { get; init; }

    /// <summary>
    /// Number of frames in the recording.
    /// </summary>
    public int FrameCount { get; init; }

    /// <summary>
    /// Duration of the recording in seconds.
    /// </summary>
    public double DurationSeconds { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }
}
