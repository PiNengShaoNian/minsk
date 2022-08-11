using Minsk.CodeAnalysis.Symbols;
using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Binding
{
    internal class BoundProgram
    {
        public BoundProgram(ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> immutableDictionary, BoundBlockStatement statement)
        {
            Diagnostics = diagnostics;
            Functions = immutableDictionary;
            Statement = statement;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
        public BoundBlockStatement Statement { get; }
    }
}