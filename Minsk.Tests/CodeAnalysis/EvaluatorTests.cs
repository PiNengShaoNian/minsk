using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;

namespace Minsk.Tests.CodeAnalysis
{
    public class EvaluatorTests
    {
        [Theory]
        [InlineData("1", 1)]
        [InlineData("-1", -1)]
        [InlineData("~1", -2)]
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

        [InlineData("3 < 3", false)]
        [InlineData("3 < 4", true)]
        [InlineData("5 < 4", false)]
        [InlineData("3 <= 4", true)]
        [InlineData("3 <= 3", true)]
        [InlineData("3 <= 2", false)]
        [InlineData("3 > 2", true)]
        [InlineData("3 > 3", false)]
        [InlineData("3 > 4", false)]
        [InlineData("3 >= 4", false)]
        [InlineData("3 >= 3", true)]
        [InlineData("3 >= 2", true)]

        [InlineData("1 | 2", 3)]
        [InlineData("1 | 0", 1)]
        [InlineData("1 & 2", 0)]
        [InlineData("1 & 0", 0)]
        [InlineData("1 ^ 0", 1)]
        [InlineData("1 ^ 3", 2)]

        [InlineData("true == false", false)]
        [InlineData("true == true", true)]
        [InlineData("false == false", true)]
        [InlineData("false == true", false)]
        [InlineData("true != false", true)]
        [InlineData("true != true", false)]
        [InlineData("false != false", false)]
        [InlineData("false != true", true)]

        [InlineData("false | true", true)]
        [InlineData("true | false", true)]
        [InlineData("false | false", false)]
        [InlineData("true | true", true)]

        [InlineData("false & true", false)]
        [InlineData("true & false", false)]
        [InlineData("false & false", false)]
        [InlineData("true & true", true)]

        [InlineData("false ^ true", true)]
        [InlineData("true ^ false", true)]
        [InlineData("false ^ false", false)]
        [InlineData("true ^ true", false)]

        [InlineData("false && true", false)]
        [InlineData("true && false", false)]
        [InlineData("true && true", true)]
        [InlineData("false && false", false)]
        [InlineData("false || true", true)]
        [InlineData("true || false", true)]
        [InlineData("true || true", true)]
        [InlineData("false || false", false)]

        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("!true", false)]
        [InlineData("!false", true)]
        [InlineData("\"test\"", "test")]
        [InlineData("\"te\"\"st\"", "te\"st")]
        [InlineData("\"test\" == \"test\"", true)]
        [InlineData("\"test\" == \"test1\"", false)]
        [InlineData("\"test\" == \"abc\"", false)]

