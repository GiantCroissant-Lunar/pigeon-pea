using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

interface IPublish : ICompile
{
    // Base artifacts root: build/_artifacts
    AbsolutePath ArtifactsRoot => RootDirectory / "build" / "_artifacts";

    // Versioned subfolder using GitVersion from the Build class when available
    string ArtifactsVersion => this is Build b ? b.GitVersionNuGet : "0.0.0-local";

    // Final publish directory: build/_artifacts/{version}
    AbsolutePath PublishDirectory => ArtifactsRoot / ArtifactsVersion;

    // Per-version build logs directory: build/_artifacts/{version}/build-logs
    AbsolutePath BuildLogsDirectory => PublishDirectory / "build-logs";

    // Alias directory that always points at the latest published players
    AbsolutePath LatestDirectory => ArtifactsRoot / "latest";

    [Parameter("Runtime identifier for publishing")]
    string Runtime => TryGetValue(() => Runtime) ?? "win-x64";

    bool SelfContained => true;

    Target Publish => _ => _
        .DependsOn(Compile)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            var projects = Solution.AllProjects
                .Where(p => p.GetProperty<string>("OutputType") == "Exe")
                .ToList();

            // Ensure build-logs directory exists for this version
            Directory.CreateDirectory(BuildLogsDirectory);

            foreach (var project in projects)
            {
                // Write a minimal metadata file for this publish run
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
                var metaFile = Path.Combine((string)BuildLogsDirectory, $"publish-players-{timestamp}.log");
                File.AppendAllText(metaFile,
                    $"PublishPlayers run at {DateTime.UtcNow:o}{Environment.NewLine}" +
                    $"Version: {ArtifactsVersion}{Environment.NewLine}" +
                    $"Runtime: {Runtime}{Environment.NewLine}" +
                    $"Configuration: {Configuration}{Environment.NewLine}" +
                    $"Project: {project.Name}{Environment.NewLine}");

                try
                {
                    DotNetPublish(s => s
                        .SetProject(project)
                        .SetConfiguration(Configuration)
                        .SetOutput(PublishDirectory / project.Name)
                        .SetRuntime(Runtime)
                        .SetSelfContained(SelfContained));

                    File.AppendAllText(metaFile,
                        $"Status: Success{Environment.NewLine}{Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    File.AppendAllText(metaFile,
                        $"Status: Failed{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
                    throw;
                }
            }
        });

    /// <summary>
    /// Publish only the main game players (console and Windows apps) to the versioned artifacts folder.
    /// </summary>
    Target PublishPlayers => _ => _
        .DependsOn<IRestore>()
        .AssuredAfterFailure()
        .Executes(() =>
        {
            var playerNames = new[] { "PigeonPea.Console", "PigeonPea.Windows" };

            var projects = Solution.AllProjects
                .Where(p => playerNames.Contains(p.Name))
                .ToList();

            // Ensure build-logs directory exists for this version
            Directory.CreateDirectory(BuildLogsDirectory);

            // Reset latest alias before copying freshly published players
            LatestDirectory.CreateOrCleanDirectory();

            void CopyDirectoryRecursively(string sourceDir, string destDir)
            {
                if (!Directory.Exists(sourceDir))
                    return;

                foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourceDir, file);
                    var destinationFile = Path.Combine(destDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                    File.Copy(file, destinationFile, overwrite: true);
                }
            }

            foreach (var project in projects)
            {
                // Write a minimal metadata file for this publish run
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
                var metaFile = Path.Combine((string)BuildLogsDirectory, $"publish-players-{timestamp}.log");
                File.AppendAllText(metaFile,
                    $"PublishPlayers run at {DateTime.UtcNow:o}{Environment.NewLine}" +
                    $"Version: {ArtifactsVersion}{Environment.NewLine}" +
                    $"Runtime: {Runtime}{Environment.NewLine}" +
                    $"Configuration: {Configuration}{Environment.NewLine}" +
                    $"Project: {project.Name}{Environment.NewLine}");

                try
                {
                    DotNetPublish(s => s
                        .SetProject(project)
                        .SetConfiguration(Configuration)
                        .SetOutput(PublishDirectory / project.Name)
                        .SetRuntime(Runtime)
                        .SetSelfContained(SelfContained));

                    File.AppendAllText(metaFile,
                        $"Status: Success{Environment.NewLine}{Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    File.AppendAllText(metaFile,
                        $"Status: Failed{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
                    throw;
                }

                // Copy the published output for this player into the latest alias folder
                CopyDirectoryRecursively(
                    PublishDirectory / project.Name,
                    LatestDirectory / project.Name);

                // For the console player, also copy its dev-time plugins into a shared plugins folder
                // under the versioned artifacts root (build/_artifacts/{version}/plugins).
                if (project.Name == "PigeonPea.Console")
                {
                    var targetFramework = project.GetProperty<string>("TargetFramework") ?? "net9.0";
                    var consoleBinPlugins = project.Directory / "bin" / Configuration / targetFramework / "plugins";

                    // Shared plugins under the versioned artifacts root, e.g. build/_artifacts/{version}/plugins
                    CopyDirectoryRecursively(
                        consoleBinPlugins,
                        PublishDirectory / "plugins");
                }
            }

            // Mirror shared plugins into the latest alias so runs from build/_artifacts/latest
            // can discover plugins via "../plugins" from the player directories.
            CopyDirectoryRecursively(
                PublishDirectory / "plugins",
                LatestDirectory / "plugins");

            // Copy build logs to latest for easy debugging access
            CopyDirectoryRecursively(
                BuildLogsDirectory,
                LatestDirectory / "build-logs");
        });
}
