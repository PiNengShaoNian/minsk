﻿namespace Minsk.CodeAnalysis.Symbols
{
    public sealed class VariableSymbol : Symbol
    {

        internal VariableSymbol(string name, bool isReadOnly, TypeSymbol type) : base(name)
        {
            IsReadOnly = isReadOnly;
            Type = type;
        }

        public bool IsReadOnly { get; }
        public TypeSymbol Type { get; }

        public override SymbolKind Kind => throw new NotImplementedException();

        public override string ToString() => Name;
    }
}