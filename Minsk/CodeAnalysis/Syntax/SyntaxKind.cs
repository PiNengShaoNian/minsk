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
        GreaterToken,
        GreaterOrEqualsToken,
        LessOrEqualsToken,
        LessToken,

        //Keyword
        TrueKeyword,
        FalseKeyword,
        LetKeyword,
        VarKeyword,
        IfKeyword,
        ElseKeyword,
        WhileKeyword,

        //Nodes
        CompilationUnit,
        ElseClause,

        //Statements
        BlockStatement,
        VariableDeclaration,
        ExpressionStatement,
        IfStatement,
        WhileStatement,

        //Expressions
        LiteralExpression,
        BinaryExpression,
        ParenthesisExpression,
        UnaryExpression,
        AssignmentExpression,
        NameExpression,
    }
}