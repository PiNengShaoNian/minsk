using Minsk.CodeAnalysis.Binding;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;
using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Lowering
{
    internal class Lowerer : BoundTreeRewriter
    {
        private int _labelCount = 0;

        private BoundLabel GenerateLabel()
        {
            var name = $"Label{_labelCount}";

            ++_labelCount;
            return new BoundLabel(name);
        }

        private Lowerer()
        {

        }

        private static BoundBlockStatement Flatten(BoundStatement statement)
        {
            var builder = ImmutableArray.CreateBuilder<BoundStatement>();
            var stack = new Stack<BoundStatement>();
            stack.Push(statement);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current is BoundBlockStatement block)
                {
                    foreach (var s in block.Statements.Reverse())
                    {
                        stack.Push(s);
                    }
                }
                else
                {
                    builder.Add(current);
                }
            }

            return new BoundBlockStatement(builder.ToImmutable());
        }

        public static BoundBlockStatement Lower(BoundStatement statement)
        {
            var lowerer = new Lowerer();
            var result = lowerer.RewriteStatement(statement);
            return Flatten(result);
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            if (node.ElseStatement == null)
            {
                // if <condition>
                //    <then>
                //
                //  ---->
                //
                // gotoIfFalse <condition> end
                // <then>
                // end:
                var endLabel = GenerateLabel();
                var gotoFalse = new BoundConditionalGotoStatement(endLabel, node.Condition, false);
                var endLabelStatement = new BoundLabelStatement(endLabel);

                var result = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(gotoFalse, node.ThenStatement, endLabelStatement));
                return RewriteStatement(result);
            }
            else
            {
                // if <condition>
                //    <then>
                // else
                //    <else>
                //
                //  ---->
                //
                // gotoIfFalse <condition> else
                // <then>
                // goto end
                // else:
                // <else>
                // end:
                //
                var elseLabel = GenerateLabel();
                var endLabel = GenerateLabel();
                var gotoFalse = new BoundConditionalGotoStatement(elseLabel, node.Condition, false);
                var gotoEndStatement = new BoundGotoStatement(endLabel);
                var elseLabelStatement = new BoundLabelStatement(elseLabel);
                var endLabelStatement = new BoundLabelStatement(endLabel);

                var result = new BoundBlockStatement(
                        ImmutableArray.Create<BoundStatement>(
                            gotoFalse,
                            node.ThenStatement,
                            gotoEndStatement,
                            elseLabelStatement,
                            node.ElseStatement,
                            endLabelStatement
                        )
                    );

                return RewriteStatement(result);
            }
        }

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            // while <condition>
            //     <body>
            // 
            // ---->
            //
            // check:
            // gotoFalse <condition> end
            // <body>
            // goto check
            // end:

            var checkLabel = GenerateLabel();
            var endLabel = GenerateLabel();
            var checkLabelStatement = new BoundLabelStatement(checkLabel);
            var gotoFalseStatement = new BoundConditionalGotoStatement(endLabel, node.Condition, false);
            var gotoCheckStatement = new BoundGotoStatement(checkLabel);
            var endLabelStatement = new BoundLabelStatement(endLabel);

            var result = new BoundBlockStatement(
                    ImmutableArray.Create<BoundStatement>(
                        checkLabelStatement,
                        gotoFalseStatement,
                        node.Body,
                        gotoCheckStatement,
                        endLabelStatement
                    )
                );

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            // for <var> = <lower> to <upper>
            //    <body>
            // --->
            // {
            //     var <var> = <lower>
            //     while(<var> <= <upper>)
            //     {
            //         <body>
            //         <var> = <var> + 1
            //      }
            // }

            var lowerBoundVariableDeclaration = new BoundVariableDeclaration(node.Variable, node.LowerBound);
            var lowerBoundVariableExpression = new BoundVariableExpression(node.Variable);
            var upperBoundVarialbe = new VariableSymbol("uppderBound", true, TypeSymbol.Int);
            var upperBoundVarialbeDeclaration = new BoundVariableDeclaration(upperBoundVarialbe, node.UpperBound);
            var condition = new BoundBinaryExpression(
                    lowerBoundVariableExpression,
                    BoundBinaryOperator.Bind(SyntaxKind.LessToken, TypeSymbol.Int, TypeSymbol.Int),
                    new BoundVariableExpression(upperBoundVarialbe)
                );
            var increment = new BoundExpressionStatement(
                    new BoundAssignmentExpression(
                        node.Variable,
                        new BoundBinaryExpression(
                            lowerBoundVariableExpression,
                            BoundBinaryOperator.Bind(SyntaxKind.PlusToken, TypeSymbol.Int, TypeSymbol.Int),
                            new BoundLiteralExpression(1)
                        )
                    )
                );
            var whileBlock = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(node.Body, increment));
            var whileStatement = new BoundWhileStatement(condition, whileBlock);
            var result = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(
                    lowerBoundVariableDeclaration, 
                    upperBoundVarialbeDeclaration, 
                    whileStatement
                    )
                );

            return RewriteStatement(result);
        }
    }
}
