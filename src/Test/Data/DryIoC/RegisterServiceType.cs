// Patterns: 1
// Matches: Foo.cs
// NotMatches: Bar.cs

using DryIoc;
using TestApplication.Types;

namespace TestApplication.DryIoC
{
    public class RegisterServiceType
    {
        public RegisterServiceType()
        {
            var container = new Container();

            container.Register<IFoo, Foo>();
        }
    }
}