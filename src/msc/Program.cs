using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;
using Minsk.IO;
using Mono.Options;

namespace Minsk
{
    internal static class Program
    {
        private static int Main(string[] args)
        {


            var referencePaths = new List<string>();
            var outputPath = (string?)null;
            var moduleName = (string?)null;
            var sourcePaths = new List<string>();
            var helpRequest = false;

            var options = new OptionSet {
                "usage: msc <source-paths> [options]",
                { "r=", "The {path} of an assembly to reference", v => referencePaths.Add(v) },
                { "o=", "The output {path} of the assembly to create", v => outputPath = v },
                { "m=", "The {name} of the module ", v => moduleName = v },
                { "?|h|help", "Prints help",  v => helpRequest = true },
                { "<>",  v => sourcePaths.Add(v) },
            };

            options.Parse(args);

            if (helpRequest)
            {
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (sourcePaths.Count == 0)
            {
                Console.Error.WriteLine("error: need at least one source file");
                return 1;
            }

            if (outputPath == null)
                outputPath = Path.ChangeExtension(sourcePaths[0], ".exe");

            if (moduleName == null)
                moduleName = Path.GetFileNameWithoutExtension(outputPath);

            var syntaxTrees = new List<SyntaxTree>();
            var hasErrors = false;

            foreach (var path in sourcePaths)
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"error: file '{path}' doesn't exist");
                    hasErrors = true;
                    continue;
                }
                var syntaxTree = SyntaxTree.Load(path);
                syntaxTrees.Add(syntaxTree);
            }

            foreach (var path in sourcePaths)
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"error: file '{path}' doesn't exist");
                    hasErrors = true;
                    continue;
                }
            }

            if (hasErrors)
                return 1;

            var compilation = Compilation.Create(syntaxTrees.ToArray());
            var diagnotics = compilation.Emit(moduleName, referencePaths.ToArray(), outputPath);

            if (diagnotics.Any())
            {
                Console.Error.WriteDiagnostics(diagnotics);
                return 1;
            }
            else
            {
            }

            return 0;
        }
    }
}
