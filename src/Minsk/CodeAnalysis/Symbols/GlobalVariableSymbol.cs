using Minsk.CodeAnalysis.Binding;

namespace Minsk.CodeAnalysis.Symbols
{
    public class GlobalVariableSymbol : VariableSymbol
    {

        internal GlobalVariableSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant constant) : base(name, isReadOnly, type, constant)
        {
        }

        public override SymbolKind Kind => SymbolKind.GlobalVariable;
    }
}