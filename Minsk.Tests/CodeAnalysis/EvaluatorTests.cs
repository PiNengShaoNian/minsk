using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Syntax;

namespace Minsk.Tests.CodeAnalysis
{
    public class EvaluatorTests
    {
        [Theory]
        [InlineData("1", 1)]
        [InlineData("-1", -1)]
        [InlineData("+1", 1)]
        [InlineData("1 + 2", 3)]
        [InlineData("1 - 2", -1)]
        [InlineData("1 * 2", 2)]
        [InlineData("1 / 2", 0)]
        [InlineData("(10)", 10)]
        [InlineData("12 == 3", false)]
        [InlineData("3 == 3", true)]
        [InlineData("12 != 3", true)]
        [InlineData("3 != 3", false)]
        [InlineData("true == false", false)]
        [InlineData("true == true", true)]
        [InlineData("false == false", true)]
        [InlineData("false == true", false)]
        [InlineData("true != false", true)]
        [InlineData("true != true", false)]
        [InlineData("false != false", false)]
        [InlineData("false != true", true)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("!true", false)]
        [InlineData("!false", true)]
        [InlineData("(a = 10) * a", 100)]
        public void SyntaxFact_GetText_RoundTrips(string expression, object expectedValue)
        {
            var syntaxTree = SyntaxTree.Parse(expression);
            var compilation = new Compilation(syntaxTree);
            var variables = new Dictionary<VariableSymbol, object>();

            var result = compilation.Evaluate(variables);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(expectedValue, result.Value);
        }
    }
}
