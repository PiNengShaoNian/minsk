﻿using Minsk.CodeAnalysis;
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
        [InlineData(@"{var a = 10
            a * a}", 100)]
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
        private void Evaluator_Name_Reports_Undefined()
        {
            var text = @"[x] * 10";

            var diagnostics = @"
                Variable 'x' dosn't exist.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_Assigned_Reports_Undefined()
        {
            var text = @"[x] = 10";

            var diagnostics = @"
                Variable 'x' dosn't exist.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_Assigned_Reports_CannotAssign()
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
        private void Evaluator_Assigned_Reports_CannotConvert()
        {
            var text = @"
                {
                    var x = 10
                    x = [true]
                }
                ";

            var diagnostics = @$"
                Cannot convert type '{typeof(bool)}' to '{typeof(int)}'.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_Unary_Reports_Undefined()
        {
            var text = @"[+]true";

            var diagnostics = @$"
                Unary operator '+' is not defined for type {typeof(bool)}.
                ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        private void Evaluator_Binary_Reports_Undefined()
        {
            var text = @"10 [+] false";

            var diagnostics = @$"
                binary operator '+' is not defined for types {typeof(int)} and {typeof(bool)}.
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
