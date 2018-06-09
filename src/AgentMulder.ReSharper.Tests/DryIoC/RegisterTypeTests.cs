using AgentMulder.Containers.DryIoC;

namespace AgentMulder.ReSharper.Tests.DryIoC
{
    [TestWithNuGetPackage(Packages = new[] { "DryIoc/2.12.10" })]
    public class RegisterTypeTests : AgentMulderTestBase<DryIoCContainerInfo>
    {
        protected override string RelativeTestDataPath => "DryIoC";

        protected override string AdditionalSolutionFilesPath => "DryIoCContainer";
    }
}
