using System;
using System.Collections.Generic;
using System.Linq;
using AgentMulder.ReSharper.Domain.Registrations;
using AgentMulder.ReSharper.Plugin.Components;
using JetBrains.Annotations;
using JetBrains.Application.DataContext;
using JetBrains.Application.Progress;
using JetBrains.DocumentModel.DataContext;
using JetBrains.IDE.TreeBrowser;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Feature.Services.Navigation.ExecutionHosting;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TreeModels;

namespace AgentMulder.ReSharper.Plugin.Navigation
{
    [ContextNavigationProvider]
    public class GotoRegisteredComponentsNavigationProvider : INavigateFromHereProvider
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
            var execution = GetNavigationAction(dataContext, navigationExecutionHost, patternManager, solution);

            if (execution != null)
            {
                yield return
                    new ContextNavigation("&Registered Components", "GotoRegistered", NavigationActionGroup.Blessed,
                        execution, "GotoRegisteredShort");
            }
        }

        private static Action GetNavigationAction(IDataContext dataContext,
            INavigationExecutionHost navigationExecutionHost, [NotNull] IPatternManager patternManager,
            [NotNull] ISolution solution)
        {
            var invokedNode = dataContext.GetSelectedTreeNode<IExpression>();

            var document = dataContext.GetData(DocumentModelDataConstants.DOCUMENT);

            var psiSourceFile = document?.GetPsiSourceFile(solution);
            if (psiSourceFile == null)
            {
                return null;
            }

            var registration = patternManager.GetRegistrationsForFile(psiSourceFile)
                .FirstOrDefault(r => r.Registration.RegistrationElement.Children().Contains(invokedNode));
            if (registration == null)
            {
                return null;
            }

            var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(solution, false);
            var visitor = new RegisteredComponentVisitor(registration.Registration);

            searchDomain.Accept(visitor);

            if (visitor.MatchingTypes.Count == 0)
            {
                return null;
            }

            return () =>
            {
                var occurences =
                    visitor.MatchingTypes.Where(_ => _.DeclaredElement != null)
                        .Select(_ => new DeclaredElementOccurrence(_.DeclaredElement))
                        .Cast<IOccurrence>()
                        .ToList();

                navigationExecutionHost.ShowContextPopupMenu(dataContext, occurences,
                    DescriptorBuilder(solution, occurences),
                    new OccurrencePresentationOptions(IconDisplayStyle.OccurrenceEntityType), true,
                    "Registered Components");
            };
        }

        private static Func<IOccurrenceBrowserDescriptor> DescriptorBuilder(ISolution solution, IEnumerable<IOccurrence> occurences)
        {
            return () => new RegisteredComponentDescriptor(solution, occurences);
        }

        private class RegisteredComponentVisitor : SearchDomainVisitor
        {
            private readonly IComponentRegistration registration;
            public override bool ProcessingIsFinished { get; } = false;

            public List<ITypeDeclaration> MatchingTypes { get; } = new List<ITypeDeclaration>();

            public RegisteredComponentVisitor(IComponentRegistration registration)
            {
                this.registration = registration;
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
                    if (registration.IsSatisfiedBy(typeDeclaration.DeclaredElement))
                    {
                        MatchingTypes.Add(typeDeclaration);
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
            public RegisteredComponentDescriptor([NotNull] ISolution solution, [NotNull] IEnumerable<IOccurrence> occurrences)
                : base(solution)
            {
                using (ReadLockCookie.Create())
                {
                    this.SetResults(occurrences.ToList());
                }

                var treeSimpleModel = new TreeSimpleModel();
                Model = treeSimpleModel;

                foreach (var occurrence in occurrences)
                {
                    treeSimpleModel.Insert(null, occurrence);
                }

                Title.Value = "Registered Components";
            }

            public override TreeModel Model { get; }

            protected override void SetResults(ICollection<IOccurrence> items, IProgressIndicator indicator = null, bool mergeItems = true)
            {
                base.SetResults(items, indicator, mergeItems);
                this.RequestUpdate(UpdateKind.Display, true, (IEnumerable<TreeModelNode>)null);
            }
        }
    }
}
