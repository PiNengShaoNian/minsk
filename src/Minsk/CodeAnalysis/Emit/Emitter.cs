using Minsk.CodeAnalysis.Binding;
using Minsk.CodeAnalysis.Symbols;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Emit
{
    internal sealed class Emitter
    {
        private readonly List<AssemblyDefinition> _assemblies = new List<AssemblyDefinition>();
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly Dictionary<TypeSymbol, TypeReference> _knownTypes = new Dictionary<TypeSymbol, TypeReference>();
        private readonly AssemblyDefinition _assemblyDefinition;
        private readonly MethodReference _consoleWriteLineReference;

        private Emitter(string moduleName, string[] references)
        {
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
                    _diagnostics.ReportInvalidReference(reference);
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
            _assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, moduleName, ModuleKind.Console);

            foreach (var (typeSymbol, metadataName) in buildInTypes)
            {
                var minskName = typeSymbol.Name;
                var typeReference = ResolveType(minskName, metadataName);
                _knownTypes.Add(typeSymbol, typeReference);
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
                    typeReference = _assemblyDefinition.MainModule.ImportReference(foundTypes[0]);
                    return typeReference;
                }
                else if (foundTypes.Length == 0)
                {
                    _diagnostics.ReportRequiredTypeNotFound(minskName, metadataName);
                }
                else if (foundTypes.Length > 1)
                {
                    _diagnostics.ReportRequiredTypeAmbigous(minskName, metadataName, foundTypes);
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

                        return _assemblyDefinition.MainModule.ImportReference(method);
                    }

                    _diagnostics.ReportRequiredMethodNotFound(typeName, methodName, parameterTypeNames);
                    return null;
                }
                else if (foundTypes.Length == 0)
                {
                    _diagnostics.ReportRequiredTypeNotFound(null, typeName);
                }
                else if (foundTypes.Length > 1)
                {
                    _diagnostics.ReportRequiredTypeAmbigous(null, typeName, foundTypes);
                }

                return null;
            }

            _consoleWriteLineReference = ResolveMethod("System.Console", "WriteLine", new[] { "System.String" });
        }

        private ImmutableArray<Diagnostic> Emit(BoundProgram program, string outputPath)
        {
            if (_diagnostics.Any())
            {
                return _diagnostics.ToImmutableArray();
            }

            var objectType = _knownTypes[TypeSymbol.Any];
            var typeDefinition = new TypeDefinition(
                "",
                "Program",
                TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public,
                objectType);
            _assemblyDefinition.MainModule.Types.Add(typeDefinition);

            var voidType = _knownTypes[TypeSymbol.Void];
            var mainMethod = new MethodDefinition("Main", MethodAttributes.Static | MethodAttributes.Private, voidType);
            typeDefinition.Methods.Add(mainMethod);

            var ilProcessor = mainMethod.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldstr, "Hello world from minsk!");
            ilProcessor.Emit(OpCodes.Call, _consoleWriteLineReference);
            ilProcessor.Emit(OpCodes.Ret);

            _assemblyDefinition.EntryPoint = mainMethod;

            _assemblyDefinition.Write(outputPath);
            return _diagnostics.ToImmutableArray();
        }


        internal static ImmutableArray<Diagnostic> Emit(BoundProgram program, string moduleName, string[] references, string outputPath)
        {
            if (program.Diagnostics.Any())
                return program.Diagnostics;

            var emitter = new Emitter(moduleName, references);

            return emitter.Emit(program, outputPath);
        }
    }
}
