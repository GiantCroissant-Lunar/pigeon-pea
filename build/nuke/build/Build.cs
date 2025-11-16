using System;
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

    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => ((ICompile)x).Compile);
}
