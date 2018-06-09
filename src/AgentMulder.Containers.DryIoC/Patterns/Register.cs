using System.ComponentModel.Composition;
using AgentMulder.ReSharper.Domain.Patterns;
using JetBrains.ReSharper.Feature.Services.CSharp.StructuralSearch;
using JetBrains.ReSharper.Feature.Services.CSharp.StructuralSearch.Placeholders;
using JetBrains.ReSharper.Feature.Services.StructuralSearch;

namespace AgentMulder.Containers.DryIoC.Patterns
{
    [Export("ComponentRegistration", typeof(IRegistrationPattern))]
    public class Register : DryIoCPatternBase
    {
        private static readonly IStructuralSearchPattern pattern =
            new CSharpStructuralSearchPattern("$container$.Register($arguments$)",
                new ExpressionPlaceholder("container", "DryIoc.Container"),
                new ArgumentPlaceholder("arguments"));

        public Register() : base(pattern)
        {
        }
    }
}
