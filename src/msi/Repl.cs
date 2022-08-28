﻿// See https://aka.ms/new-console-template for more information

using Minsk.CodeAnalysis;
using Minsk.IO;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;

namespace Minsk
{
    internal abstract class Repl
    {
        private readonly List<MetaCommand> _metaCommands = new List<MetaCommand>();
        private readonly List<string> _submissionHistory = new List<string>();
        private int _submissionHistoryIndex;
        private bool _done;

        protected Repl()
        {
            InitializeMetaCommands();
        }

        private void InitializeMetaCommands()
        {
            var methods = GetType().GetMethods(
                                       BindingFlags.Public |
                                       BindingFlags.NonPublic |
                                       BindingFlags.Static |
                                       BindingFlags.Instance |
                                       BindingFlags.FlattenHierarchy);
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<MetaCommandAttribute>();

                if (attribute == null)
                    continue;

                var metaCommand = new MetaCommand(attribute.Name, attribute.Description, method);
                _metaCommands.Add(metaCommand);
            }
        }

        public void Run()
        {
            while (true)
            {
                var text = EditSubmission();

                if (string.IsNullOrEmpty(text))
                    return;

                if (!text.Contains(Environment.NewLine) && text.StartsWith("#"))
                    EvaluateMetaCommand(text);
                else
                    EvaluateSubmission(text);

                _submissionHistory.Add(text);
                _submissionHistoryIndex = 0;
            }
        }

        private delegate object? LineRenderHandler(IReadOnlyList<string> lines, int lineIndex, object? state);

        private sealed class SubmissionView
        {
            private readonly LineRenderHandler _lineRender;
            private readonly ObservableCollection<string> _submissionDocument;
            private int _cursorTop;
            private int _renderedLineCount;
            private int _currentLineIndex;
            private int _currentCharacter;

            public SubmissionView(LineRenderHandler lineRender, ObservableCollection<string> submissionDocument)
            {
                _lineRender = lineRender;
                _submissionDocument = submissionDocument;
                _submissionDocument.CollectionChanged += SubmissionDocumentChanged!;
                _cursorTop = Console.CursorTop;
                Render();
            }

            private void SubmissionDocumentChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                Render();
            }

            private void Render()
            {
                Console.CursorVisible = false;
                var lineCount = 0;
                var state = (object?)null;

                foreach (var line in _submissionDocument)
                {
                    if (_cursorTop + lineCount >= Console.WindowHeight)
                    {
                        Console.SetCursorPosition(0, Console.WindowHeight - 1);
                        Console.WriteLine();
                        if (_cursorTop > 0)
                            _cursorTop--;
                    }

                    Console.SetCursorPosition(0, _cursorTop + lineCount);
                    Console.ForegroundColor = ConsoleColor.Green;

                    if (lineCount == 0)
                        Console.Write("> ");
                    else
                        Console.Write("| ");

                    Console.ResetColor();

                    state = _lineRender(_submissionDocument, lineCount, state);

                    Console.Write(new string(' ', Console.WindowWidth - line.Length - 2));
                    ++lineCount;
                }

                var numberOfBlankLines = _renderedLineCount - lineCount;
                if (numberOfBlankLines > 0)
                {
                    var blankLine = new string(' ', Console.WindowWidth);

                    for (var i = 0; i < numberOfBlankLines; ++i)
                    {
                        Console.SetCursorPosition(0, _cursorTop + lineCount + i);
                        Console.WriteLine(blankLine);
                    }
                }

                _renderedLineCount = lineCount;
                Console.CursorVisible = true;
                UpdateCursorPosition();
            }

            private void UpdateCursorPosition()
            {
                Console.CursorTop = _cursorTop + _currentLineIndex;
                Console.CursorLeft = 2 + _currentCharacter;
            }

