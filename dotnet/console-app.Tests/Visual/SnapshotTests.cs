using System.Diagnostics;
using System.Text;
using Xunit;
using FluentAssertions;

namespace PigeonPea.Console.Tests.Visual;

/// <summary>
/// Visual regression tests that compare console output against stored snapshots.
/// </summary>
public class SnapshotTests
{
    private const string SnapshotDirectory = "snapshots";
    private const string RecordingsDirectory = "recordings";

    /// <summary>
    /// Test that runs PTY scenario for main menu and compares with snapshot.
    /// </summary>
    [Fact]
    public async Task MainMenu_MatchesSnapshot()
    {
        // Arrange
        var snapshotName = "main-menu";
        var snapshotPath = GetSnapshotPath(snapshotName);
        var recordingPath = GetRecordingPath(snapshotName);

        // Act - Run PTY scenario to capture console output
        var consoleOutput = await RunPTYScenario(
            scenario: "main-menu",
            duration: TimeSpan.FromSeconds(2),
            recordingPath: recordingPath
        );

        // Get normalized output (remove ANSI escape codes)
        var normalizedOutput = NormalizeOutput(consoleOutput);

        // Assert - Compare with snapshot
        await AssertMatchesSnapshot(normalizedOutput, snapshotPath);
    }

    /// <summary>
    /// Runs a PTY scenario and captures console output.
    /// </summary>
    /// <param name="scenario">Name of the test scenario.</param>
    /// <param name="duration">How long to run the scenario.</param>
    /// <param name="recordingPath">Path to save the asciinema recording.</param>
    /// <returns>The captured console output.</returns>
    private async Task<string> RunPTYScenario(
        string scenario,
        TimeSpan duration,
        string recordingPath)
    {
        // Ensure recordings directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(recordingPath)!);

        // For this implementation, we'll use a simple process-based approach
        // In a full implementation, this would use node-pty and asciinema
        // For now, we'll simulate the PTY scenario by running the console app directly

        var output = new StringBuilder();
        
        // Find the console-app project path
        var testProjectDir = GetTestProjectDirectory();
        var consoleAppDir = Path.GetFullPath(Path.Combine(testProjectDir, "..", "console-app"));
        var consoleAppProject = Path.Combine(consoleAppDir, "PigeonPea.Console.csproj");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{consoleAppProject}\"",
            WorkingDirectory = consoleAppDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // Set terminal environment
            Environment =
            {
                ["TERM"] = "xterm-256color",
                ["COLUMNS"] = "80",
                ["LINES"] = "24"
            }
        };

        using var process = new Process { StartInfo = startInfo };
        
        // Capture output
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();

        // Wait for the specified duration or process exit
        await Task.WhenAny(
            process.WaitForExitAsync(),
            Task.Delay(duration)
        );

