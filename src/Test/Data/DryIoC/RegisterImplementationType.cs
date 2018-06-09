// Patterns: 1
// Matches: Foo.cs
// NotMatches: Bar.cs

using DryIoc;
using TestApplication.Types;

namespace TestApplication.DryIoC
{
    public class RegisterImplementationType
    {
        public RegisterImplementationType()
        {
            var container = new Container();

            container.Register<Foo>();
        }
    }
}
