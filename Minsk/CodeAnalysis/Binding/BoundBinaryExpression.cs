namespace Minsk.CodeAnalysis.Binding
{

    internal sealed class BoundBinaryExpression : BoundExpression
    {
        public BoundBinaryExpression(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            Left = left;
            Op = op;
            Right = right;
        }

        public override Type Type => Op.Type;

        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;

        public BoundExpression Right { get; }
        public BoundExpression Left { get; }
        public BoundBinaryOperator Op { get; }
    }

}
