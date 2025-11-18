using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using Nuke.Common.Tools.GitVersion;
using Serilog;
using static Nuke.Common.EnvironmentInfo;

class Build : NukeBuild,
    IBuildConfig,
    IClean,
    IRestore,
    ICompile,
    ITest,
    IPublish
{
    [GitVersion(NoFetch = true)]
    readonly GitVersion GitVersion;

    public string GitVersionNuGet
    {
        get
        {
            var value = GitVersion?.SemVer ?? "0.0.0-local";
            Log.Information("GitVersion SemVer resolved to {Version}", value);
            return value;
        }
    }

    [Parameter("Player to run from artifacts (console or windows)")]
    readonly string Player = "console";

    [Parameter("Arguments to pass to the player executable")]
    readonly string PlayerArgs;

    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    Target Task => _ => _
        .Description("Build and publish players, then run the selected player from versioned artifacts")
        .DependsOn(((IPublish)this).PublishPlayers)
        .Executes(() =>
        {
            var playerName = Player.Equals("windows", StringComparison.OrdinalIgnoreCase)
                ? "PigeonPea.Windows"
                : "PigeonPea.Console";

            var publishDir = ((IPublish)this).PublishDirectory;
            var playerDir = publishDir / playerName;

            if (!Directory.Exists(playerDir))
            {
                Log.Error("Player directory not found: {PlayerDir}", playerDir);
                return;
            }

            var exeName = playerName + (IsWin ? ".exe" : string.Empty);
            var exePath = playerDir / exeName;

            if (!File.Exists(exePath))
            {
                Log.Error("Executable not found for player {Player}: {ExePath}", playerName, exePath);
                return;
            }

            var args = PlayerArgs ?? string.Empty;

            ProcessTasks.StartProcess(exePath, args, playerDir)
                .WaitForExit();
        });

    public static int Main () => Execute<Build>(x => ((ICompile)x).Compile);
}
