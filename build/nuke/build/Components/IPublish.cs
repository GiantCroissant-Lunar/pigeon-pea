using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

interface IPublish : ICompile
{
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PublishDirectory => ArtifactsDirectory / "publish";

    [Parameter("Runtime identifier for publishing")]
    string Runtime => TryGetValue(() => Runtime) ?? "win-x64";

    bool SelfContained => true;

    Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var projects = Solution.AllProjects
                .Where(p => p.GetProperty<string>("OutputType") == "Exe")
                .ToList();

            foreach (var project in projects)
            {
                DotNetPublish(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutput(PublishDirectory / project.Name)
                    .SetRuntime(Runtime)
                    .SetSelfContained(SelfContained)
                    .EnableNoRestore());
            }
        });
}
