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
        AmpersandToken,
        AmpersandAmpersandToken,
        PipeToken,
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
        TildeToken,
        HatToken,
        StringToken,
        Comma,

        //Keyword
        TrueKeyword,
        FalseKeyword,
        LetKeyword,
        VarKeyword,
        IfKeyword,
        ElseKeyword,
        WhileKeyword,
        ForKeyword,
        ToKeyword,

        //Nodes
        CompilationUnit,
        ElseClause,

        //Statements
        BlockStatement,
        VariableDeclaration,
        ExpressionStatement,
        IfStatement,
        WhileStatement,
        ForStatement,

        //Expressions
        LiteralExpression,
        BinaryExpression,
        ParenthesisExpression,
        UnaryExpression,
        AssignmentExpression,
        NameExpression,
        CallExpression,
    }
}