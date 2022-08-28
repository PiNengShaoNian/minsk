using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;

namespace Minsk.Tests.CodeAnalysis.Syntax
{
    public class LexerTests
    {
        [Fact]
        public void Lexer_Lexes_UnterminatedString()
        {
            var text = "\"text";
            var tokens = SyntaxTree.ParseTokens(text, out var diagnostics);
            var token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.StringToken, token.Kind);
            Assert.Equal(text, token.Text);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(new TextSpan(0, 1), diagnostic.Location.Span);
            Assert.Equal("Unterminated string literal.", diagnostic.Message);
        }
        [Fact]
        public void Lexer_Tests_CoversAllTokens()
        {
            var tokenKinds = Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Where(k => k.isToken());
            var testedTokenKinds = GetTokens().Concat(GetSeparators()).Select(t => t.kind);
            var untestedTokenKinds = new SortedSet<SyntaxKind>(tokenKinds);
            untestedTokenKinds.Remove(SyntaxKind.BadToken);
            untestedTokenKinds.Remove(SyntaxKind.EndOfFileToken);
            untestedTokenKinds.ExceptWith(testedTokenKinds);
            Assert.Empty(untestedTokenKinds);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("foo42")]
        [InlineData("foo_42")]
        [InlineData("_foo")]
        public void Lexer_Lexes_Identifiers(string name)
        {
            var tokens = SyntaxTree.ParseTokens(name).ToArray();

            Assert.Single(tokens);

            var token = tokens[0];
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind);
            Assert.Equal(name, token.Text);
        }

        [Theory]
        [MemberData(nameof(GetTokensData))]
        public void Lexer_lexes_Token(SyntaxKind kind, string text)
        {
            var tokens = SyntaxTree.ParseTokens(text);

            var token = Assert.Single(tokens);
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Theory]
        [MemberData(nameof(GetSeparatorsData))]
        public void Lexer_lexes_Separator(SyntaxKind kind, string text)
        {
            var tokens = SyntaxTree.ParseTokens(text, includeEndOfFile: true);

            var token = Assert.Single(tokens);
            var trivia = Assert.Single(token.LeadingTrivia);
            Assert.Equal(kind, trivia.Kind);
            Assert.Equal(text, trivia.Text);
        }

        [Theory]
        [MemberData(nameof(GetTokenPairsData))]
        public void Lexer_lexes_TokenPairs(SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text)
        {
            var text = t1Text + t2Text;
            var tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.Equal(2, tokens.Length);
            Assert.Equal(t1Kind, tokens[0].Kind);
            Assert.Equal(t1Text, tokens[0].Text);
            Assert.Equal(t2Kind, tokens[1].Kind);
            Assert.Equal(t2Text, tokens[1].Text);
        }

        [Theory]
        [MemberData(nameof(GetTokenPairsWithSeparatorData))]
        public void Lexer_lexes_TokenPairsWithSeparator(SyntaxKind t1Kind, string t1Text, SyntaxKind separatorKind, string separatorText, SyntaxKind t2Kind, string t2Text)
        {
            var text = t1Text + separatorText + t2Text;
            var tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.Equal(2, tokens.Length);
            Assert.Equal(t1Kind, tokens[0].Kind);
            Assert.Equal(t1Text, tokens[0].Text);

            var separator = Assert.Single(tokens[0].TrailingTrivia);
            Assert.Equal(separatorKind, separator.Kind);
            Assert.Equal(separatorText, separator.Text);

            Assert.Equal(t2Kind, tokens[1].Kind);
            Assert.Equal(t2Text, tokens[1].Text);
        }

        public static IEnumerable<object[]> GetTokensData()
        {
            foreach (var t in GetTokens())
            {
                yield return new object[] { t.kind, t.text };
            }
        }

        public static IEnumerable<object[]> GetSeparatorsData()
        {
            foreach (var t in GetSeparators())
            {
                yield return new object[] { t.kind, t.text };
            }
        }

        public static IEnumerable<object[]> GetTokenPairsData()
        {
            foreach (var t in GetTokenPairs())
            {
                yield return new object[] { t.t1Kind, t.t1Text, t.t2Kind, t.t2Text };
            }
        }

        public static IEnumerable<object[]> GetTokenPairsWithSeparatorData()
        {
            foreach (var t in GetTokenPairsWithSeparator())
            {
                yield return new object[] { t.t1Kind, t.t1Text, t.separatorKind, t.separatorText, t.t2Kind, t.t2Text };
            }
        }

        public static IEnumerable<(SyntaxKind kind, string text)> GetTokens()
        {
            var fixedTokens = Enum.GetValues(typeof(SyntaxKind)).Cast<SyntaxKind>()
                .Select(k => (kind: k, text: SyntaxFacts.GetText(k)))
                .Where(t => t.text != null);

            var dynamicTokens = new[]
            {
                (SyntaxKind.NumberToken, "323"),
                (SyntaxKind.NumberToken, "3"),
                (SyntaxKind.IdentifierToken,"a"),
                (SyntaxKind.IdentifierToken,"abc"),
                (SyntaxKind.StringToken, "\"Test\""),
                (SyntaxKind.StringToken, "\"Tes\"\"t\""),
            };

            return fixedTokens!.Concat(dynamicTokens);
        }

        public static IEnumerable<(SyntaxKind kind, string text)> GetSeparators()
        {
            return new[]
            {
                (SyntaxKind.WhitespaceTrivia, " "),
                (SyntaxKind.WhitespaceTrivia, "  "),
                (SyntaxKind.LineBreakTrivia, "\r"),
                (SyntaxKind.LineBreakTrivia, "\n"),
                (SyntaxKind.LineBreakTrivia, "\r\n"),
                (SyntaxKind.MultiLineCommentTrivia, "/* comment */"),
            };
        }

        public static IEnumerable<(SyntaxKind kind, string text)> GetSingleLineCommentSeparators()
        {
            return new[]
            {
                (SyntaxKind.WhitespaceTrivia, "\r"),
                (SyntaxKind.WhitespaceTrivia, "\n"),
                (SyntaxKind.WhitespaceTrivia, "\r\n"),
            };
        }

        private static bool RequiresSeparator(SyntaxKind t1Kind, SyntaxKind t2Kind)
        {
            var t1IsKeyword = t1Kind.isKeyWord();
            var t2IsKeyword = t2Kind.isKeyWord();

            if (t1Kind == SyntaxKind.IdentifierToken && t2Kind == SyntaxKind.IdentifierToken)
                return true;

            if (t1Kind == SyntaxKind.NumberToken && t2Kind == SyntaxKind.NumberToken)
                return true;

            if (t1IsKeyword && t2IsKeyword)
                return true;

            if (t1IsKeyword && t2Kind == SyntaxKind.IdentifierToken)
                return true;

            if (t1Kind == SyntaxKind.IdentifierToken && t2IsKeyword)
                return true;

            if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipeToken)
                return true;

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipePipeToken)
                return true;

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandToken)
                return true;

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandAmpersandToken)
                return true;

            if (t1Kind == SyntaxKind.StringToken && t2Kind == SyntaxKind.StringToken)
                return true;

            if ((t1Kind == SyntaxKind.IdentifierToken || t1IsKeyword) && t2Kind == SyntaxKind.NumberToken)
                return true;

            if (t1Kind == SyntaxKind.SlashToken
                && (t2Kind == SyntaxKind.StarToken || t2Kind == SyntaxKind.SlashToken || t2Kind == SyntaxKind.SingleLineCommentTrivia || t2Kind == SyntaxKind.MultiLineCommentTrivia))
                return true;

            if (t1Kind == SyntaxKind.SingleLineCommentTrivia)
                return true;

            return false;
        }

        public static IEnumerable<(SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text)> GetTokenPairs()
        {
            foreach (var t1 in GetTokens())
            {
                foreach (var t2 in GetTokens())
                {
                    if (!RequiresSeparator(t1.kind, t2.kind))
                        yield return (t1.kind, t1.text, t2.kind, t2.text);
                }
            }
        }

        public static IEnumerable<(
            SyntaxKind t1Kind, string t1Text,
            SyntaxKind separatorKind, string separatorText,
            SyntaxKind t2Kind, string t2Text)> GetTokenPairsWithSeparator()
        {

            foreach (var t1 in GetTokens())
            {
                var separators = t1.kind == SyntaxKind.SingleLineCommentTrivia ? GetSingleLineCommentSeparators()
                       : GetSeparators();
                foreach (var t2 in GetTokens())
                {
                    if (RequiresSeparator(t1.kind, t2.kind))
                    {
                        foreach (var s in separators)
                        {
                            if (!RequiresSeparator(t1.kind, s.kind) && !RequiresSeparator(s.kind, t2.kind))
                                yield return (t1.kind, t1.text, s.kind, s.text, t2.kind, t2.text);
                        }
                    }
                }
            }
        }
    }
}