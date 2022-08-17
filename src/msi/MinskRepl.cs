using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;
using Minsk.IO;
using System.IO;

namespace Minsk
{
    internal sealed class MinskRepl : Repl
    {
        private Compilation _previous;
        private static bool _loadingSubmission;
        private bool _showTree;
        private bool _showProgram;
        private Dictionary<VariableSymbol, object> _variables = new Dictionary<VariableSymbol, object>();

        public MinskRepl()
        {
            LoadSubmissions();
        }

        protected override void RenderLine(string line)
        {
            var tokens = SyntaxTree.ParseTokens(line);

            foreach (var token in tokens)
            {
                var isKeyword = token.Kind.ToString().EndsWith("Keyword");
                var isNumber = token.Kind == SyntaxKind.NumberToken;
                var isIdentifer = token.Kind == SyntaxKind.IdentifierToken;
                var isString = token.Kind == SyntaxKind.StringToken;

                if (isKeyword)
                    Console.ForegroundColor = ConsoleColor.Blue;
                else if (isNumber)
                    Console.ForegroundColor = ConsoleColor.Cyan;
                else if (isIdentifer)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                else if (isString)
                    Console.ForegroundColor = ConsoleColor.Magenta;
                else
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                Console.Write(token.Text);

                Console.ResetColor();
            }
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
            var compilation = _previous ?? new Compilation();

            var symbols = compilation.GetSymbols().OrderBy(s => s.Kind).ThenBy(s => s.Name);

            foreach (var symbol in symbols)
            {
                symbol.WriteTo(Console.Out);
            }
        }

        [MetaCommand("dump", "Shows bound tree of given function")]
        private void EvaluateDump(string functionName)
        {
            var compilation = _previous ?? new Compilation();

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

            var compilation = _previous == null ? new Compilation(syntaxTree) : _previous.ContinueWith(syntaxTree);
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
            if (syntaxTree.Root.Members.Last().GetLastToken().IsMissing)
                return false;

            return true;
        }
    }
}
