// Patterns: 1
// Matches: Foo.cs
// NotMatches: Bar.cs

using DryIoc;
using TestApplication.Types;

namespace TestApplication.DryIoC
{
    public class RegisterServiceTypeOf
    {
        public RegisterServiceTypeOf()
        {
            var container = new Container();

            container.Register(typeof(IFoo), typeof(Foo));
        }
    }
}