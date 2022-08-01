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
        OpenBraceToken,
        CloseBraceToken,

        //Keyword
        TrueKeyword,
        FalseKeyword,
        LetKeyword,
        VarKeyword,

        //Nodes
        CompilationUnit,

        //Statements
        BlockStatement,
        VariableDeclaration,
        ExpressionStatement,

        //Expressions
        LiteralExpression,
        BinaryExpression,
        ParenthesisExpression,
        UnaryExpression,
        AssignmentExpression,
        NameExpression,
    }
}