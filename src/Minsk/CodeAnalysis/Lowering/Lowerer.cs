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
            var name = $"Label_{++_labelCount}";
            return new BoundLabel(name);
        }

        private Lowerer()
        {
        }

        private static BoundBlockStatement Flatten(FunctionSymbol function, BoundStatement statement)
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

            if (function.Type == TypeSymbol.Void)
            {
                if (builder.Count == 0 || CanllFallThrough(builder.Last()))
                {
                    builder.Add(new BoundReturnStatement(null));
                }
            }

            return new BoundBlockStatement(builder.ToImmutable());
        }

        private static bool CanllFallThrough(BoundStatement boundStatement)
        {
            // TODO: We don't rewrite conditional gotos where the condition is always true.
            //       We shouldn't handle this here, because we should really rewrite those to 
            //       unconditional gotos in the first place.
            return boundStatement.Kind != BoundNodeKind.ReturnStatement && boundStatement.Kind != BoundNodeKind.GotoStatement;
        }

        public static BoundBlockStatement Lower(FunctionSymbol function, BoundStatement statement)
        {
            var lowerer = new Lowerer();
            var result = lowerer.RewriteStatement(statement);
            return RemoveDeadCode(Flatten(function, result));
        }

        public static BoundBlockStatement RemoveDeadCode(BoundBlockStatement node)
        {
            var controlFlow = ControlFlowGraph.Create(node);
            var reachableStatements = new HashSet<BoundStatement>(controlFlow.Blocks.SelectMany(b => b.Statements));

            var builder = node.Statements.ToBuilder();
            for (var i = builder.Count - 1; i >= 0; --i)
            {
                if (!reachableStatements.Contains(builder[i]))
                    builder.RemoveAt(i);
            }

            return new BoundBlockStatement(builder.ToImmutable());
        }

        protected override BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            // do
            // <body>
            // while <condition>
            //
            // --->
            // continue:
            // <body>
            // check: 
            // ifTrue <condition> goto continue
            // break: 

            var breakLabel = node.BreakLabel;
            var continueLabel = GenerateLabel();
            var checkLabel = node.ContinueLabel;

            var bodyStatement = RewriteStatement(node.Body);
            var condition = RewriteExpression(node.Condition);
            var continueLabelStatement = new BoundLabelStatement(continueLabel);
            var checkLabelStatement = new BoundLabelStatement(checkLabel);
            var gotoContinueStatement = new BoundConditionalGotoStatement(continueLabel, condition, true);
            var breakLabelStatement = new BoundLabelStatement(breakLabel);

            var statements = ImmutableArray.Create(
                continueLabelStatement,
                bodyStatement,
                checkLabelStatement,
                gotoContinueStatement,
                breakLabelStatement);

            return new BoundBlockStatement(statements);
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
            // continue:
            // gotoFalse <condition> break
            // <body>
            // goto continue
            // break:

            var continueLabel = node.ContinueLabel;
            var breakLabel = node.BreakLabel;

            var continueLabelStatement = new BoundLabelStatement(continueLabel);
            var gotoFalseStatement = new BoundConditionalGotoStatement(breakLabel, node.Condition, false);
            var gotoContinueStatement = new BoundGotoStatement(continueLabel);
            var breakLabelStatement = new BoundLabelStatement(breakLabel);

            var result = new BoundBlockStatement(
                    ImmutableArray.Create<BoundStatement>(
                        continueLabelStatement,
                        gotoFalseStatement,
                        node.Body,
                        gotoContinueStatement,
                        breakLabelStatement
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
            //         continue:                      
            //         <var> = <var> + 1
            //      }
            // }

            var lowerBoundVariableDeclaration = new BoundVariableDeclaration(node.Variable, node.LowerBound);
            var lowerBoundVariableExpression = new BoundVariableExpression(node.Variable);
            var upperBoundVarialbe = new LocalVariableSymbol("uppderBound", true, TypeSymbol.Int, node.UpperBound.ConstantValue);
            var upperBoundVarialbeDeclaration = new BoundVariableDeclaration(upperBoundVarialbe, node.UpperBound);
            var condition = new BoundBinaryExpression(
                    lowerBoundVariableExpression,
                    BoundBinaryOperator.Bind(SyntaxKind.LessOrEqualsToken, TypeSymbol.Int, TypeSymbol.Int),
                    new BoundVariableExpression(upperBoundVarialbe)
                );
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
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
            var whileBlock = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(
                node.Body,
                continueLabelStatement,
                increment));
            var whileStatement = new BoundWhileStatement(condition, whileBlock, node.BreakLabel, GenerateLabel());
            var result = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(
                    lowerBoundVariableDeclaration,
                    upperBoundVarialbeDeclaration,
                    whileStatement
                    )
                );

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            if (node.Condition.ConstantValue != null)
            {
                var condition = (bool)node.Condition.ConstantValue.Value;
                condition = node.JumpIfTrue ? condition : !condition;

                if (condition)
                    return new BoundGotoStatement(node.Label);
                else
                    return new BoundNopStatement();
            }

            return base.RewriteConditionalGotoStatement(node);
        }
    }
}
