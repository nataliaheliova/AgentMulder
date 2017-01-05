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
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
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
            var navigationExecutionHost = DefaultNavigationExecutionHost.GetInstance(solution);
            var executions = GetNavigateToRegistrationAction(dataContext, navigationExecutionHost, patternManager, solution);

            var i = 0;
            foreach (var action in executions)
            {
                switch (i)
                {
                    case 0:
                        yield return new ContextNavigation("Matching Component Registration", "GotoRegistration",
                            NavigationActionGroup.Blessed,
                            action, "GotoRegistrationShort");
                        break;
                    case 1:
                        yield return new ContextNavigation("Matching Components", "GotoMatchingRegistered",
                            NavigationActionGroup.Blessed,
                            action, "GotoMatchingRegisteredShort");
                        break;
                }
                  
                ++i;
            }
        }

        private static IEnumerable<Action> GetNavigateToRegistrationAction(IDataContext dataContext,
            INavigationExecutionHost navigationExecutionHost, [NotNull] IPatternManager patternManager,
            [NotNull] ISolution solution)
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

            var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(solution, false);
            var visitor = new RegisteredTypeCollector(allRegistrations);

            searchDomain.Accept(visitor);

            if (visitor.MatchingTypes.Count == 0)
            {
                // no registrations
                yield break;
            }

            var typesMatchingParameter = visitor.MatchingTypes.Where(type => MatchTypes(parameterNode, type)).ToList();

            yield return () =>
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

            yield return () =>
            {
                var occurences =
                    typesMatchingParameter.Distinct(_ => _.Item1)
                        .Select(
                            _ =>
                                new DeclaredElementOccurrence(_.Item1.DeclaredElement))
                        .Cast<IOccurrence>()
                        .ToList();

                navigationExecutionHost.ShowContextPopupMenu(dataContext, occurences,
                    DescriptorBuilderForRegistrations(solution, occurences, "Matching Components"),
                    new OccurrencePresentationOptions(IconDisplayStyle.OccurrenceEntityType), true,
                    "Matching Components");
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

        private class RegisteredTypeCollector : SearchDomainVisitor
        {
            private readonly List<RegistrationInfo> registrations;
            public override bool ProcessingIsFinished { get; } = false;

            public List<Tuple<ITypeDeclaration, RegistrationInfo>> MatchingTypes { get; } = new List<Tuple<ITypeDeclaration, RegistrationInfo>>();

            public RegisteredTypeCollector(IEnumerable<RegistrationInfo> registrations)
            {
                this.registrations = registrations.ToList();
            }

            public override void VisitElement(ITreeNode element)
            {
                var typeDeclaration = element as ITypeDeclaration;

                if (typeDeclaration == null)
                {
                    foreach (var treeNode in element.Children())
                    {
                        VisitElement(treeNode);
                    }
                }
                else
                {
                    var matchingRegistration =
                        registrations.FirstOrDefault(_ => _.Registration.IsSatisfiedBy(typeDeclaration.DeclaredElement));
                    if (matchingRegistration != null)
                    {
                        MatchingTypes.Add(new Tuple<ITypeDeclaration, RegistrationInfo>(typeDeclaration, matchingRegistration));
                    }
                }
            }

            public override void VisitPsiSourceFile(IPsiSourceFile sourceFile)
            {
                base.VisitPsiSourceFile(sourceFile);
                foreach (var psiFile in sourceFile.GetPsiServices().Files.GetPsiFiles(sourceFile))
                {
                    VisitElement(psiFile);
                }
            }
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