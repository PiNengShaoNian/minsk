using Minsk.CodeAnalysis.Symbols;

namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundBinaryExpression : BoundExpression
    {
        public BoundBinaryExpression(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            Left = left;
            Op = op;
            Right = right;
            ConstantValue = ConstantFolding.ComputeConstant(left, op, right);
        }

        public override TypeSymbol Type => Op.Type;

        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;

        public BoundExpression Left { get; }
        public BoundExpression Right { get; }
        public BoundBinaryOperator Op { get; }
        public override BoundConstant ConstantValue { get; }
    }
}
