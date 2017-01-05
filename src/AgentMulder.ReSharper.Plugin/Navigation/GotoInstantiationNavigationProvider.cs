using System;
using System.Collections.Generic;
using System.Linq;
using AgentMulder.ReSharper.Plugin.Components;
using JetBrains.Annotations;
using JetBrains.Application.DataContext;
using JetBrains.Application.Progress;
using JetBrains.IDE.TreeBrowser;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Feature.Services.Navigation.ExecutionHosting;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TreeModels;

namespace AgentMulder.ReSharper.Plugin.Navigation
{
    [ContextNavigationProvider]
    public class GotoInstantiationNavigationProvider : INavigateFromHereProvider
    {
        public IEnumerable<ContextNavigation> CreateWorkflow(IDataContext dataContext)
        {
            var solution = dataContext.GetData(ProjectModelDataConstants.SOLUTION);
            if (solution == null)
            {
                throw new InvalidOperationException("Unable to get the solution");
            }

            var patternManager = solution.GetComponent<IPatternManager>();
            var typeCollector = solution.GetComponent<IRegisteredTypeCollector>();

            var navigationExecutionHost = DefaultNavigationExecutionHost.GetInstance(solution);
            var navigations = GetNavigateToRegistrationAction(dataContext, navigationExecutionHost, patternManager, solution, typeCollector);

            foreach (var navigation in navigations)
            {
                yield return navigation;
            }
        }

        private static IEnumerable<ContextNavigation> GetNavigateToRegistrationAction(IDataContext dataContext,
            INavigationExecutionHost navigationExecutionHost, [NotNull] IPatternManager patternManager,
            [NotNull] ISolution solution, IRegisteredTypeCollector typeCollector)
        {
            var parameterNode = dataContext.GetSelectedTreeNode<ICSharpParameterDeclaration>();

            if (parameterNode == null || parameterNode.Type.IsResolved == false ||
                parameterNode.Type.Classify != TypeClassification.REFERENCE_TYPE)
            {
                // must be parameter declaration, with resolved type, and a reference type
                yield break;
            }

            var constructor = ConstructorDeclarationNavigator.GetByParameterDeclaration(parameterNode);

            if (constructor == null)
            {
                yield break;
            }

            var allRegistrations = patternManager.GetAllRegistrations().ToList();

            if (allRegistrations.Count == 0)
            {
                // no registrations
                yield break;
            }

            var registeredTypes = typeCollector.GetRegisteredTypes();

            if (registeredTypes.Count == 0)
            {
                // no registrations
                yield break;
            }

            var typesMatchingParameter = registeredTypes.Where(type => MatchTypes(parameterNode, type)).ToList();

            if (!typesMatchingParameter.Any())
            {
                yield break;
            }

            yield return
                new ContextNavigation("Matching Component Registration", "GotoRegistration",
                    NavigationActionGroup.Blessed,
                    NavigateToMatchingRegistrationsAction(dataContext, navigationExecutionHost, solution,
                        typesMatchingParameter), "GotoRegistrationShort");

            yield return
                new ContextNavigation("Matching Components", "GotoMatchingRegistered", NavigationActionGroup.Blessed,
                    NaviagetToMatchingComponentsAction(dataContext, navigationExecutionHost, solution,
                        typesMatchingParameter), "GotoMatchingRegisteredShort");
        }

        private static Action NaviagetToMatchingComponentsAction(IDataContext dataContext, INavigationExecutionHost navigationExecutionHost, ISolution solution, List<Tuple<ITypeDeclaration, RegistrationInfo>> typesMatchingParameter)
        {
            return () =>
            {
                var occurences =
                    typesMatchingParameter.Distinct(_ => _.Item1)
                        .Select(_ => new DeclaredElementOccurrence(_.Item1.DeclaredElement))
                        .Cast<IOccurrence>()
                        .ToList();

                navigationExecutionHost.ShowContextPopupMenu(dataContext, occurences,
                    DescriptorBuilderForRegistrations(solution, occurences, "Matching Components"),
                    new OccurrencePresentationOptions(IconDisplayStyle.OccurrenceEntityType), true,
                    "Matching Components");
            };
        }

        private static Action NavigateToMatchingRegistrationsAction(IDataContext dataContext,
            INavigationExecutionHost navigationExecutionHost, ISolution solution, List<Tuple<ITypeDeclaration, RegistrationInfo>> typesMatchingParameter)
        {
            return () =>
            {
                var occurences =
                    typesMatchingParameter.Distinct(_ => _.Item2)
                        .Select(
                            _ =>
                                new ReferenceOccurrence(_.Item2.Registration.RegistrationElement,
                                    OccurrenceType.Occurrence))
                        .Cast<IOccurrence>()
                        .ToList();

                navigationExecutionHost.ShowContextPopupMenu(dataContext, occurences,
                    DescriptorBuilderForRegistrations(solution, occurences, "Matching Component Registrations"),
                    new OccurrencePresentationOptions(IconDisplayStyle.OccurrenceEntityType), true,
                    "Matching Component Registrations");
            };
        }

        private static bool MatchTypes(ICSharpParameterDeclaration parameterNode, Tuple<ITypeDeclaration, RegistrationInfo> _)
        {
            return (parameterNode.Type.IsClassType() && _.Item1.DeclaredElement.Equals(parameterNode.Type.GetTypeElement())) ||
                   _.Item1.DeclaredElement.GetAllSuperTypes().Contains(parameterNode.Type);
        }

        private static Func<IOccurrenceBrowserDescriptor> DescriptorBuilderForRegistrations(ISolution solution, IList<IOccurrence> occurences, string title)
        {
            return () => new RegisteredComponentDescriptor(solution, occurences, title);
        }

        private class RegisteredComponentDescriptor : OccurrenceBrowserDescriptor
        {
            public RegisteredComponentDescriptor([NotNull] ISolution solution, [NotNull] IList<IOccurrence> occurrences, string title)
                : base(solution)
            {
                using (ReadLockCookie.Create())
                {
                    this.SetResults(occurrences.ToList());
                }

                var treeSimpleModel = new TreeSimpleModel();
                Model = treeSimpleModel;

                foreach (var occurrenceGroup in occurrences.Cast<ReferenceOccurrence>().GroupBy(_ => _.Target))
                {
                    var parent = new DeclaredElementOccurrence(occurrenceGroup.Key);
                    treeSimpleModel.Insert(null, parent);
                    foreach (var occurence in occurrenceGroup)
                    {
                        treeSimpleModel.Insert(parent, occurence);
                    }
                }

                Title.Value = title;
            }

            public override TreeModel Model { get; }

            protected override void SetResults(ICollection<IOccurrence> items, IProgressIndicator indicator = null, bool mergeItems = true)
            {
                base.SetResults(items, indicator, mergeItems);
                RequestUpdate(UpdateKind.Display, true);
            }
        }
    }
}