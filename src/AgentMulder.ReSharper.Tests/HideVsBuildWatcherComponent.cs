using AgentMulder.ReSharper.Plugin.Components;
using JetBrains.Application.Components;
using JetBrains.ProjectModel;

namespace AgentMulder.ReSharper.Tests
{
    [SolutionComponent]
    public class HideVsBuildWatcherComponent : IHideImplementation<VsBuildWatcher>
    {
    }
}