﻿using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Minsk.CodeAnalysis.Syntax
{
    internal class Lexer
    {
        private readonly SourceText _text;
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly SyntaxTree _syntaxTree;
        private int _position;
        private SyntaxKind _kind;
        private int _start;
        private object? _value;
        private ImmutableArray<SyntaxTrivia>.Builder _triviaBuilder = ImmutableArray.CreateBuilder<SyntaxTrivia>();

        public Lexer(SyntaxTree syntaxTree)
        {
            _text = syntaxTree.Text;
            _syntaxTree = syntaxTree;
        }

        public DiagnosticBag Diagnostics => _diagnostics;

        private char Current => Peek(0);

        private char Lookahead => Peek(1);

        private char Peek(int offset)
        {
            int index = _position + offset;

            if (index >= _text.Length)
            {
                return '\0';
            }
            else
            {
                return _text[index];
            }
        }

        private void Next()
        {
            _position++;
        }

        public SyntaxToken Lex()
        {
            ReadTrivia(leading: true);
            var leadingTrivia = _triviaBuilder.ToImmutable();
            var tokenStart = _position;

            ReadToken();

            var tokenKind = _kind;
            var tokenValue = _value;
            var tokenLength = _position - _start;

            ReadTrivia(leading: false);
            var trailingTrivia = _triviaBuilder.ToImmutable();

            var text = SyntaxFacts.GetText(tokenKind);
            if (text == null)
                text = _text.ToString(tokenStart, tokenLength);

            return new SyntaxToken(_syntaxTree, tokenKind, tokenStart, text, tokenValue, leadingTrivia, trailingTrivia);
        }

        private void ReadTrivia(bool leading)
        {
            _triviaBuilder.Clear();
            var done = false;

            while (!done)
            {
                _start = _position;
                _kind = SyntaxKind.BadToken;
                _value = null;

                switch (Current)
                {
                    case '\0':
                        done = true;
                        break;
                    case '/':
                        if (Lookahead == '/')
                            ReadSingleLineComment();
                        else if (Lookahead == '*')
                            ReadMultiLineComment();
                        else
                            done = true;
                        break;
                    case ' ':
                    case '\t':
                        ReadWhiteSpace();
                        break;
                    case '\r':
                    case '\n':
                        if (!leading)
                            done = true;
                        ReadLineBreak();
                        break;
                    default:
                        if (char.IsWhiteSpace(Current))
                        {
                            ReadWhiteSpace();
                        }
                        else
                        {
                            done = true;
                        }
                        break;
                }

                var length = _position - _start;

                if (length > 0)
                {
                    var text = _text.ToString(_start, length);
                    var trivia = new SyntaxTrivia(_syntaxTree, _kind, _start, text);
                    _triviaBuilder.Add(trivia);
                }
            }
        }

        private void ReadToken()
        {
            _start = _position;
            _kind = SyntaxKind.BadToken;
            _value = null;

            switch (Current)
            {
                case '\0':
                    _kind = SyntaxKind.EndOfFileToken;
                    break;
                case '+':
                    _kind = SyntaxKind.PlusToken;
                    ++_position;
                    break;
                case '-':
                    _kind = SyntaxKind.MinusToken;
                    ++_position;
                    break;
                case '*':
                    _kind = SyntaxKind.StarToken;
                    ++_position;
                    break;
                case '/':
                    _kind = SyntaxKind.SlashToken;
                    ++_position;
                    break;
                case '(':
                    _kind = SyntaxKind.OpenParenthesisToken;
                    ++_position;
                    break;
                case ')':
                    _kind = SyntaxKind.CloseParenthesisToken;
                    ++_position;
                    break;
                case '{':
                    _kind = SyntaxKind.OpenBraceToken;
                    ++_position;
                    break;
                case '}':
                    _kind = SyntaxKind.CloseBraceToken;
                    ++_position;
                    break;
                case ':':
                    _kind = SyntaxKind.ColonToken;
                    ++_position;
                    break;
                case ',':
                    _kind = SyntaxKind.CommaToken;
                    ++_position;
                    break;
                case '~':
                    _kind = SyntaxKind.TildeToken;
                    ++_position;
                    break;
                case '^':
                    _kind = SyntaxKind.HatToken;
                    ++_position;
                    break;
                case '!':
                    if (Lookahead == '=')
                    {
                        _kind = SyntaxKind.BangEqualsToken;
                        _position += 2;
                    }
                    else
                    {
                        _kind = SyntaxKind.BangToken;
                        ++_position;
                    }
                    break;
                case '&':
                    if (Lookahead == '&')
                    {
                        _kind = SyntaxKind.AmpersandAmpersandToken;
                        _position += 2;
                    }
                    else
                    {
                        _kind = SyntaxKind.AmpersandToken;
                        ++_position;
                    }
                    break;
                case '|':
                    if (Lookahead == '|')
                    {
                        _kind = SyntaxKind.PipePipeToken;
                        _position += 2;
                    }
                    else
                    {
                        _kind = SyntaxKind.PipeToken;
                        ++_position;
                    }
                    break;
                case '=':
                    if (Lookahead == '=')
                    {
                        _kind = SyntaxKind.EqualsEqualsToken;
                        _position += 2;
                    }
                    else
                    {
                        _kind = SyntaxKind.EqualsToken;
                        ++_position;
                    }
                    break;
                case '<':
                    if (Lookahead == '=')
                    {
                        _kind = SyntaxKind.LessOrEqualsToken;
                        _position += 2;
                    }
                    else
                    {
                        _kind = SyntaxKind.LessToken;
                        ++_position;
                    }
                    break;
                case '>':
                    if (Lookahead == '=')
                    {
                        _kind = SyntaxKind.GreaterOrEqualsToken;
                        _position += 2;
                    }
                    else
                    {
                        _kind = SyntaxKind.GreaterToken;
                        ++_position;
                    }
                    break;
                case '"':
                    ReadString();
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    ReadNumberToken();
                    break;
                case '_':
                    ReadIdentifierOrKeyword();
                    break;
                default:
                    {
                        if (char.IsLetter(Current))
                        {
                            ReadIdentifierOrKeyword();
                        }
                        else
                        {
                            var span = new TextSpan(_position, 1);
                            var location = new TextLocation(_text, span);
                            _diagnostics.ReportBadCharacter(location, Current);
                            ++_position;
                        }
                        break;
                    }
            }
        }

        private void ReadMultiLineComment()
        {
            _position += 2;
            var done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                        var span = new TextSpan(_start, 2);
                        var location = new TextLocation(_text, span);
                        _diagnostics.ReportUnterminatedMultiLineComment(location);
                        done = true;
                        break;
                    case '*':
                        if (Lookahead == '/')
                        {
                            done = true;
                            _position += 2;
                        }
                        else
                        {
                            ++_position;
                        }
                        break;
                    default:
                        ++_position;
                        break;
                }
            }

            _kind = SyntaxKind.MultiLineCommentTrivia;
        }

        private void ReadSingleLineComment()
        {
            // Skip leading slashes
            _position += 2;
            var done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\r':
                    case '\n':
                    case '\0':
                        done = true;
                        break;
                    default:
                        ++_position;
                        break;
                }
            }

            _kind = SyntaxKind.SingleLineCommentTrivia;
        }

        private void ReadString()
        {
            _position++;
            var sb = new StringBuilder();
            var done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        var span = new TextSpan(_start, 1);
                        var location = new TextLocation(_text, span);
                        _diagnostics.ReportUnterminatedString(location);
                        done = true;
                        break;
                    case '"':
                        if (Lookahead == '"')
                        {
                            sb.Append(Current);
                            _position += 2;
                        }
                        else
                        {
                            ++_position;
                            done = true;
                        }
                        break;
                    default:
                        sb.Append(Current);
                        ++_position;
                        break;
                }
            }

            _kind = SyntaxKind.StringToken;
            _value = sb.ToString();
        }

        private void ReadIdentifierOrKeyword()
        {
            ++_position;
            while (char.IsLetterOrDigit(Current) || Current == '_')
            {
                Next();
            }

            var length = _position - _start;
            var text = _text.ToString(_start, length);
            _kind = SyntaxFacts.GetKeywordKind(text);
        }

        private void ReadWhiteSpace()
        {
            var done = false;
            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                        done = true;
                        break;
                    case '\r':
                    case '\n':
                        done = true;
                        break;
                    default:
                        if (!char.IsWhiteSpace(Current))
                            done = true;
                        else
                            ++_position;
                        break;
                }
            }

            _kind = SyntaxKind.WhitespaceTrivia;
        }

        private void ReadLineBreak()
        {
            if (Current == '\r' && Lookahead == '\n')
            {
                _position += 2;
            }
            else
            {
                _position++;
            }

            _kind = SyntaxKind.LineBreakTrivia;
        }

        private void ReadNumberToken()
        {
            while (char.IsDigit(Current))
            {
                Next();
            }

            var length = _position - _start;
            var text = _text.ToString(_start, length);
            if (!int.TryParse(text, out var value))
            {
                var location = new TextLocation(_text, new TextSpan(_start, length));
                _diagnostics.ReportInvalidNumber(location, text, TypeSymbol.Int);
            }

            _kind = SyntaxKind.NumberToken;
            _value = value;
        }
    }
}