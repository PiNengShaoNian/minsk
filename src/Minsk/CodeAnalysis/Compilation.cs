using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Binding;
using System.Collections.Immutable;
using Minsk.CodeAnalysis.Lowering;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Emit;

namespace Minsk.CodeAnalysis
{
    public sealed class Compilation
    {
        private BoundGlobalScope _globalScope;

        private Compilation(bool isScript, Compilation previous, params SyntaxTree[] syntaxTrees)
        {
            IsScript = isScript;
            Previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

        public static Compilation Create(params SyntaxTree[] syntaxTrees)
        {
            return new Compilation(isScript: false, null, syntaxTrees);
        }

        public static Compilation CreateScript(Compilation previous, params SyntaxTree[] syntaxTrees)
        {
            return new Compilation(isScript: true, previous, syntaxTrees);
        }

        public bool IsScript { get; }
        public Compilation Previous { get; }
        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
        public FunctionSymbol MainFunction => GlobalScope.MainFunction;
        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope == null)
                {
                    var globalScope = Binder.BindGlobalScope(IsScript, Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }

        public IEnumerable<Symbol> GetSymbols()
        {
            var submission = this;
            var seenSymbols = new HashSet<string>();

            while (submission != null)
            {
                foreach (var function in submission.Functions)
                    if (seenSymbols.Add(function.Name))
                        yield return function;

                foreach (var variable in submission.Variables)
                    if (seenSymbols.Add(variable.Name))
                        yield return variable;

                submission = submission.Previous;
            }

            foreach (var f in BuiltinFunctions.GetAll())
                if (seenSymbols.Add(f.Name))
                    yield return f;
        }

        private BoundProgram GetProgram()
        {
            var previous = Previous == null ? null : Previous.GetProgram();
            return Binder.BindProgram(IsScript, previous, GlobalScope);
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            var parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);
            var diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics).ToArray();
            if (diagnostics.Any())
                return new EvaluationResult(diagnostics.ToImmutableArray(), null);

            var program = GetProgram();

            //var appPath = Environment.GetCommandLineArgs()[0];
            //var appDirectory = Path.GetDirectoryName(appPath);
            //var cfgPath = Path.Combine(appDirectory, "cfg.dot");

            //var cfgStatements = !program.Statement.Statements.Any() && program.Functions.Any()
            //                        ? program.Functions.Last().Value : program.Statement;

            //var cfg = ControlFlowGraph.Create(cfgStatements);
            //using (var streamWriter = new StreamWriter(cfgPath))
            //{
            //    cfg.WriteTo(streamWriter);
            //}
            if (program.Diagnostics.Any())
                return new EvaluationResult(program.Diagnostics.ToImmutableArray(), null);

            var evaluator = new Evaluator(program, variables);
            var value = evaluator.Evaluate();
            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            if (GlobalScope.MainFunction != null)
                EmitTree(GlobalScope.MainFunction, writer);
            else if (GlobalScope.ScriptFunction != null)
                EmitTree(GlobalScope.ScriptFunction, writer);
        }

        public void EmitTree(FunctionSymbol function, TextWriter writer)
        {
            var program = GetProgram();

            function.WriteTo(writer);

            if (!program.Functions.TryGetValue(function, out var body))
                return;
            body.WriteTo(writer);
        }

        public ImmutableArray<Diagnostic> Emit(string moudleName, string[] references, string outputPath)
        {
            var parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);

            var diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics).ToImmutableArray();
            if (diagnostics.Any())
                return diagnostics;

            var program = GetProgram();

            return Emitter.Emit(program, moudleName, references, outputPath);
        }
    }
}