        [InlineData("var a = 10", 10)]
        [InlineData(@"{var a = 10
            a * a}", 100)]
        [InlineData(@"{
                           var a = 0
                           if a == 0
                              a = 100      
                           a
                      }", 100)]
        [InlineData(@"{
                           var a = 1
                           if a == 0
                              a = 100      
                           a
                      }", 1)]
        [InlineData(@"{
                           var a = 1
                           if a == 0
                              a = 100      
                           else 
                              a = 200
                           a
                      }", 200)]
        [InlineData(@"{
                          var a = 0
                          var i = 10
                          while i > 0
                          {
                              i = i - 1
                              a = a + 1
                          }
                          a
                      }", 10)]
        [InlineData(@"{
                          var a = 0
                          var i = 10
                          while i > 0
                          {
                              i = i - 2
                              a = a + 1
                          }
                          a
                      }", 5)]
        [InlineData(@"{
                          var a = 0
                          for i = 0 to 10
                          {
                              a = a + i
                          }
                          a
                      }", 55)]
        [InlineData(@"{
                          var a = 10
                          for i = 0 to (a = a - 1)
                          {
                          }
                          a
                      }", 9)]
        [InlineData(@"{ 
                          var a = 0 
                          do a = a + 1 
                          while a < 10
                          a
                      }", 10)]
        public void Evaluator_Computes_CorrectValues(string expression, object expectedValue)
        {
            AssertValue(expression, expectedValue);
        }

        private static void AssertValue(string expression, object expectedValue)
        {
            var syntaxTree = SyntaxTree.Parse(expression);
            var compilation = new Compilation(syntaxTree);
            var variables = new Dictionary<VariableSymbol, object>();

            var result = compilation.Evaluate(variables);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(expectedValue, result.Value);
        }

        [Fact]
        private void Evaluator_VariableDeclaration_Reports_Redeclaration()
        {
            var text = @"
                {
                    var x = 10
                    var y = 100
                    {
                        var x = 10
                    }
                    var [x] = 5
                }
             ";

            var diagnostics = @"
                Variable 'x' is already declared.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Variables_Can_Shadow_Functions()
        {
            var text = @"
                {
                    let print = 42
                    [print](""test"")
                }
            ";

            var diagnostics = @"
                Function 'print' doesn't exist.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_BlockStatement_Reports_NoInfiniteLoop()
        {
            var text = @"
                {
                [)][]
             ";

            var diagnostics = @"
                Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
                Unexpected token <EndOfFileToken>, expected <CloseBraceToken>.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_InvokeFunctionArguments_NoInfiniteLoop()
        {
            var text = @"
                print(""Hi""[[=]][)]
            ";

            var diagnostics = @"
                Unexpected token <EqualsToken>, expected <CloseParenthesisToken>.
                Unexpected token <EqualsToken>, expected <IdentifierToken>.
                Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_FunctionParameters_NoInfiniteLoop()
        {
            var text = @"
                function hi(name: string[[[=]]][)]
                {
                    print(""Hi "" + name + ""!"" )
                }[]
            ";

            var diagnostics = @"
                Unexpected token <EqualsToken>, expected <CloseParenthesisToken>.
                Unexpected token <EqualsToken>, expected <OpenBraceToken>.
                Unexpected token <EqualsToken>, expected <IdentifierToken>.
                Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
                Unexpected token <EndOfFileToken>, expected <CloseBraceToken>.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_NameExpression_Reports_Undefined()
        {
            var text = @"[x] * 10";

            var diagnostics = @"
                Variable 'x' doesn't exist.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_NameExpression_Reports_NoErrorForInsertedToken()
        {
            var text = @"1 + []";

            var diagnostics = @"
                   Unexpected token <EndOfFileToken>, expected <IdentifierToken>.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_AssignmentExpression_Reports_Undefined()
        {
            var text = @"[x] = 10";

            var diagnostics = @"
                Variable 'x' doesn't exist.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_AssignmentExpression_Reports_CannotAssign()
        {
            var text = @"
                {
                    let x = 10
                    x [=] 0
                }
                ";

            var diagnostics = @"
                Variable 'x' is read-only and cannot be assigned to.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_AssignmentExpression_Reports_CannotConvert()
        {
            var text = @"
                {
                    var x = 10
                    x = [true]
                }
                ";

            var diagnostics = @$"
                Cannot convert type '{TypeSymbol.Bool}' to '{TypeSymbol.Int}'.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_IfStatement_Reports_CannotConvert()
        {
            var text = @"
                {
                    var x = 0
                    if [10]
                        x = 10
                }
                ";

            var diagnostics = @$"
                Cannot convert type '{TypeSymbol.Int}' to '{TypeSymbol.Bool}'.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_WhileStatement_Reports_CannotConvert()
        {
            var text = @"
                {
                    var x = 0
                    while [10]
                        x = 10
                }
                ";

            var diagnostics = @$"
                Cannot convert type '{TypeSymbol.Int}' to '{TypeSymbol.Bool}'.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_ForStatement_Reports_CannotConvert_LowerBound()
        {
            var text = @"
                {
                    var x = 0
                    for i = [false] to 10
                        x = x + i
                }
                ";

            var diagnostics = @$"
                Cannot convert type '{TypeSymbol.Bool}' to '{TypeSymbol.Int}'.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_ForStatement_Reports_CannotConvert_UpperBound()
        {
            var text = @"
                {
                    var x = 0
                    for i = 0 to [true]
                        x = x + i
                }
                ";

            var diagnostics = @$"
                Cannot convert type '{TypeSymbol.Bool}' to '{TypeSymbol.Int}'.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_Unary_Reports_Undefined()
        {
            var text = @"[+]true";

            var diagnostics = @$"
                Unary operator '+' is not defined for type {TypeSymbol.Bool}.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_Binary_Reports_Undefined()
        {
            var text = @"10 [+] false";

            var diagnostics = @$"
                binary operator '+' is not defined for types {TypeSymbol.Int} and {TypeSymbol.Bool}.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        private void AssertDiagnostics(string text, string diagnosticText)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);
            var compilation = new Compilation(syntaxTree);
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());
            var expectedDiagnostics = AnnotatedText.UnidentLines(diagnosticText);
            if (annotatedText.Spans.Length != expectedDiagnostics.Length)
                throw new Exception("ERROR: Must mark as many spans as there are expected diagnostics");

            Assert.Equal(expectedDiagnostics.Length, result.Diagnostics.Length);

            Console.WriteLine(annotatedText.Text);

            for (var i = 0; i < expectedDiagnostics.Length; ++i)
            {
                var expectedMessage = expectedDiagnostics[i];
                var actualMessage = result.Diagnostics[i].Message;

                Assert.Equal(expectedMessage, actualMessage);

                var expectedSpan = annotatedText.Spans[i];
                var actualSpan = result.Diagnostics[i].Span;

                Assert.Equal(expectedSpan, actualSpan);
            }
        }
    }
}
