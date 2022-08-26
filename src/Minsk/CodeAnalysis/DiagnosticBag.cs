using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;
using Mono.Cecil;
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

        public void AddRange(IEnumerable<Diagnostic> diagnostics)
        {
            _diagnostics.AddRange(diagnostics);
        }


        private void Report(TextLocation location, string message)
        {
            var diagnostic = new Diagnostic(location, message);
            _diagnostics.Add(diagnostic);
        }

        public void ReportInvalidNumber(TextLocation location, string text, TypeSymbol type)
        {
            var message = $"The number {text} isn't valid {type}.";
            Report(location, message);
        }


        public void ReportBadCharacter(TextLocation location, char current)
        {
            var message = $"bad character input: '{current}'.";
            Report(location, message);
        }

        public void ReportUnexpectedToken(TextLocation location, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            var message = $"Unexpected token <{actualKind}>, expected <{expectedKind}>.";
            Report(location, message);
        }

        internal void ReportUndefinedBinaryOperator(TextLocation location, string operatorText, TypeSymbol leftType, TypeSymbol rightType)
        {
            var message = $"binary operator '{operatorText}' is not defined for types {leftType} and {rightType}.";
            Report(location, message);
        }

        internal void ReportUndefinedUnaryOperator(TextLocation location, string opText, TypeSymbol operandType)
        {
            var message = $"Unary operator '{opText}' is not defined for type {operandType}.";
            Report(location, message);
        }

        public void ReportUndefinedVariable(TextLocation location, string name)
        {
            var message = $"Variable '{name}' doesn't exist.";
            Report(location, message);
        }

        internal void ReportSymbolAlreadyDeclared(TextLocation location, string parameterName)
        {
            var message = $"'{parameterName}' is already declared.";
            Report(location, message);
        }

        internal void ReportParameterAlreadyDeclared(TextLocation location, string parameterName)
        {
            var message = $"A parameter with the name '{parameterName}' already exists.";
            Report(location, message);
        }

        internal void ReportCannotConvert(TextLocation location, TypeSymbol fromType, TypeSymbol toType)
        {
            var message = $"Cannot convert type '{fromType}' to '{toType}'.";
            Report(location, message);
        }

        internal void ReportAllPathsMustReturn(TextLocation location)
        {
            var message = "Not all code paths return a value.";
            Report(location, message);
        }

        internal void ReportVariableAlreadyDeclared(TextLocation location, string name)
        {
            var message = $"Variable '{name}' is already declared.";
            Report(location, message);
        }

        internal void ReportCannotAssign(TextLocation location, string name)
        {
            var message = $"Variable '{name}' is read-only and cannot be assigned to.";
            Report(location, message);
        }

        public void ReportUnterminatedString(TextLocation location)
        {
            var message = $"Unterminated string literal.";
            Report(location, message);
        }

        internal void ReportUndefinedFunction(TextLocation location, string name)
        {
            var message = $"Function '{name}' doesn't exist.";
            Report(location, message);
        }

        internal void ReportWrongArgumentCount(TextLocation location, string name, int expectedCount, int actualCount)
        {
            var message = $"Function '{name}' requires {expectedCount} arguments but was given {actualCount}.";
            Report(location, message);
        }

        internal void ReportExpressionMustHaveValue(TextLocation location)
        {
            var message = "Expression must have a value.";
            Report(location, message);
        }

        internal void ReportUndefinedType(TextLocation location, string typeName)
        {
            var message = $"Type '{typeName}' doesn't exist.";
            Report(location, message);
        }

        internal void ReportCannotConvertImplicitly(TextLocation location, TypeSymbol fromType, TypeSymbol toType)
        {
            var message = $"Cannot convert type '{fromType}' to '{toType}'. An explicit conversion exists (are you missing a cast?).";
            Report(location, message);
        }

        internal void ReportInvalidBreakOrContinue(TextLocation location, string text)
        {
            var message = $"The keyword '{text}' can only be used inside of loops.";
            Report(location, message);
        }

        internal void ReportInvalidReturnExpression(TextLocation location, string functionName)
        {
            var message = $"Since the function '{functionName}' does not return a value the 'return' keyword cannot be followed by an expression.";
            Report(location, message);
        }

        internal void ReportMissingReturnExpression(TextLocation location, TypeSymbol returnType)
        {
            var message = $"An expression of type '{returnType}' is expected.";
            Report(location, message);
        }

        public void ReportNotAVariable(TextLocation location, string name)
        {
            var message = $"'{name}' is not a variable.";
            Report(location, message);
        }

        internal void ReportNotAFunction(TextLocation location, string name)
        {
            var message = $"'{name}' is not a function.";
            Report(location, message);
        }

        internal void ReportInvalidExpressionStatement(TextLocation location)
        {
            var message = $"Only assignment and call expressions can be used as a statement.";
            Report(location, message);
        }

        internal void ReportCannotMixMainAndGlobalStatements(TextLocation location)
        {
            var message = $"Cannot declare both main function when global statements are used.";
            Report(location, message);
        }

        internal void ReportMainMustHaveCorrectSignature(TextLocation location)
        {
            var message = $"Main must not take arguments and not return anything.";
            Report(location, message);
        }

        internal void ReportOnlyOneFileCanHaveGlobalStatements(TextLocation location)
        {
            var message = $"At most one file can have global statements.";
            Report(location, message);
        }

        public void ReportInvalidReturnWithValueInGlobalStatements(TextLocation location)
        {
            var message = "The 'return' keyword cannot be followed by an expression in global statements.";
            Report(location, message);
        }

        internal void ReportInvalidReference(string reference)
        {
            var message = $"The reference is not a valid .NET assembly: '{reference}'";

            Report(default, message);
        }

        internal void ReportRequiredTypeNotFound(string minskName, string metadataName)
        {
            var message =
                minskName != null ?
                $"The required type '{minskName}' ('{metadataName}') cannot be resolve among the given references."
                : $"The required type '{metadataName}' cannot be resolve among the given references.";
            Report(default, message);
        }

        internal void ReportRequiredTypeAmbigous(string minskName, string metadataName, TypeDefinition[] foundTypes)
        {
            var assemblyNames = foundTypes.Select(t => t.Module.Assembly.Name.Name);
            var assemblyNameList = String.Join(", ", assemblyNames);

            var message = minskName != null ?
    $"The required type '{minskName}' ('{metadataName}') was found in multiple references {assemblyNameList}."
    : $"The required type '{metadataName}' was found in multiple references {assemblyNameList}.";

            Report(default, message);
        }

        internal void ReportRequiredMethodNotFound(string typeName, string methodName, string[] parameterTypeNames)
        {
            var parameterTypeNameList = String.Join(", ", parameterTypeNames);
            var message = $"The required method '{typeName}.{methodName}({parameterTypeNameList})' cannot be resolved amont the given references.";
            Report(default, message);
        }

        internal void ReportUnterminatedMultiLineComment(TextLocation location)
        {
            var message = $"Unterminated multi-line comment.";
            Report(location, message);
        }
    }
}