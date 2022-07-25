namespace Minsk.CodeAnalysis.Syntax
{
    internal static class SyntaxFacts
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
    }
}