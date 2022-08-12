using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;
using System.Collections;

namespace Minsk.CodeAnalysis
{
    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public IEnumerator<Diagnostic> GetEnumerator()
        {
            return ((IEnumerable<Diagnostic>)_diagnostics).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_diagnostics).GetEnumerator();
        }

        public void AddRange(DiagnosticBag diagnostics)
        {
            _diagnostics.AddRange(diagnostics._diagnostics);
        }


        private void Report(TextSpan span, string message)
        {
            var diagnostic = new Diagnostic(span, message);
            _diagnostics.Add(diagnostic);
        }

        public void ReportInvalidNumber(TextSpan textSpan, string text, TypeSymbol type)
        {
            var message = $"The number {text} isn't valid {type}.";
            Report(textSpan, message);
        }


        public void ReportBadCharacter(int position, char current)
        {
            var message = $"bad character input: '{current}'.";
            Report(new TextSpan(position, 1), message);
        }

        public void ReportUnexpectedToken(TextSpan span, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            var message = $"Unexpected token <{actualKind}>, expected <{expectedKind}>.";
            Report(span, message);
        }

        internal void ReportUndefinedBinaryOperator(TextSpan span, string operatorText, TypeSymbol leftType, TypeSymbol rightType)
        {
            var message = $"binary operator '{operatorText}' is not defined for types {leftType} and {rightType}.";
            Report(span, message);
        }

        internal void ReportUndefinedUnaryOperator(TextSpan span, string opText, TypeSymbol operandType)
        {
            var message = $"Unary operator '{opText}' is not defined for type {operandType}.";
            Report(span, message);
        }

        public void ReportUndefinedName(TextSpan span, string name)
        {
            var message = $"Variable '{name}' doesn't exist.";
            Report(span, message);
        }

        internal void ReportSymoblAlreadyDeclared(TextSpan span, string parameterName)
        {
            var message = $"'{parameterName}' is already declared.";
            Report(span, message);
        }

        internal void ReportParameterAlreadyDeclared(TextSpan span, string parameterName)
        {
            var message = $"A parameter with the name '{parameterName}' is already exists.";
            Report(span, message);
        }

        internal void ReportCannotConvert(TextSpan span, TypeSymbol fromType, TypeSymbol toType)
        {
            var message = $"Cannot convert type '{fromType}' to '{toType}'.";
            Report(span, message);
        }

        internal void ReportVariableAlreadyDeclared(TextSpan span, string name)
        {
            var message = $"Variable '{name}' is already declared.";
            Report(span, message);
        }

        internal void ReportCannotAssign(TextSpan span, string name)
        {
            var message = $"Variable '{name}' is read-only and cannot be assigned to.";
            Report(span, message);
        }

        public void ReportUnterminatedString(TextSpan span)
        {
            var message = $"Unterminated string literal.";
            Report(span, message);
        }

        internal void ReportUndefinedFunction(TextSpan span, string name)
        {
            var message = $"Function '{name}' doesn't exist.";
            Report(span, message);
        }

        internal void ReportWrongArgumentCount(TextSpan span, string name, int expectedCount, int actualCount)
        {
            var message = $"Function '{name}' request {expectedCount} arguments but was given {actualCount}.";
            Report(span, message);
        }

        internal void ReportWrongArgumentType(TextSpan span, string name, TypeSymbol expectedType, TypeSymbol actualType)
        {
            var message = $"Parameter '{name}' requires value of type '{expectedType}' but was given '{actualType}'.";
            Report(span, message);
        }

        internal void ReportExpressionMustHaveValue(TextSpan span)
        {
            var message = "Expression must have a value.";
            Report(span, message);
        }

        internal void ReportUndefinedType(TextSpan span, string typeName)
        {
            var message = $"Type '{typeName}' doesn't exist.";
            Report(span, message);
        }

        internal void ReportCannotConvertImplicitly(TextSpan diagnosticSpan, TypeSymbol fromType, TypeSymbol toType)
        {
            var message = $"Cannot convert type '{fromType}' to '{toType}'. An explicit conversion exists (are you missing a cast?)";
            Report(diagnosticSpan, message);
        }

        internal void XXX_ReportFunctionsAreUnsupported(TextSpan span)
        {
            var message = "Functions with return values are unsupported.";
            Report(span, message);
        }

        internal void ReportInvalidBreakOrContinue(TextSpan span, string text)
        {
            var message = $"The keyword {text} is only be used inside of loops.";
            Report(span, message);
        }
    }
}