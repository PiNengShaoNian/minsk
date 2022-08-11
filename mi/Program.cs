// See https://aka.ms/new-console-template for more information

namespace Minsk
{
    internal static class Program
    {
        private static void Main()
        {
            var repl = new MinskRepl();
            repl.Run();
        }
    }
}
