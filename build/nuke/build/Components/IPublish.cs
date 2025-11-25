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
        .DependsOn<IRestore>()
        .AssuredAfterFailure()
        .Executes(() =>
        {
            // Ensure build-logs directory exists for this version before writing logs
            Directory.CreateDirectory(BuildLogsDirectory);

            var buildConfig = this as IBuildConfig;
            var configuredProjects = buildConfig?.Config?.PublishProjectPaths;

            if (configuredProjects != null && configuredProjects.Count > 0)
            {
                foreach (var relativePath in configuredProjects)
                {
                    var projectPath = RootDirectory / relativePath;
                    var projectName = Path.GetFileNameWithoutExtension(projectPath);

                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
                    var metaFile = Path.Combine((string)BuildLogsDirectory, $"publish-players-{timestamp}.log");
                    File.AppendAllText(metaFile,
                        $"PublishPlayers run at {DateTime.UtcNow:o}{Environment.NewLine}" +
                        $"Version: {ArtifactsVersion}{Environment.NewLine}" +
                        $"Runtime: {Runtime}{Environment.NewLine}" +
                        $"Configuration: {Configuration}{Environment.NewLine}" +
                        $"Project: {projectName}{Environment.NewLine}");

                    try
                    {
                        DotNetPublish(s => s
                            .SetProject(projectPath)
                            .SetConfiguration(Configuration)
                            .SetOutput(PublishDirectory / projectName)
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
            }
            else
            {
                var projects = Solution.AllProjects
                    .Where(p => p.GetProperty<string>("OutputType") == "Exe")
                    .ToList();

                foreach (var project in projects)
                {
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
            }

            // Copy ALL built plugins from their source bin directories to the shared plugins folder
            // This discovers plugins from app-essential, game-essential, and project-specific plugins
            var pluginProjects = Solution.AllProjects
                .Where(p => p.Name.Contains("Plugin") && !p.Name.Contains("Test"))
                .ToList();

            foreach (var pluginProject in pluginProjects)
            {
                var targetFramework = pluginProject.GetProperty<string>("TargetFramework") ?? "net9.0";
                var pluginBinDir = pluginProject.Directory / "bin" / Configuration / targetFramework;

                // Check if plugin.json exists to confirm this is a valid plugin output
                var pluginJsonPath = pluginBinDir / "plugin.json";
                if (!File.Exists(pluginJsonPath))
                    continue;

                // Read plugin.json to get the plugin ID for the destination directory name
                var pluginJson = System.Text.Json.JsonDocument.Parse(File.ReadAllText(pluginJsonPath));
                var pluginId = pluginJson.RootElement.GetProperty("id").GetString();

                if (string.IsNullOrEmpty(pluginId))
                    continue;

                // Copy plugin to shared plugins directory using plugin ID as directory name
                var destPluginDir = PublishDirectory / "plugins" / pluginId;
                CopyDirectoryRecursively(pluginBinDir, destPluginDir);
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
