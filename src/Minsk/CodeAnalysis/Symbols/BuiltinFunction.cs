﻿using System.Collections.Immutable;
using System.Reflection;

namespace Minsk.CodeAnalysis.Symbols
{
    internal static class BuiltinFunctions
    {
        public static readonly FunctionSymbol Print = new FunctionSymbol(
                                                             "print",
                                                             ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.Any, 0)),
                                                             TypeSymbol.Void
                                                          );
        public static readonly FunctionSymbol Input = new FunctionSymbol(
                                                             "input",
                                                             ImmutableArray<ParameterSymbol>.Empty,
                                                             TypeSymbol.String
                                                          );

        public static readonly FunctionSymbol Random = new FunctionSymbol(
                                                     "random",
                                                     ImmutableArray.Create(new ParameterSymbol("max", TypeSymbol.Int, 0)),
                                                     TypeSymbol.Int
                                                  );

        internal static IEnumerable<FunctionSymbol> GetAll() => typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(FunctionSymbol))
            .Select(f => (FunctionSymbol)f.GetValue(null));
    }
}