﻿namespace Minsk.CodeAnalysis.Syntax
{
    public sealed class ReturnStatementSyntax : StatementSyntax
    {
        public ReturnStatementSyntax(SyntaxToken returnKeyword, ExpressionSyntax expression)
        {
            ReturnKeyword = returnKeyword;
            Expression = expression;
        }


        public SyntaxToken ReturnKeyword { get; }
        public ExpressionSyntax Expression { get; }

        public override SyntaxKind Kind => SyntaxKind.ReturnStatement;
    }
}