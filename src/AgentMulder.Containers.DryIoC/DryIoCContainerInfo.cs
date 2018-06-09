using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using AgentMulder.ReSharper.Domain.Containers;

namespace AgentMulder.Containers.DryIoC
{
    [Export(typeof(IContainerInfo))]
    public class DryIoCContainerInfo : ContainerInfoBase
    {
        public override string ContainerDisplayName => "DryIoC";

        public override IEnumerable<string> ContainerQualifiedNames
        {
            get
            {
                yield return "DryIoC";
            }
        }

        protected override ComposablePartCatalog GetComponentCatalog()
        {
            return new AssemblyCatalog(Assembly.GetExecutingAssembly());
        }
    }
}
