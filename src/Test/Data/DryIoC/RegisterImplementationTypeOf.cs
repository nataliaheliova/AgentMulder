// Patterns: 1
// Matches: Foo.cs
// NotMatches: Bar.cs

using DryIoc;
using TestApplication.Types;

namespace TestApplication.DryIoC
{
    public class RegisterImplementationTypeOf
    {
        public RegisterImplementationTypeOf()
        {
            var container = new Container();

            container.Register(typeof(Foo));
        }
    }
}