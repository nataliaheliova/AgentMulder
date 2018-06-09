// Patterns: 1
// Matches: Foo.cs
// NotMatches: Bar.cs

using DryIoc;
using TestApplication.Types;

namespace TestApplication.DryIoC
{
    public class RegisterDelegateLambda
    {
        public RegisterDelegateLambda()
        {
            var container = new Container();

            container.RegisterDelegate(resolver => new Foo(), reuse: Reuse.InCurrentScope);
        }
    }
}