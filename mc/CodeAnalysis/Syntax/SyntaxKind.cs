namespace Minsk.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        //Tokens
        BadToken,
        EndOfFileToken,
        WhitespaceToken,
        NumberToken,
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        OpenParenthesisToken,
        CloseParenthesisToken,
        
        //Keyword
        TrueKeyword,
        FalseKeyword,

        //Expressions
        LiteralExpression,
        BinaryExpression,
        ParenthesisExpression,
        UnaryExpression,
        IdentifierToken,
    }
}