using Minsk.CodeAnalysis.Symbols;

namespace Minsk.CodeAnalysis.Binding
{
    internal static class ConstantFolding
    {
        public static BoundConstant? ComputeConstant(BoundUnaryOperator op, BoundExpression operand)
        {
            if (operand.ConstantValue != null)
            {
                var value = operand.ConstantValue.Value;
                switch (op.Kind)
                {
                    case BoundUnaryOperatorKind.Negation:
                        return new BoundConstant(-((int)value));
                    case BoundUnaryOperatorKind.Identity:
                        return new BoundConstant(((int)value));
                    case BoundUnaryOperatorKind.LogicalNegation:
                        return new BoundConstant(!((bool)value));
                    case BoundUnaryOperatorKind.OnesComplement:
                        return new BoundConstant(~((int)value));
                    default:
                        throw new Exception($"Unpexted unary operator ${op.Kind}");
                }
            }

            return null;
        }

        public static BoundConstant? ComputeConstant(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            var leftConstant = left.ConstantValue;
            var rightConstant = right.ConstantValue;

            // Special case && and || because they only need one side to be known.
            if (op.Kind == BoundBinaryOperatorKind.LogicalAnd)
            {
                if (leftConstant != null && !(bool)leftConstant.Value ||
                    rightConstant != null && !((bool)rightConstant.Value))
                    return new BoundConstant(false);
            }

            if (op.Kind == BoundBinaryOperatorKind.LogicalOr)
            {
                if (leftConstant != null && (bool)leftConstant.Value ||
                    rightConstant != null && ((bool)rightConstant.Value))
                    return new BoundConstant(true);
            }

            if (leftConstant == null || rightConstant == null)
                return null;

            var leftValue = leftConstant.Value;
            var rightValue = rightConstant.Value;

            switch (op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    if (left.Type == TypeSymbol.Int)
                        return new BoundConstant((int)leftValue + (int)rightValue);
                    else
                        return new BoundConstant((string)leftValue + (string)rightValue);
                case BoundBinaryOperatorKind.Subtraction:
                    return new BoundConstant((int)leftValue - (int)rightValue);
                case BoundBinaryOperatorKind.Multiplication:
                    return new BoundConstant((int)leftValue * (int)rightValue);
                case BoundBinaryOperatorKind.Division:
                    return new BoundConstant((int)leftValue / (int)rightValue);
                case BoundBinaryOperatorKind.LogicalAnd:
                    return new BoundConstant((bool)leftValue && (bool)rightValue);
                case BoundBinaryOperatorKind.LogicalOr:
                    return new BoundConstant((bool)leftValue || (bool)rightValue);
                case BoundBinaryOperatorKind.Equals:
                    return new BoundConstant(Equals(leftValue, rightValue));
                case BoundBinaryOperatorKind.NotEquals:
                    return new BoundConstant(!Equals(leftValue, rightValue));
                case BoundBinaryOperatorKind.Less:
                    return new BoundConstant((int)leftValue < (int)rightValue);
                case BoundBinaryOperatorKind.LessOrEquals:
                    return new BoundConstant((int)leftValue <= (int)rightValue);
                case BoundBinaryOperatorKind.Greater:
                    return new BoundConstant((int)leftValue > (int)rightValue);
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return new BoundConstant((int)leftValue >= (int)rightValue);
                case BoundBinaryOperatorKind.BitwiseAnd:
                    if (left.Type == TypeSymbol.Int)
                        return new BoundConstant((int)leftValue & (int)rightValue);
                    else
                        return new BoundConstant((bool)leftValue & (bool)rightValue);
                case BoundBinaryOperatorKind.BitwiseOr:
                    if (left.Type == TypeSymbol.Int)
                        return new BoundConstant((int)leftValue | (int)rightValue);
                    else
                        return new BoundConstant((bool)leftValue | (bool)rightValue);
                case BoundBinaryOperatorKind.BitwiseXor:
                    if (left.Type == TypeSymbol.Int)
                        return new BoundConstant((int)leftValue ^ (int)rightValue);
                    else
                        return new BoundConstant((bool)leftValue ^ (bool)rightValue);
                default:
                    throw new Exception($"Unpexted binary operator ${op.Kind}");
            }
        }
    }
}
