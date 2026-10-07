// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace CobolNet.Tools.ArchCensus;

/// <summary>
/// Reachability, MEASURED (engineering-standards: never deduced). A census member is reachable when authored or
/// generated product code uses it outside its own body (<see cref="ReferenceIndex"/>). Every other authored member
/// gets exactly one <c>reach</c>:
/// <list type="bullet">
/// <item>a STATED exclusion rule (<see cref="Rules"/>) when something the reference query cannot see reaches it — a
/// runtime dispatch, an implicit call, reflection, emitted program text, a synthesized record or enum member;</item>
/// <item><c>test-only</c> when only a test project uses it;</item>
/// <item><c>unreferenced</c> otherwise.</item>
/// </list>
/// Every <c>test-only</c> and <c>unreferenced</c> answer is CONFIRMED by an independent query
/// (<see cref="SymbolFinder.FindReferencesAsync(ISymbol, Solution, CancellationToken)"/>, which cascades through
/// overrides and implementations); a use the finder sees and the walk did not is recorded as a walk miss and wins.
/// Types are judged the same way, on uses from outside the type and the types nested in it.
/// </summary>
internal sealed class Reachability(LoadedSolution solution, TypeInventory inventory, ReferenceIndex index,
    IReadOnlySet<string> emittedSurface)
{
    /// <summary>The stated exclusion rules, in the order they are tried. The record carries this text.</summary>
    public static readonly IReadOnlyDictionary<string, string> Rules = new Dictionary<string, string>
    {
        ["entry-point"] = "the program entry point, a [ModuleInitializer], or a type that declares one",
        ["implicit-call"] = "a static constructor, a finalizer, or a parameterless instance constructor of a struct or "
                            + "of a class some source type derives from (the derived constructor's implicit base() call)",
        ["framework-dispatch"] = "overrides or implements a member declared outside the solution (the framework or a "
                                 + "package calls it through the base or the interface)",
        ["dispatch"] = "overrides or implements a solution member that is itself used (the call binds to the root)",
        ["reflection-attribute"] = "the member or an enclosing type carries an attribute a framework discovers by "
                                   + "reflection (ModuleInitializer, Roslyn Generator/DiagnosticAnalyzer, "
                                   + "System.Text.Json.Serialization, DynamicDependency, UnmanagedCallersOnly, xunit, "
                                   + "BenchmarkDotNet)",
        ["serialized"] = "a property of a type that reaches System.Text.Json serialization",
        ["emitted-text"] = "a member of the emitted surface (the runtime generated programs call) whose name is written "
                           + "inside a string literal or interpolated text of a code-generating project",
        ["named-in-string"] = "a type whose full name is a string literal somewhere in the solution (found by name)",
        ["record-positional"] = "a positional record property: the synthesized Equals, GetHashCode, ToString and "
                                + "Deconstruct read it",
        ["enum-member"] = "an enum member: its NAME is reachable through ToString, Parse and interpolation, which no "
                          + "reference query sees",
    };

    private static readonly string[] ReflectionAttributes =
    [
        "System.Runtime.CompilerServices.ModuleInitializerAttribute",
        "Microsoft.CodeAnalysis.GeneratorAttribute",
        "Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerAttribute",
        "System.Text.Json.Serialization.",
        "System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute",
        "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute",
        "Xunit.",
        "BenchmarkDotNet.Attributes.",
    ];

    private readonly HashSet<string> baseTypes = BaseTypeKeys(solution);
    private readonly HashSet<string> entryPoints = EntryPointKeys(solution);
    private readonly ConcurrentBag<string> misses = [];

    public IReadOnlyCollection<string> WalkMisses => misses;

    public int Candidates { get; private set; }

    /// <summary>Every authored census member that production code does not use, with its reach.</summary>
    public async Task<IReadOnlyList<RawMember>> MembersAsync()
    {
        var pending = new List<(CensusType Type, ISymbol Member, string Key, string Reach, string? Family)>();
        foreach (CensusType type in inventory.Types.Where(t => !t.Generated))
        {
            foreach (ISymbol member in TypeInventory.AuthoredMembers(type.Symbol))
            {
                // Every authored member has a documentation id; one without would be silently unjudged, so refuse.
                ISymbol symbol = SymbolKeys.Normalize(member)
                                 ?? throw new CensusRefusal($"{type.Id}: member {member.Name} has no census identity");
                string key = SymbolKeys.Key(symbol)
                             ?? throw new CensusRefusal($"{type.Id}: member {member.Name} has no documentation id");

                Uses uses = index.UsesOf(key);
                if (uses.Production + uses.Generated > 0)
                {
                    continue;
                }

                (string? rule, string? family) = Exclusion(type, symbol);
                pending.Add((type, symbol, key, rule ?? (uses.Test > 0 ? "test-only" : "unreferenced"), family));
            }
        }

        var confirmed = new ConcurrentBag<RawMember>();
        Candidates = pending.Count(p => p.Reach is "test-only" or "unreferenced");
        await Parallel.ForEachAsync(pending, Parallelism(), async (p, ct) =>
        {
            string reach = p.Reach;
            if (reach is "test-only" or "unreferenced")
            {
                Origin? found = await StrongestUseAsync(p.Member, p.Member.DeclaringSyntaxReferences, ct);
                if (found is Origin.Production or Origin.Generated)
                {
                    misses.Add($"{p.Member.Kind} {p.Key}");
                    return;
                }

                reach = found is Origin.Test ? "test-only" : "unreferenced";
            }

            (string file, int line, int lines) = Where(p.Member.DeclaringSyntaxReferences);
            confirmed.Add(new RawMember(p.Key[(p.Key.IndexOf('|') + 1)..], p.Type.Id, p.Type.Project.Name,
                MemberKind(p.Member), file, line, lines, reach, p.Family));
        });
        return [.. confirmed.OrderBy(m => m.Type, StringComparer.Ordinal).ThenBy(m => m.Line).ThenBy(m => m.Id, StringComparer.Ordinal)];
    }

    /// <summary>The reach of every authored census type no type outside it uses (null for the rest).</summary>
    public async Task<IReadOnlyDictionary<string, string>> TypesAsync()
    {
        var reach = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        var pending = inventory.Types
            .Where(t => !t.Generated && !index.OutsideUsersOf(t.Key).Any(inventory.IsCensusType))
            .ToList();
        await Parallel.ForEachAsync(pending, Parallelism(), async (type, ct) =>
        {
            string? rule = TypeExclusion(type);
            if (rule is not null)
            {
                reach[type.Key] = rule;
                return;
            }

            Origin? found = await StrongestUseAsync(type.Symbol, type.Symbol.DeclaringSyntaxReferences, ct);
            if (found is Origin.Production or Origin.Generated)
            {
                misses.Add($"Type {type.Key}");
                return;
            }

            reach[type.Key] = found is Origin.Test || index.OutsideUsersOf(type.Key).Count > 0 ? "test-only" : "unreferenced";
        });
        return reach;
    }

    private static ParallelOptions Parallelism() => new() { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };

    private (string? Rule, string? Family) Exclusion(CensusType type, ISymbol member)
    {
        if (IsEntryPoint(member))
        {
            return ("entry-point", null);
        }

        if (IsImplicitlyCalled(type, member))
        {
            return ("implicit-call", null);
        }

        IReadOnlyList<ISymbol> roots = DispatchRoots(member);
        if (roots.Count > 0)
        {
            if (roots.Any(r => !r.Locations.Any(l => l.IsInSource)))
            {
                return ("framework-dispatch", null);
            }

            var rootKeys = roots.Select(r => SymbolKeys.Key(SymbolKeys.Normalize(r))).OfType<string>().ToList();
            if (rootKeys.Any(k => index.UsesOf(k).Total > 0))
            {
                return ("dispatch", null);
            }

            // The family is named by its top-most root: the last member of the override chain, else the interface member.
            string family = rootKeys[^1];
            return (null, family[(family.IndexOf('|') + 1)..]);
        }

        if (HasReflectionAttribute(member))
        {
            return ("reflection-attribute", null);
        }

        if (member is IPropertySymbol && SymbolKeys.Key(type.Symbol.OriginalDefinition) is { } typeKey
            && index.SerializedTypes.Contains(typeKey))
        {
            return ("serialized", null);
        }

        if (emittedSurface.Contains(type.Project.Compilation.AssemblyName ?? "") && index.EmittedIdentifiers.Contains(member.Name))
        {
            return ("emitted-text", null);
        }

        if (member is IPropertySymbol && type.Symbol.IsRecord
            && member.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is ParameterSyntax))
        {
            return ("record-positional", null);
        }

        if (member is IFieldSymbol && type.Symbol.TypeKind == TypeKind.Enum)
        {
            return ("enum-member", null);
        }

        return (null, null);
    }

    private string? TypeExclusion(CensusType type)
    {
        INamedTypeSymbol symbol = type.Symbol;
        if (symbol.GetMembers().Any(m => IsEntryPoint(m)))
        {
            return "entry-point";
        }

        if (HasReflectionAttribute(symbol))
        {
            return "reflection-attribute";
        }

        if (index.StringLiterals.Contains(type.Id) || index.StringLiterals.Contains(symbol.ToDisplayString()))
        {
            return "named-in-string";
        }

        return emittedSurface.Contains(type.Project.Compilation.AssemblyName ?? "") && index.EmittedIdentifiers.Contains(symbol.Name)
            ? "emitted-text"
            : null;
    }

    private bool IsEntryPoint(ISymbol member) =>
        member is IMethodSymbol m
        && (entryPoints.Contains(SymbolKeys.Key(m) ?? "")
            || m.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == ReflectionAttributes[0]));

    private static HashSet<string> EntryPointKeys(LoadedSolution solution) =>
        solution.Census
            .Select(p => SymbolKeys.Key(p.Compilation.GetEntryPoint(CancellationToken.None)))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

    private bool IsImplicitlyCalled(CensusType type, ISymbol member) => member switch
    {
        IMethodSymbol { MethodKind: MethodKind.StaticConstructor or MethodKind.Destructor } => true,
        IMethodSymbol { MethodKind: MethodKind.Constructor, Parameters.Length: 0 } =>
            type.Symbol.TypeKind == TypeKind.Struct || baseTypes.Contains(type.Key),
        _ => false,
    };

    /// <summary>What a call can reach this member THROUGH: every member up its override chain, and every
    /// interface member it or one of those implements. Empty when it neither overrides nor implements.</summary>
    private static IReadOnlyList<ISymbol> DispatchRoots(ISymbol member)
    {
        var chain = new List<ISymbol>();
        for (ISymbol? s = ReferenceIndex.Overridden(member); s is not null; s = ReferenceIndex.Overridden(s))
        {
            chain.Add(s.OriginalDefinition);
        }

        var roots = new List<ISymbol>(chain);
        foreach (ISymbol implementer in chain.Prepend(member))
        {
            INamedTypeSymbol container = implementer.ContainingType;
            foreach (INamedTypeSymbol iface in container.AllInterfaces)
            {
                foreach (ISymbol candidate in iface.GetMembers())
                {
                    if (SymbolEqualityComparer.Default.Equals(container.FindImplementationForInterfaceMember(candidate), implementer))
                    {
                        roots.Add(candidate.OriginalDefinition);
                    }
                }
            }
        }

        return roots;
    }

    private static bool HasReflectionAttribute(ISymbol symbol)
    {
        for (ISymbol? s = symbol; s is not null and not INamespaceSymbol; s = s.ContainingSymbol)
        {
            foreach (AttributeData a in s.GetAttributes())
            {
                string name = a.AttributeClass?.ToDisplayString() ?? "";
                if (ReflectionAttributes.Any(r => r.EndsWith('.') ? name.StartsWith(r, StringComparison.Ordinal) : name == r))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>The strongest origin of any use the finder sees outside the declarations themselves, or null.</summary>
    private async Task<Origin?> StrongestUseAsync(ISymbol symbol, IEnumerable<SyntaxReference> own, CancellationToken ct)
    {
        var spans = own.Select(r => (r.SyntaxTree, r.Span)).ToList();
        Origin? strongest = null;
        foreach (ReferencedSymbol referenced in await SymbolFinder.FindReferencesAsync(symbol, solution.Solution, ct))
        {
            foreach (ReferenceLocation location in referenced.Locations)
            {
                SyntaxTree? tree = location.Location.SourceTree;
                if (tree is null || spans.Any(s => s.SyntaxTree == tree && s.Span.Contains(location.Location.SourceSpan))
                    || InDocumentation(tree, location.Location.SourceSpan.Start))
                {
                    continue;
                }

                Origin origin = OriginOf(location.Document.Project.Name, tree);
                if (strongest is null || origin < strongest)
                {
                    strongest = origin;
                }
            }
        }

        return strongest;
    }

    /// <summary>A <c>cref</c> in an XML documentation comment names a symbol without using it: the finder reports
    /// it, the census does not count it.</summary>
    private static bool InDocumentation(SyntaxTree tree, int position) =>
        tree.GetRoot().FindToken(position, findInsideTrivia: true).Parent?.IsPartOfStructuredTrivia() == true;

    private Origin OriginOf(string projectName, SyntaxTree tree)
    {
        // A use from a reader project is a test use; a use from an EXCLUDED project (the legacy engine, which no census
        // project references and which references no census project) is never product use either, so it ranks
        // with the tests rather than keeping a member alive.
        LoadedProject? project = solution.Projects.FirstOrDefault(p => p.Name == projectName);
        if (project is null || !project.IsCensus)
        {
            return Origin.Test;
        }

        return project.Documents.FirstOrDefault(d => d.Tree == tree)?.Generated == true ? Origin.Generated : Origin.Production;
    }

    private (string File, int Line, int Lines) Where(IEnumerable<SyntaxReference> declarations)
    {
        string file = "";
        int line = 0;
        int lines = 0;
        foreach (SyntaxReference r in declarations)
        {
            FileLinePositionSpan span = r.SyntaxTree.GetLineSpan(r.Span);
            if (file.Length == 0)
            {
                file = inventory.DocumentOf(r.SyntaxTree).RepoPath;
                line = span.StartLinePosition.Line + 1;
            }

            lines += span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
        }

        return (file, line, lines);
    }

    private static string MemberKind(ISymbol member) => member switch
    {
        IMethodSymbol { MethodKind: MethodKind.Constructor } => "constructor",
        IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion } => "operator",
        IMethodSymbol => "method",
        IPropertySymbol { IsIndexer: true } => "indexer",
        IPropertySymbol => "property",
        IFieldSymbol { IsConst: true } => "const",
        IFieldSymbol => "field",
        IEventSymbol => "event",
        _ => member.Kind.ToString().ToLowerInvariant(),
    };

    /// <summary>Keys of every type some source type of a loaded project names as its base class.</summary>
    private static HashSet<string> BaseTypeKeys(LoadedSolution solution)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (LoadedProject project in solution.Projects)
        {
            foreach (INamedTypeSymbol type in TypeInventory.AllTypes(project.Compilation.Assembly.GlobalNamespace))
            {
                if (type.BaseType is { } b && SymbolKeys.Key(b.OriginalDefinition) is { } key)
                {
                    keys.Add(key);
                }
            }
        }

        return keys;
    }
}
