﻿using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;
using Minsk.IO;
using System.Collections.Immutable;
using System.IO;

namespace Minsk
{
    internal sealed class MinskRepl : Repl
    {
        private Compilation _previous;
        private static bool _loadingSubmission;
        private static readonly Compilation emptyCompilation = Compilation.CreateScript(null);
        private bool _showTree;
        private bool _showProgram;
        private Dictionary<VariableSymbol, object> _variables = new Dictionary<VariableSymbol, object>();

        public MinskRepl()
        {
            LoadSubmissions();
        }

        private sealed class RenderState
        {
            public RenderState(SourceText text, ImmutableArray<SyntaxToken> tokens)
            {
                Text = text;
                Tokens = tokens;
            }

            public SourceText Text { get; }
            public ImmutableArray<SyntaxToken> Tokens { get; }
        }

        protected override object RenderLine(IReadOnlyList<string> lines, int lineIndex, object state)
        {
            RenderState renderState;
            if (state == null)
            {
                var text = string.Join(Environment.NewLine, lines);
                var sourceText = SourceText.From(text);
                var tokens = SyntaxTree.ParseTokens(sourceText);
                renderState = new RenderState(sourceText, tokens);
            }
            else
            {
                renderState = (RenderState)state;
            }

            var lineSpan = renderState.Text.Lines[lineIndex].Span;

            foreach (var token in renderState.Tokens)
            {
                if (!lineSpan.OverlapsWith(token.Span))
                    continue;

                var tokenStart = Math.Max(token.Span.Start, lineSpan.Start);
                var tokenEnd = Math.Min(token.Span.End, lineSpan.End);
                var tokenSpan = TextSpan.FromBounds(tokenStart, tokenEnd);
                var tokenText = renderState.Text.ToString(tokenSpan);

                var isKeyword = token.Kind.isKeyWord();
                var isNumber = token.Kind == SyntaxKind.NumberToken;
                var isIdentifer = token.Kind == SyntaxKind.IdentifierToken;
                var isString = token.Kind == SyntaxKind.StringToken;
                var isComment = token.Kind == SyntaxKind.SingleLineCommentTrivia || token.Kind == SyntaxKind.MultiLineCommentTrivia;
                if (isKeyword)
                    Console.ForegroundColor = ConsoleColor.Blue;
                else if (isNumber)
                    Console.ForegroundColor = ConsoleColor.Cyan;
                else if (isIdentifer)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                else if (isString)
                    Console.ForegroundColor = ConsoleColor.Magenta;
                else if (isComment)
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                Console.Write(tokenText);

                Console.ResetColor();
            }

            return renderState;
        }

        [MetaCommand("showTree", "Shows the parse tree")]
        private void EvaluateShowTree()
        {
            _showTree = !_showTree;
            Console.WriteLine(_showTree ? "Showing parse tree" : "Not showing parse tree");
        }

        [MetaCommand("showProgram", "Shows the bound tree")]
        private void EvaluateShowProgram()
        {
            _showProgram = !_showProgram;
            Console.WriteLine(_showProgram ? "Showing bound tree" : "Not showing bound tree");
        }

        [MetaCommand("cls", "Clears the screen")]
        private void EvaluateClear()
        {
            Console.Clear();
        }

        [MetaCommand("reset", "Clears all previous submissions")]
        private void EvaluateReset()
        {
            _previous = null;
            _variables.Clear();
            ClearSubmissions();
        }

        [MetaCommand("load", "Loads a script file")]
        private void EvaluateLoad(string path)
        {
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: file does not exist '{path}'");
                Console.ResetColor();
                return;
            }

            var text = File.ReadAllText(path);
            EvaluateSubmission(text);
        }

        [MetaCommand("ls", "Lists all symbols")]
        private void EvaluateLs()
        {
            var compilation = _previous ?? emptyCompilation;

            var symbols = compilation.GetSymbols().OrderBy(s => s.Kind).ThenBy(s => s.Name);

            foreach (var symbol in symbols)
            {
                symbol.WriteTo(Console.Out);
            }
        }

        [MetaCommand("dump", "Shows bound tree of given function")]
        private void EvaluateDump(string functionName)
        {
            var compilation = _previous ?? emptyCompilation;

            var function = compilation.GetSymbols().OfType<FunctionSymbol>().SingleOrDefault(f => f.Name == functionName);

            if (function == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: function '{functionName}' does not exist");
                Console.ResetColor();
                return;
            }

            compilation.EmitTree(function, Console.Out);
        }

        protected override void EvaluateSubmission(string text)
        {
            var syntaxTree = SyntaxTree.Parse(text);

            var compilation = Compilation.CreateScript(_previous, syntaxTree);
            var result = compilation.Evaluate(_variables);
            var diagnostics = result.Diagnostics;

            if (_showTree)
                syntaxTree.Root.WriteTo(Console.Out);

            if (_showProgram)
                compilation.EmitTree(Console.Out);

            if (!diagnostics.Any())
            {
                if (result.Value != null)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(result.Value);
                    Console.ResetColor();
                }
                _previous = compilation;

                SaveSubmission(text);
            }
            else
            {
                Console.Out.WriteDiagnostics(diagnostics);
            }
        }

        private static string GetSubmissionDirectory()
        {
            var locationAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var submissionDirectory = Path.Combine(locationAppData, "Minsk", "Submissions");
            return submissionDirectory;
        }

        private void LoadSubmissions()
        {
            var submissionDirectory = GetSubmissionDirectory();
            if (!Directory.Exists(submissionDirectory)) return;
            var files = Directory.GetFiles(submissionDirectory).OrderBy(f => f).ToArray();
            if (files.Length == 0) return;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"loaded {files.Length} submission(s)");
            Console.ResetColor();

            _loadingSubmission = true;

            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                EvaluateSubmission(text);
            }

            _loadingSubmission = false;
        }

        private static void ClearSubmissions()
        {
            if (Directory.Exists(GetSubmissionDirectory()))
                Directory.Delete(GetSubmissionDirectory(), recursive: true);
        }

        private static void SaveSubmission(string text)
        {
            if (_loadingSubmission)
                return;
            var submissionDirectory = GetSubmissionDirectory();
            Directory.CreateDirectory(submissionDirectory);

            var count = Directory.GetFiles(submissionDirectory).Length;
            var name = $"submission{count:0000}";
            var fileName = Path.Combine(submissionDirectory, name);
            File.WriteAllText(fileName, text);
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

            //Use Members beasue we need to exclude the EndOfFileToken.
            var lastMember = syntaxTree.Root.Members.LastOrDefault();
            if (lastMember == null || lastMember.GetLastToken().IsMissing)
                return false;

            return true;
        }
    }
}
