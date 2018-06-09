// Patterns: 1
// Matches: Foo.cs
// NotMatches: Bar.cs

using DryIoc;
using TestApplication.Types;

namespace TestApplication.DryIoC
{
    public class RegisterDelegateOfType
    {
        public RegisterDelegateOfType()
        {
            var container = new Container();

            container.RegisterDelegate(typeof(IFoo), resolver => new Foo(), reuse: Reuse.InCurrentScope);
        }
    }
}