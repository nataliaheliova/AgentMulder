// Patterns: 1
// Matches: Foo.cs
// NotMatches: Bar.cs

using DryIoc;
using TestApplication.Types;

namespace TestApplication.DryIoC
{
    public class RegisterDelegateLambdaGeneric
    {
        public RegisterDelegateLambdaGeneric()
        {
            var container = new Container();

            container.RegisterDelegate<IFoo>(resolver => new Foo(), reuse: Reuse.InCurrentScope);
        }
    }
}