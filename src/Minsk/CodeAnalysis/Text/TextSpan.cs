namespace Minsk.CodeAnalysis.Text
{
    public struct TextSpan
    {
        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        public static TextSpan FromBounds(int start, int end)
        {
            return new TextSpan(start, end - start);
        }

        //  Case 1
        //   [-------]
        //       [----------]
        //
        //  Case 4
        //   [--------------]
        //      [----------]
        //
        public bool OverlapsWith(TextSpan span)
        {
            var left = span.Start < this.Start ? span : this;
            var right = span.Start > this.Start ? span : this;

            if (left.End > right.Start)
                return true;

            return false;
        }

        public override string ToString() => $"{Start}..{End}";
    }
}