            public int CurrentLine
            {
                get => _currentLineIndex;
                set
                {
                    if (_currentLineIndex != value)
                    {
                        _currentLineIndex = value;
                        _currentCharacter = Math.Min(_submissionDocument[_currentLineIndex].Length, _currentCharacter);
                        UpdateCursorPosition();
                    }
                }
            }
            public int CurrentCharacter
            {
                get => _currentCharacter; set
                {
                    if (_currentCharacter != value)
                    {
                        _currentCharacter = value;
                        UpdateCursorPosition();
                    }
                }
            }
        }

        private ObservableCollection<string> _document = new ObservableCollection<string>() { "" };

        private string EditSubmission()
        {
            _done = false;
            var document = new ObservableCollection<string>() { "" };
            var view = new SubmissionView(RenderLine, document);

            while (!_done)
            {
                var key = Console.ReadKey(true);

                HandleKey(key, document, view);
            }

            view.CurrentLine = document.Count - 1;
            view.CurrentCharacter = document[view.CurrentLine].Length;
            Console.WriteLine();

            return String.Join(Environment.NewLine, document);
        }

        private void HandleKey(ConsoleKeyInfo key, ObservableCollection<string> document, SubmissionView view)
        {
            if (key.Modifiers == default(ConsoleModifiers))
            {
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        HandleEscape(document, view);
                        break;
                    case ConsoleKey.Enter:
                        HandleEnter(document, view);
                        break;
                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow(document, view);
                        break;
                    case ConsoleKey.RightArrow:
                        HandleRightArrow(document, view);
                        break;
                    case ConsoleKey.UpArrow:
                        HandleUpArrow(document, view);
                        break;
                    case ConsoleKey.DownArrow:
                        HandleDownArrow(document, view);
                        break;
                    case ConsoleKey.Backspace:
                        HandleBackspace(document, view);
                        break;
                    case ConsoleKey.Delete:
                        HandleDelete(document, view);
                        break;
                    case ConsoleKey.Home:
                        HandleHome(document, view);
                        break;
                    case ConsoleKey.End:
                        HandleEnd(document, view);
                        break;
                    case ConsoleKey.Tab:
                        HandleTab(document, view);
                        break;
                        //case ConsoleKey.PageUp:
                        //    HandlePageUp(document, view);
                        //    break;
                        //case ConsoleKey.PageDown:
                        //    HandlePageDown(document, view);
                        //    break;
                }
            }
            else if (key.Modifiers == ConsoleModifiers.Control)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        HandleControlEnter(document, view);
                        break;
                }
            }
            else if (key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Control))
            {
                switch (key.Key)
                {
                    case ConsoleKey.U:
                        HandlePageUp(document, view);
                        break;
                    case ConsoleKey.D:
                        HandlePageDown(document, view);
                        break;
                }
            }

            if (key.KeyChar >= ' ')
            {
                HandleTyping(document, view, key.KeyChar.ToString());
            }
        }

        private void HandleEscape(ObservableCollection<string> document, SubmissionView view)
        {
            document.Clear();
            document.Add(string.Empty);
            view.CurrentLine = 0;
            view.CurrentCharacter = 0;
        }

        private void HandleEnter(ObservableCollection<string> document, SubmissionView view)
        {
            var submissionText = string.Join(Environment.NewLine, document);

            if (submissionText.StartsWith("#") || IsCompleteSubmission(submissionText))
            {
                _done = true;
                return;
            }

            InsertLine(document, view);
        }

        private void HandleControlEnter(ObservableCollection<string> document, SubmissionView view)
        {
            InsertLine(document, view);
        }

        private static void InsertLine(ObservableCollection<string> document, SubmissionView view)
        {
            var remainder = document[view.CurrentLine].Substring(view.CurrentCharacter);
            document[view.CurrentLine] = document[view.CurrentLine].Substring(0, view.CurrentCharacter);

            var lineIndex = view.CurrentLine + 1;
            document.Insert(lineIndex, remainder);
            view.CurrentCharacter = 0;
            view.CurrentLine = lineIndex;
        }

        private void HandleDownArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine < document.Count - 1)
                ++view.CurrentLine;
        }

        private void HandleUpArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine > 0)
                --view.CurrentLine;
        }

        private void HandleRightArrow(ObservableCollection<string> document, SubmissionView view)
        {
            var line = document[view.CurrentLine];
            if (view.CurrentCharacter <= line.Length - 1)
                ++view.CurrentCharacter;
        }

        private void HandleLeftArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentCharacter > 0)
                --view.CurrentCharacter;
        }

        private void HandleDelete(ObservableCollection<string> document, SubmissionView view)
        {
            var start = view.CurrentCharacter;
            var lineIndex = view.CurrentLine;
            var line = document[lineIndex];
            if (start >= line.Length)
            {
                if (view.CurrentLine == document.Count - 1)
                    return;

                var nextLine = document[view.CurrentLine + 1];
                document[view.CurrentLine] += nextLine;
                document.RemoveAt(view.CurrentLine + 1);
                return;
            }

            var before = line.Substring(0, start);
            var after = line.Substring(start + 1);
            document[lineIndex] = before + after;
        }

        private void HandleBackspace(ObservableCollection<string> document, SubmissionView view)
        {
            var start = view.CurrentCharacter;
            if (start == 0)
            {
                if (view.CurrentLine == 0)
                {
                    return;
                }
                var currentLine = document[view.CurrentLine];
                var previousLine = document[view.CurrentLine - 1];
                document.RemoveAt(view.CurrentLine);
                --view.CurrentLine;
                document[view.CurrentLine] = previousLine + currentLine;
                view.CurrentCharacter = previousLine.Length;
            }
            else
            {
                var lineIndex = view.CurrentLine;
                var line = document[lineIndex];

                var before = line.Substring(0, start - 1);
                var after = line.Substring(start);
                document[lineIndex] = before + after;
                --view.CurrentCharacter;
            }

        }

        private void HandleEnd(ObservableCollection<string> document, SubmissionView view)
        {
            view.CurrentCharacter = document[view.CurrentLine].Length;
        }

        private void HandleTab(ObservableCollection<string> document, SubmissionView view)
        {
            const int TabWidth = 4;
            var start = view.CurrentCharacter;
            var remainingSpaces = TabWidth - start % TabWidth;

            var line = document[view.CurrentLine];
            document[view.CurrentLine] = line.Insert(start, new string(' ', remainingSpaces));
            view.CurrentCharacter += remainingSpaces;
        }

        private void HandleHome(ObservableCollection<string> document, SubmissionView view)
        {
            view.CurrentCharacter = 0;
        }

        private void HandlePageDown(ObservableCollection<string> document, SubmissionView view)
        {
            ++_submissionHistoryIndex;
            if (_submissionHistoryIndex >= _submissionHistory.Count)
                _submissionHistoryIndex = 0;

            UpdateDocumentFromHistory(document, view);
        }

        private void UpdateDocumentFromHistory(ObservableCollection<string> document, SubmissionView view)
        {
            if (_submissionHistory.Count == 0)
                return;

            document.Clear();
            var historyItem = _submissionHistory[_submissionHistoryIndex];
            var lines = historyItem.Split(Environment.NewLine);
            foreach (var line in lines)
                document.Add(line);

            view.CurrentLine = document.Count - 1;
            view.CurrentCharacter = document[view.CurrentLine].Length;
        }

        private void HandlePageUp(ObservableCollection<string> document, SubmissionView view)
        {
            _submissionHistoryIndex--;
            if (_submissionHistoryIndex < 0)
                _submissionHistoryIndex = _submissionHistory.Count - 1;

            UpdateDocumentFromHistory(document, view);
        }

        private void HandleTyping(ObservableCollection<string> document, SubmissionView view, string text)
        {
            var lineIndex = view.CurrentLine;
            var start = view.CurrentCharacter;
            document[lineIndex] = document[lineIndex].Insert(start, text);
            view.CurrentCharacter += text.Length;
        }

        protected void ClearHistory()
        {
            _submissionHistory.Clear();
        }

        protected virtual object? RenderLine(IReadOnlyList<string> lines, int lineIndex, object? state)
        {
            Console.Write(lines[lineIndex]);
            return state;
        }

        private void EvaluateMetaCommand(string input)
        {
            // #dump getname
            // #load samples/hello/hello.ms
            // # "samples/hello world/hello.ms"

            var position = 1;
            var inQuotes = false;
            var args = new List<string>();
            var sb = new StringBuilder();
            while (position < input.Length)
            {
                var c = input[position];
                var l = position + 1 >= input.Length ? '\0' : input[position + 1];

                if (char.IsWhiteSpace(c))
                {
                    if (!inQuotes)
                        CommitPendingArgument();
                }
                else if (c == '\"')
                {
                    if (!inQuotes)
                        inQuotes = true;
                    else if (l == '\"')
                    {
                        sb.Append(c);
                        ++position;
                    }
                    else if (inQuotes)
                        inQuotes = false;
                }
                else
                {
                    sb.Append(c);
                }

                ++position;
            }

            CommitPendingArgument();

            void CommitPendingArgument()
            {
                var arg = sb.ToString();
                if (!string.IsNullOrWhiteSpace(arg))
                    args.Add(arg);

                sb.Clear();
            }


            var commandName = args.FirstOrDefault();
            if (args.Count > 0)
                args.RemoveAt(0);
            var command = _metaCommands.SingleOrDefault(mc => mc.Name == commandName);

            if (command == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid command {input}.");
                Console.ResetColor();
                return;
            }

            var parameters = command.Method.GetParameters();

            if (args.Count != parameters.Length)
            {
                var parameterNames = String.Join(" ", parameters.Select(p => $"<{p.Name}>"));
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: invalid number of arguments (given {args.Count}, expected {parameters.Length}).");
                Console.WriteLine($"usage: #{commandName} {parameterNames}");
                Console.ResetColor();
                return;
            }

            var instance = command.Method.IsStatic ? null : this;
            command.Method.Invoke(instance, args.ToArray());
        }

        protected abstract void EvaluateSubmission(string text);

        protected abstract bool IsCompleteSubmission(string text);

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        protected sealed class MetaCommandAttribute : Attribute
        {
            public MetaCommandAttribute(string name, string description)
            {
                Name = name;
                Description = description;
            }

            public string Description { get; set; }
            public string Name { get; }
        }

        private sealed class MetaCommand
        {
            public MetaCommand(string name, string description, MethodInfo method)
            {
                Name = name;
                Description = description;
                Method = method;
            }

            public string Name { get; }
            public string Description { get; set; }
            public MethodInfo Method { get; }
        }

        [MetaCommand("help", "Shows help")]
        protected void EvaluateHelp()
        {
            var maxNameLength = _metaCommands.Max(mc => mc.Name.Length);
            foreach (var metaCommand in _metaCommands.OrderBy(m => m.Name))
            {
                var metaParams = metaCommand.Method.GetParameters();
                if (metaParams.Length == 0)
                {
                    var paddedName = metaCommand.Name.PadRight(maxNameLength);

                    Console.Out.WritePunctuation("#");
                    Console.Out.WriteIdentifier(paddedName);
                }
                else
                {
                    Console.Out.WritePunctuation("#");
                    Console.Out.WriteIdentifier(metaCommand.Name);
                    foreach (var pi in metaParams)
                    {
                        if (pi.Name == null) continue;
                        Console.Out.WritePunctuation(" <");
                        Console.Out.WriteIdentifier(pi.Name);
                        Console.Out.WritePunctuation(">");
                    }
                    Console.Out.WriteLine();

                    for (int _ = 0; _ < maxNameLength; _++)
                        Console.Out.Write(" ");
                }
                Console.Out.Write("   ");
                Console.Out.WritePunctuation(metaCommand.Description);
                Console.Out.WriteLine();
            }
        }
    }
}
