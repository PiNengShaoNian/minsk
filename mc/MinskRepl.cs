// See https://aka.ms/new-console-template for more information

using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;

namespace Minsk
{
    internal sealed class MinskRepl : Repl
    {
        private Compilation _previous;
        private bool _showTree;
        private bool _showProgram;
        private Dictionary<VariableSymbol, object> _variables = new Dictionary<VariableSymbol, object>();

        protected override void RenderLine(string line)
        {
            var tokens = SyntaxTree.ParseTokens(line);

            foreach (var token in tokens)
            {
                var isKeyword = token.Kind.ToString().EndsWith("Keyword");
                var isNumber = token.Kind == SyntaxKind.NumberToken;
                var isIdentifer = token.Kind == SyntaxKind.IdentifierToken;

                if (isKeyword)
                    Console.ForegroundColor = ConsoleColor.Blue;
                else if (isNumber)
                    Console.ForegroundColor = ConsoleColor.Cyan;
                else if (isIdentifer)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                else
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                Console.Write(token.Text);

                Console.ResetColor();
            }
        }

        protected override void EvaluateMetaCommand(string input)
        {
            if (input == "#showTree")
            {
                _showTree = !_showTree;
                Console.WriteLine(_showTree ? "Showing parse tree" : "Not showing parse tree");
            }
            else if (input == "#showProgram")
            {
                _showProgram = !_showProgram;
                Console.WriteLine(_showProgram ? "Showing bound tree" : "Not showing bound tree");
            }
            else if (input == "#cls")
            {
                Console.Clear();
            }
            else if (input == "#reset")
            {
                _previous = null;
            }
            else
            {
                base.EvaluateMetaCommand(input);
            }
        }

        protected override void EvaluateSubmission(string text)
        {
            var syntaxTree = SyntaxTree.Parse(text);

            var compilation = _previous == null ? new Compilation(syntaxTree) : _previous.ContinueWith(syntaxTree);
            var result = compilation.Evaluate(_variables);
            var diagnostics = result.Diagnostics;

            if (_showTree)
                syntaxTree.Root.WriteTo(Console.Out);

            if (_showProgram)
                compilation.EmitTree(Console.Out);

            if (!diagnostics.Any())
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(result.Value);
                Console.ResetColor();
                _previous = compilation;
            }
            else
            {
                foreach (var diagnostic in diagnostics)
                {
                    var lineIndex = syntaxTree.Text.GetLineIndex(diagnostic.Span.Start);
                    TextLine line = syntaxTree.Text.Lines[lineIndex];
                    var character = diagnostic.Span.Start - line.Start + 1;
                    var lineNumber = lineIndex + 1;
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write($"({lineNumber}, {character}): ");
                    Console.WriteLine(diagnostic);
                    Console.ResetColor();

                    var prefixSpan = TextSpan.FromBounds(line.Start, diagnostic.Span.Start);
                    var suffixSpan = TextSpan.FromBounds(diagnostic.Span.End, line.End);

                    var prefix = syntaxTree.Text.ToString(prefixSpan);
                    var error = syntaxTree.Text.ToString(diagnostic.Span);
                    var suffix = syntaxTree.Text.ToString(suffixSpan);

                    Console.Write("    ");
                    Console.Write(prefix);

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(error);
                    Console.ResetColor();

                    Console.Write(suffix);

                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }

        protected override bool IsCompleteSubmission(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            var lastTwoLinesAreBlank = text.Split(Environment.NewLine)
                                           .Reverse()
                                           .TakeWhile(s => string.IsNullOrEmpty(s))
                                           .Take(2)
                                           .Count() == 2;

            if (lastTwoLinesAreBlank)
                return true;

            var syntaxTree = SyntaxTree.Parse(text);
            //Use Statement beasue we need to exclude the EndOfFileToken.
            if (syntaxTree.Root.Statement.GetLastToken().IsMissing)
                return false;

            return true;
        }
    }
}
