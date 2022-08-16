using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Text
{
    public sealed class SourceText
    {
        public ImmutableArray<TextLine> Lines { get; }
        private readonly string _text;

        private SourceText(string text, string fileName)
        {
            _text = text;
            FileName = fileName;
            Lines = ParseLines(this, text);
        }

        public char this[int index] => _text[index];
        public int Length => _text.Length;

        public string FileName { get; }

        public int GetLineIndex(int position)
        {
            int l = 0;
            int r = Lines.Length - 1;

            while (l <= r)
            {
                int mid = l + ((r - l) >> 1);
                var line = Lines[mid];
                if (line.Start > position)
                {
                    r = mid - 1;
                }
                else if (line.End < position)
                {
                    l = mid + 1;
                }
                else if (line.End >= position && line.Start <= position)
                {
                    return mid;
                }
            }

            return -1;
        }

        private static ImmutableArray<TextLine> ParseLines(SourceText sourceText, string text)
        {
            var result = ImmutableArray.CreateBuilder<TextLine>();
            var lineStart = 0;
            var position = 0;
            while (position < text.Length)
            {
                var lineBreakWidth = GetLineBreakWidth(text, position);

                if (lineBreakWidth == 0)
                {
                    ++position;
                }
                else
                {
                    AddLine(result, sourceText, lineStart, position, lineBreakWidth);
                    position += lineBreakWidth;
                    lineStart = position;
                }
            }

            if (position >= lineStart)
                AddLine(result, sourceText, lineStart, position, 0);

            return result.ToImmutable();
        }

        private static void AddLine(ImmutableArray<TextLine>.Builder resut, SourceText sourceText, int lineStart, int position, int lineBreakWidth)
        {
            var lineLength = position - lineStart;
            var lineLengthIncludingLineBreak = lineLength + lineBreakWidth;
            var line = new TextLine(sourceText, lineStart, lineLength, lineLengthIncludingLineBreak);
            resut.Add(line);
        }

        private static int GetLineBreakWidth(string text, int i)
        {
            var c = text[i];
            var l = i + 1 >= text.Length ? '\0' : text[i + 1];
            if (c == '\r' && l == '\n')
            {
                return 2;
            }

            if (c == '\r' || c == '\n')
            {
                return 1;
            }

            return 0;
        }

        public static SourceText From(string text, string fileName = "")
        {
            return new SourceText(text, fileName);
        }

        public override string ToString() => _text;

        public string ToString(int start, int length) => _text.Substring(start, length);

        public string ToString(TextSpan span) => _text.Substring(span.Start, span.Length);
    }
}
