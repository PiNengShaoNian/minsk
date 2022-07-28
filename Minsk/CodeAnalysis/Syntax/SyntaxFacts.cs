namespace Minsk.CodeAnalysis.Syntax
{
    public static class SyntaxFacts
    {
        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PipePipeToken: //||
                    return 1;
                case SyntaxKind.AmpersandAmpersandToken: //&&
                    return 2;
                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                    return 3;
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 4;
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return 5;
                default:
                    return 0;
            }
        }

        public static int getUnaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.BangToken:
                    return 6;
                default:
                    return 0;
            }
        }

        public static SyntaxKind GetKeywordKind(string text)
        {
            switch (text)
            {
                case "true":
                    return SyntaxKind.TrueKeyword;
                case "false":
                    return SyntaxKind.FalseKeyword;
                default:
                    return SyntaxKind.IdentifierToken;
            }
        }

        public static string GetText(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                    return "+";
                case SyntaxKind.MinusToken: return "-";
                case SyntaxKind.StarToken: return "*";
                case SyntaxKind.SlashToken: return "/";
                case SyntaxKind.OpenParenthesisToken:
                    return "(";
                case SyntaxKind.CloseParenthesisToken:
                    return ")";
                case SyntaxKind.BangToken:
                    return "!";
                case SyntaxKind.AmpersandAmpersandToken:
                    return "&&";
                case SyntaxKind.PipePipeToken:
                    return "||";
                case SyntaxKind.EqualsEqualsToken:
                    return "==";
                case SyntaxKind.BangEqualsToken:
                    return "!=";
                case SyntaxKind.EqualsToken:
                    return "=";
                case SyntaxKind.TrueKeyword:
                    return "true";
                case SyntaxKind.FalseKeyword:
                    return "false";
                default:
                    return null;
            }
        }
    }
}