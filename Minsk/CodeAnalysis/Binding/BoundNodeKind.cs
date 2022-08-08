namespace Minsk.CodeAnalysis.Binding
{
    internal enum BoundNodeKind
    {
        //Statements
        BlockStatement,
        VariableDeclaration,
        ExpressionStatement,
        IfStatement,
        WhileStatement,
        ForStatement,
        GotoStatement,
        LabelStatement,
        ConditionalGotoStatement,

        //Expressions
        UnaryExpression,
        LiteralExpression,
        BinaryExpression,
        VariableExpression,
        AssignmentExpression,
        ErrorExpression,
        CallExpression,
        ConversionExpression,
    }
}
