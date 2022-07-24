using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Binding;

namespace Minsk.CodeAnalysis
{

    internal sealed class Evaluator
    {
        private readonly BoundExpression _root;
        public Evaluator(BoundExpression root)
        {
            _root = root;
        }

        public object Evaluate()
        {
            return EvaluateExpression(_root);
        }

        private object EvaluateExpression(BoundExpression node)
        {
            if (node is BoundLiteralExpression n)
            {
                return n.Value;
            }

            if (node is BoundUnaryExpression u)
            {
                var operand = (int)EvaluateExpression(u.Operand);
                if (u.OperatorKind == BoundUnaryOperatorKind.Negation)
                {
                    return -operand;
                }
                else if (u.OperatorKind == BoundUnaryOperatorKind.Identity)
                {
                    return operand;
                }

                throw new Exception($"Unpexted unary operator ${u.OperatorKind}");
            }

            if (node is BoundBinaryExpression b)
            {
                var left = (int)EvaluateExpression(b.Left);
                var right = (int)EvaluateExpression(b.Right);

                switch (b.OperatorKind)
                {
                    case BoundBinaryOperatorKind.Addition:
                        return left + right;
                    case BoundBinaryOperatorKind.Subtraction:
                        return left - right;
                    case BoundBinaryOperatorKind.Multiplication:
                        return left * right;
                    case BoundBinaryOperatorKind.Division:
                        return left / right;
                    default:
                        throw new Exception($"Unpexted binary operator ${b.OperatorKind}");
                }
            }


            throw new Exception($"Unpexted node ${node.Kind}");
        }
    }
}