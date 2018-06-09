// Patterns: 1
// Matches: Foo.cs
// NotMatches: Bar.cs

using DryIoc;
using TestApplication.Types;

namespace TestApplication.DryIoC
{
    public class RegisterServiceTypeOfMoreArguments
    {
        public RegisterServiceTypeOfMoreArguments()
        {
            var container = new Container();

            container.Register(typeof(IFoo), typeof(Foo), reuse: Reuse.InCurrentScope, serviceKey: "key");
        }
    }
}