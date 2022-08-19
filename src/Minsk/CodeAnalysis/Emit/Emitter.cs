using Minsk.CodeAnalysis.Binding;
using Minsk.CodeAnalysis.Symbols;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Emit
{
    internal class Emitter
    {
        private readonly List<AssemblyDefinition> _assemblies = new List<AssemblyDefinition>();
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly Dictionary<TypeSymbol, TypeReference> _knownTypes = new Dictionary<TypeSymbol, TypeReference>();

        internal static ImmutableArray<Diagnostic> Emit(BoundProgram program, string moduleName, string[] references, string outputPath)
        {
            if (program.Diagnostics.Any())
                return program.Diagnostics;

            var result = new DiagnosticBag();
            var asseblies = new List<AssemblyDefinition>();
            foreach (var reference in references)
            {
                try
                {
                    var assembly = AssemblyDefinition.ReadAssembly(reference);
                    asseblies.Add(assembly);
                }
                catch (BadImageFormatException)
                {
                    result.ReportInvalidReference(reference);
                }
            }

            var buildInTypes = new List<(TypeSymbol type, string MetadataName)>()
            {
                (TypeSymbol.Any, "System.Object"),
                (TypeSymbol.Bool, "System.Boolean"),
                (TypeSymbol.Int, "System.Int32"),
                (TypeSymbol.String, "System.String"),
                (TypeSymbol.Void, "System.Void"),
            };

            var assemblyName = new AssemblyNameDefinition(moduleName, new Version(1, 0));
            var assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, moduleName, ModuleKind.Console);
            var knownTypes = new Dictionary<TypeSymbol, TypeReference>();

            foreach (var (typeSymbol, metadataName) in buildInTypes)
            {
                var minskName = typeSymbol.Name;
                var typeReference = ResolveType(minskName, metadataName);
                knownTypes.Add(typeSymbol, typeReference);
            }

            TypeReference ResolveType(string minskName, string metadataName)
            {

                TypeReference typeReference;
                var foundTypes = asseblies.SelectMany(a => a.Modules)
                                              .SelectMany(m => m.Types)
                                              .Where(t => t.FullName == metadataName)
                                              .ToArray();
                if (foundTypes.Length == 1)
                {
                    typeReference = assemblyDefinition.MainModule.ImportReference(foundTypes[0]);
                    return typeReference;
                }
                else if (foundTypes.Length == 0)
                {
                    result.ReportRequiredTypeNotFound(minskName, metadataName);
                }
                else if (foundTypes.Length > 1)
                {
                    result.ReportRequiredTypeAmbigous(minskName, metadataName, foundTypes);
                }

                return null;
            }

            MethodReference ResolveMethod(string typeName, string methodName, string[] parameterTypeNames)
            {

                var foundTypes = asseblies.SelectMany(a => a.Modules)
                                              .SelectMany(m => m.Types)
                                              .Where(t => t.FullName == typeName)
                                              .ToArray();
                if (foundTypes.Length == 1)
                {
                    var foundType = foundTypes[0];
                    var methods = foundType.Methods.Where(m => m.Name == methodName);

                    foreach (var method in methods)
                    {
                        if (method.Parameters.Count != parameterTypeNames.Length)
                            continue;
                        var allParametersMatch = true;
                        for (var i = 0; i < method.Parameters.Count; ++i)
                        {
                            if (method.Parameters[i].ParameterType.FullName != parameterTypeNames[i])
                            {
                                allParametersMatch = false;
                                break;
                            }
                        }

                        if (!allParametersMatch)
                            continue;

                        return assemblyDefinition.MainModule.ImportReference(method);
                    }

                    result.ReportRequiredMethodNotFound(typeName, methodName, parameterTypeNames);
                    return null;
                }
                else if (foundTypes.Length == 0)
                {
                    result.ReportRequiredTypeNotFound(null, typeName);
                }
                else if (foundTypes.Length > 1)
                {
                    result.ReportRequiredTypeAmbigous(null, typeName, foundTypes);
                }

                return null;
            }

            var consoleWriteLineReference = ResolveMethod("System.Console", "WriteLine", new[] { "System.String" });

            if (result.Any())
            {
                return result.ToImmutableArray();
            }

            var objectType = knownTypes[TypeSymbol.Any];
            var typeDefinition = new TypeDefinition(
                "",
                "Program",
                TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public,
                objectType);
            assemblyDefinition.MainModule.Types.Add(typeDefinition);

            var voidType = knownTypes[TypeSymbol.Void];
            var mainMethod = new MethodDefinition("Main", MethodAttributes.Static | MethodAttributes.Private, voidType);
            typeDefinition.Methods.Add(mainMethod);

            var ilProcessor = mainMethod.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldstr, "Hello world from minsk!");
            ilProcessor.Emit(OpCodes.Call, consoleWriteLineReference);
            ilProcessor.Emit(OpCodes.Ret);

            assemblyDefinition.EntryPoint = mainMethod;

            assemblyDefinition.Write(outputPath);

            return result.ToImmutableArray();
        }
    }
}