        // Try to gracefully terminate the process
        if (!process.HasExited)
        {
            try
            {
                // Send 'q' to quit
                await process.StandardInput.WriteAsync('q');
                await process.StandardInput.FlushAsync();
                
                // Wait a bit for graceful exit
                await Task.WhenAny(
                    process.WaitForExitAsync(),
                    Task.Delay(TimeSpan.FromSeconds(1))
                );

                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // If we can't write to stdin or kill gracefully, force kill
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Process already exited
                }
            }
        }

        // If the process is using asciinema recording, parse it
        if (File.Exists(recordingPath))
        {
            return ParseAsciinemaRecording(recordingPath);
        }

        return output.ToString();
    }

    /// <summary>
    /// Parses an asciinema recording and extracts the accumulated content.
    /// </summary>
    /// <param name="recordingPath">Path to the asciinema recording file.</param>
    /// <returns>The accumulated content from the recording.</returns>
    private string ParseAsciinemaRecording(string recordingPath)
    {
        try
        {
            var parser = AsciinemaParser.ParseFile(recordingPath);
            
            // Get accumulated content from all frames
            var frames = parser.Frames;
            if (frames.Count == 0)
            {
                return string.Empty;
            }

            // Get the last timestamp
            var lastTimestamp = frames[frames.Count - 1].Timestamp;
            
            // Get all accumulated content
            var content = parser.GetAccumulatedContentAtTimestamp(lastTimestamp);
            
            return content;
        }
        catch
        {
            // If parsing fails, return empty string
            return string.Empty;
        }
    }

    /// <summary>
    /// Normalizes console output by removing ANSI escape codes and extra whitespace.
    /// </summary>
    /// <param name="output">Raw console output.</param>
    /// <returns>Normalized output suitable for snapshot comparison.</returns>
    private string NormalizeOutput(string output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return string.Empty;
        }

        // Create a Frame to use its ANSI removal logic
        var frame = new Frame { Content = output };
        var plainContent = frame.PlainContent;

        // Normalize line endings
        plainContent = plainContent.Replace("\r\n", "\n").Replace("\r", "\n");

        // Trim trailing whitespace from each line while preserving structure
        var lines = plainContent.Split('\n');
        var normalizedLines = lines.Select(line => line.TrimEnd()).ToArray();

        return string.Join("\n", normalizedLines).Trim();
    }

    /// <summary>
    /// Asserts that the actual output matches the snapshot.
    /// Creates the snapshot if it doesn't exist.
    /// Reports diff if there's a mismatch.
    /// </summary>
    /// <param name="actualOutput">The actual output to compare.</param>
    /// <param name="snapshotPath">Path to the snapshot file.</param>
    private async Task AssertMatchesSnapshot(string actualOutput, string snapshotPath)
    {
        // Ensure snapshot directory exists
        var snapshotDir = Path.GetDirectoryName(snapshotPath);
        if (!string.IsNullOrEmpty(snapshotDir))
        {
            Directory.CreateDirectory(snapshotDir);
        }

        if (!File.Exists(snapshotPath))
        {
            // Create new snapshot
            await File.WriteAllTextAsync(snapshotPath, actualOutput);
            
            // Inform that a new snapshot was created
            // In CI, this should fail to ensure snapshots are reviewed
            if (IsRunningInCI())
            {
                Assert.Fail($"New snapshot created at {snapshotPath}. " +
                           "Please review and commit the snapshot file.");
            }
            
            return;
        }

        // Load expected snapshot
        var expectedOutput = await File.ReadAllTextAsync(snapshotPath);
        expectedOutput = expectedOutput.Replace("\r\n", "\n").Replace("\r", "\n").Trim();

        // Compare outputs
        if (actualOutput != expectedOutput)
        {
            // Generate diff report
            var diff = GenerateDiff(expectedOutput, actualOutput);
            
            // Save the actual output for inspection
            var failurePath = snapshotPath.Replace(".txt", ".actual.txt");
            await File.WriteAllTextAsync(failurePath, actualOutput);
            
            // Fail with detailed diff
            Assert.Fail($"Visual regression detected!\n\n" +
                       $"Expected snapshot: {snapshotPath}\n" +
                       $"Actual output saved to: {failurePath}\n\n" +
                       $"Diff:\n{diff}");
        }

        // Outputs match - test passes
        actualOutput.Should().Be(expectedOutput);
    }

    /// <summary>
    /// Generates a human-readable diff between expected and actual output.
    /// </summary>
    /// <param name="expected">Expected output.</param>
    /// <param name="actual">Actual output.</param>
    /// <returns>A formatted diff string.</returns>
    private string GenerateDiff(string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');

        var diff = new StringBuilder();
        diff.AppendLine("Line-by-line comparison:");
        diff.AppendLine();

        var maxLines = Math.Max(expectedLines.Length, actualLines.Length);
        
        for (int i = 0; i < maxLines; i++)
        {
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : "<missing>";
            var actualLine = i < actualLines.Length ? actualLines[i] : "<missing>";

            if (expectedLine != actualLine)
            {
                diff.AppendLine($"Line {i + 1}:");
                diff.AppendLine($"  Expected: {expectedLine}");
                diff.AppendLine($"  Actual:   {actualLine}");
                diff.AppendLine();
            }
        }

        if (diff.Length == 0)
        {
            diff.AppendLine("Outputs differ but line-by-line comparison shows no differences.");
            diff.AppendLine("This may be due to whitespace or encoding differences.");
        }

        return diff.ToString();
    }

    /// <summary>
    /// Checks if the tests are running in a CI environment.
    /// </summary>
    /// <returns>True if running in CI, false otherwise.</returns>
    private bool IsRunningInCI()
    {
        // Check common CI environment variables
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_HOME")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TRAVIS"));
    }

    /// <summary>
    /// Gets the full path to a snapshot file.
    /// </summary>
    /// <param name="snapshotName">Name of the snapshot (without extension).</param>
    /// <returns>Full path to the snapshot file.</returns>
    private string GetSnapshotPath(string snapshotName)
    {
        // Get the test project directory
        var testProjectDir = GetTestProjectDirectory();
        return Path.Combine(testProjectDir, SnapshotDirectory, $"{snapshotName}.txt");
    }

    /// <summary>
    /// Gets the full path to a recording file.
    /// </summary>
    /// <param name="recordingName">Name of the recording (without extension).</param>
    /// <returns>Full path to the recording file.</returns>
    private string GetRecordingPath(string recordingName)
    {
        // Get the test project directory
        var testProjectDir = GetTestProjectDirectory();
        return Path.Combine(testProjectDir, RecordingsDirectory, $"{recordingName}.cast");
    }

    /// <summary>
    /// Gets the test project directory by walking up from the current directory.
    /// </summary>
    /// <returns>The test project directory path.</returns>
    private string GetTestProjectDirectory()
    {
        var currentDir = Directory.GetCurrentDirectory();
        
        // Look for the test project file
        while (!string.IsNullOrEmpty(currentDir))
        {
            var projectFile = Path.Combine(currentDir, "PigeonPea.Console.Tests.csproj");
            if (File.Exists(projectFile))
            {
                return currentDir;
            }

            var parent = Directory.GetParent(currentDir);
            currentDir = parent?.FullName ?? string.Empty;
        }

        // Fallback to current directory if we can't find the project file
        return Directory.GetCurrentDirectory();
    }
}
