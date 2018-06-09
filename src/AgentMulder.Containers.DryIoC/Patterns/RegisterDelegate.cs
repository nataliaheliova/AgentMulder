using System.Collections.Generic;
using System.ComponentModel.Composition;
using AgentMulder.ReSharper.Domain.Patterns;
using AgentMulder.ReSharper.Domain.Registrations;
using JetBrains.ReSharper.Feature.Services.CSharp.StructuralSearch;
using JetBrains.ReSharper.Feature.Services.CSharp.StructuralSearch.Placeholders;
using JetBrains.ReSharper.Feature.Services.StructuralSearch;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace AgentMulder.Containers.DryIoC.Patterns
{
    [Export("ComponentRegistration", typeof(IRegistrationPattern))]
    public class RegisterDelegate : DryIoCPatternBase
    {
        private static readonly IStructuralSearchPattern pattern =
            new CSharpStructuralSearchPattern("$container$.RegisterDelegate($arguments$)",
                new ExpressionPlaceholder("container", "DryIoc.Container"),
                new ArgumentPlaceholder("arguments", minimalOccurrences: 1));

        public RegisterDelegate() : base(pattern)
        {

        }

        public override IEnumerable<IComponentRegistration> GetComponentRegistrations(ITreeNode registrationRootElement)
        {
            var match = this.Match(registrationRootElement);

            if (!match.Matched)
            {
                yield break;
            }

            var invocationExpression = match.MatchedElement as IInvocationExpression;
            if (invocationExpression == null)
            {
                yield break;
            }

            if (invocationExpression.Arguments.Count == 0)
            {
                yield break;
            }

            if (invocationExpression.TypeArguments.Count == 0)
            {
                if (invocationExpression.ArgumentList.Arguments[0].Expression is ILambdaExpression)
                {
                    // factory method
                    var implementationTypes = GetArgumentTypes(invocationExpression, 0);

                    foreach (var implementationType in implementationTypes)
                    {
                        var registration =  CreateRegistration(invocationExpression, implementationType, implementationType);
                        if (registration != null)
                        {
                            yield return registration;
                        }
                    }
                }
                else if (invocationExpression.ArgumentList.Arguments.Count >= 2)
                {
                    var serviceTypes = GetArgumentTypes(invocationExpression, 0);
                    var implementationTypes = GetArgumentTypes(invocationExpression, 1);

                    foreach (var serviceType in serviceTypes)
                    {
                        foreach (var implementationType in implementationTypes)
                        {
                            var registration = CreateRegistration(invocationExpression, serviceType, implementationType);
                            if (registration != null)
                            {
                                yield return registration;
                            }
                        }
                    }
                }
            }
            else if (invocationExpression.TypeArguments.Count == 1)
            {
                var implementationTypes = GetArgumentTypes(invocationExpression, 0);
                var serviceType = invocationExpression.TypeArguments[0] as IDeclaredType;

                foreach (var implementationType in implementationTypes)
                {
                    var registration = CreateRegistration(invocationExpression, serviceType, implementationType);
                    if (registration != null)
                    {
                        yield return registration;
                    }
                }
            }
        }
    }
}