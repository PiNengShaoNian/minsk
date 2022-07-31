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
        IdentifierToken,
        BangToken,
        AmpersandAmpersandToken,
        PipePipeToken,
        EqualsEqualsToken,
        BangEqualsToken,
        EqualsToken,

        //Keyword
        TrueKeyword,
        FalseKeyword,

        //Expressions
        LiteralExpression,
        BinaryExpression,
        ParenthesisExpression,
        UnaryExpression,
        AssignmentExpression,
        NameExpression,

        //Nodes
        CompilationUnit,
    }
}