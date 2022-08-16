﻿using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;
using Minsk.IO;

namespace Minsk
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("usage: mc <source-path>");
                return;
            }

            if (args.Length > 1)
            {
                Console.WriteLine("error: only one path supported right now");
                return;
            }

            var paths = GetFilePaths(args);
            var syntaxTrees = new List<SyntaxTree>();
            var hasErrors = false;

            foreach (var path in paths)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"error: file '{path}' doesn't exist");
                    hasErrors = true;
                    continue;
                }
                var syntaxTree = SyntaxTree.Load(path);
                syntaxTrees.Add(syntaxTree);
            }

            if (hasErrors)
                return;

            var compilation = new Compilation(syntaxTrees.ToArray());
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());
            if (!result.Diagnostics.Any())
            {
                if (result.Value != null)
                    Console.Out.WriteLine(result.Value);
            }
            else
                Console.Error.WriteDiagnostics(result.Diagnostics);
        }

        private static IEnumerable<string> GetFilePaths(string[] args)
        {
            var result = new SortedSet<string>();

            foreach (var path in args)
            {
                if (Directory.Exists(path))
                {
                    result.UnionWith(Directory.EnumerateFiles(path, "*.ms", SearchOption.AllDirectories));
                }
                else
                {
                    result.Add(path);
                }
            }

            return result;
        }
    }
}
