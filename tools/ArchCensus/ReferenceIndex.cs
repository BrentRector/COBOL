// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CobolNet.Tools.ArchCensus;

/// <summary>Where a use was written: authored product code, generated product code, or a reader (test) project.</summary>
internal enum Origin
{
    Production,
    Generated,
    Test,
}

/// <summary>How often one symbol is used from each origin (its own body excluded).</summary>
internal sealed class Uses
{
    public int Production { get; set; }

    public int Generated { get; set; }

    public int Test { get; set; }

    public int Total => Production + Generated + Test;
}

/// <summary>
/// ONE semantic walk over every document of every census and reader project, binding each node the compiler binds
/// a symbol to — names, constructions, constructor initializers, attributes, indexers, user-defined operators and
/// conversions, <c>foreach</c>'s pattern members, deconstruction, collection-initializer <c>Add</c> calls and
/// positional patterns — and recording, per symbol key, how often it is used and from which origin, and, per type,
/// which census types its code depends on. A use inside the member's own body (recursion) is not a use.
/// <para>A walk can miss an implicit binding no node exposes. That blind spot is MEASURED, not assumed: every member
/// the walk leaves unreferenced is re-queried with <c>SymbolFinder</c> (<see cref="Reachability"/>), and the walk's
/// misses are reported in the record.</para>
/// </summary>
internal sealed partial class ReferenceIndex
{
    private readonly Dictionary<string, Uses> uses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> dependsOn = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> usedByProduction = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> usedByTests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> usedFromOutside = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<string>> chains = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> usedIn = new(StringComparer.Ordinal);
    private readonly HashSet<string> censusTypes;
    private readonly HashSet<string> emittedSurface;
    private string? authoredFile;

    public ReferenceIndex(IEnumerable<string> censusTypeKeys, IEnumerable<string> emittedSurfaceAssemblies)
    {
        censusTypes = censusTypeKeys.ToHashSet(StringComparer.Ordinal);
        emittedSurface = emittedSurfaceAssemblies.ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Every string literal's value, solution-wide: a type whose full name is one of them can be found by
    /// name (an xunit framework attribute, <c>Type.GetType</c>).</summary>
    public HashSet<string> StringLiterals { get; } = new(StringComparer.Ordinal);

    /// <summary>Every identifier written inside a string literal or interpolated-string text of a census project
    /// that is NOT the emitted surface: the names the code generator writes into generated programs.</summary>
    public HashSet<string> EmittedIdentifiers { get; } = new(StringComparer.Ordinal);

    /// <summary>Per identifier in <see cref="EmittedIdentifiers"/>, the AUTHORED documents whose string or
    /// interpolated text writes it: the emitters a change to a runtime name must edit in the same change (kb/Work
    /// PB2118 Draft 9, the eighth refuter's K1: <c>catch (ProgramReturn)</c> is C# text, never a symbol use).</summary>
    public Dictionary<string, HashSet<string>> EmittedIn { get; } = new(StringComparer.Ordinal);

    /// <summary>Keys of the types that reach <c>System.Text.Json</c> serialization (their properties are read and
    /// written by reflection).</summary>
    public HashSet<string> SerializedTypes { get; } = new(StringComparer.Ordinal);

    public int Documents { get; private set; }

    public int Recorded { get; private set; }

    public Uses UsesOf(string key) => uses.TryGetValue(key, out Uses? u) ? u : new Uses();

    public IReadOnlySet<string> DependenciesOf(string typeKey) =>
        dependsOn.TryGetValue(typeKey, out HashSet<string>? s) ? s : [];

    public IReadOnlySet<string> ProductionUsersOf(string typeKey) =>
        usedByProduction.TryGetValue(typeKey, out HashSet<string>? s) ? s : [];

    public IReadOnlySet<string> TestUsersOf(string typeKey) =>
        usedByTests.TryGetValue(typeKey, out HashSet<string>? s) ? s : [];

    /// <summary>The types that use <paramref name="typeKey"/> (or a member of it) from outside it — its own
    /// container included, which the fan metrics leave out.</summary>
    public IReadOnlySet<string> OutsideUsersOf(string typeKey) =>
        usedFromOutside.TryGetValue(typeKey, out HashSet<string>? s) ? s : [];

    /// <summary>The member index (kb/Work PB2118 Draft 8, J2): per symbol key, the AUTHORED documents (product and
    /// test, never generated) whose code uses it; a type's entry also holds every document using any of its members.
    /// What a restructuring wave that moves or changes a symbol must edit in the same change (CLAUDE.md rule 4).</summary>
    public IReadOnlyDictionary<string, HashSet<string>> FilesUsing => usedIn;

    public void Walk(LoadedProject project, LoadedDocument document, bool userConversionsExist)
    {
        Documents++;
        authoredFile = document.Generated ? null : document.RepoPath;
        SemanticModel model = project.Compilation.GetSemanticModel(document.Tree);
        Origin origin = !project.IsCensus ? Origin.Test : document.Generated ? Origin.Generated : Origin.Production;
        bool emitter = project.IsCensus && !emittedSurface.Contains(project.Compilation.AssemblyName ?? "");
        var walker = new DocumentWalk(this, model, origin, emitter, userConversionsExist);
        walker.Run(document.Tree.GetRoot());
    }

    private IReadOnlyList<string> ChainOf(INamedTypeSymbol type, string key)
    {
        if (!chains.TryGetValue(key, out IReadOnlyList<string>? chain))
        {
            chains[key] = chain = SymbolKeys.Chain(type);
        }

        return chain;
    }

    private void Record(ISymbol? used, in WalkContext at, Origin origin)
    {
        ISymbol? symbol = SymbolKeys.Normalize(used);
        if (symbol is null || !symbol.Locations.Any(l => l.IsInSource) || SymbolKeys.Key(symbol) is not { } key)
        {
            return;
        }

        if (authoredFile is not null)
        {
            Add(usedIn, key, authoredFile);
            if (SymbolKeys.TargetType(symbol) is { } owner && SymbolKeys.Key(owner) is { } ownerKey && ownerKey != key)
            {
                Add(usedIn, ownerKey, authoredFile);
            }
        }

        if (key == at.MemberKey)
        {
            return;   // recursion: a member's use of itself does not make it reachable
        }

        Recorded++;
        Count(key, origin);

        // A call bound to an override is a call of its whole override chain: the virtual slot it fills is in use,
        // so the chain's root is not dead because every caller names a derived class.
        for (ISymbol? overridden = Overridden(symbol); overridden is not null; overridden = Overridden(overridden))
        {
            if (overridden.Locations.Any(l => l.IsInSource) && SymbolKeys.Key(SymbolKeys.Normalize(overridden)) is { } rootKey)
            {
                Count(rootKey, origin);
            }
        }

        if (at.Type is null || SymbolKeys.TargetType(symbol) is not { } target || SymbolKeys.Key(target) is not { } to
            || !target.Locations.Any(l => l.IsInSource))
        {
            return;
        }

        // A use from inside the type itself (or a type nested in it) does not make the type reachable.
        IReadOnlyList<string> fromChain = ChainOf(at.Type, at.TypeKey!);
        if (fromChain.Contains(to))
        {
            return;
        }

        Add(usedFromOutside, to, at.TypeKey!);

        // For dependency purposes a type and the types nested in it are one unit: an outer type using its own
        // nested type is not a fan-out edge either.
        if (ChainOf(target, to).Contains(at.TypeKey!))
        {
            return;
        }

        if (censusTypes.Contains(at.TypeKey!) && censusTypes.Contains(to))
        {
            Add(dependsOn, at.TypeKey!, to);
        }

        Add(origin == Origin.Test ? usedByTests : usedByProduction, to, at.TypeKey!);
    }

    private void Count(string key, Origin origin)
    {
        Uses u = uses.TryGetValue(key, out Uses? existing) ? existing : uses[key] = new Uses();
        switch (origin)
        {
            case Origin.Production: u.Production++; break;
            case Origin.Generated: u.Generated++; break;
            default: u.Test++; break;
        }
    }

    public static ISymbol? Overridden(ISymbol s) => s switch
    {
        IMethodSymbol m => m.OverriddenMethod,
        IPropertySymbol p => p.OverriddenProperty,
        IEventSymbol e => e.OverriddenEvent,
        _ => null,
    };

    private static void Add(Dictionary<string, HashSet<string>> map, string key, string value)
    {
        if (!map.TryGetValue(key, out HashSet<string>? set))
        {
            map[key] = set = new HashSet<string>(StringComparer.Ordinal);
        }

        set.Add(value);
    }

    [GeneratedRegex("[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex IdentifierToken();

    /// <summary>The innermost declared type and member enclosing a node.</summary>
    private readonly record struct WalkContext(INamedTypeSymbol? Type, string? TypeKey, string? MemberKey);

    /// <summary>The walk over one document: an explicit stack (expression trees here run deep enough to make
    /// recursion a stack-overflow risk), carrying the enclosing type and member down to every node.</summary>
    private sealed class DocumentWalk(ReferenceIndex index, SemanticModel model, Origin origin, bool emitter,
        bool userConversionsExist)
    {
        public void Run(SyntaxNode root)
        {
            var stack = new Stack<(SyntaxNode Node, WalkContext At)>();
            stack.Push((root, default));
            while (stack.TryPop(out (SyntaxNode Node, WalkContext At) item))
            {
                WalkContext at = Enter(item.Node, item.At);
                Visit(item.Node, at);
                foreach (SyntaxNode child in item.Node.ChildNodes())
                {
                    stack.Push((child, at));
                }
            }
        }

        private WalkContext Enter(SyntaxNode node, WalkContext at)
        {
            switch (node)
            {
                case BaseTypeDeclarationSyntax or DelegateDeclarationSyntax:
                    var type = (INamedTypeSymbol?)model.GetDeclaredSymbol(node);
                    return type is null ? at : new WalkContext(type, SymbolKeys.Key(type.OriginalDefinition), null);
                case GlobalStatementSyntax when model.Compilation.GetEntryPoint(CancellationToken.None) is { } main:
                    return new WalkContext(main.ContainingType, SymbolKeys.Key(main.ContainingType), SymbolKeys.Key(main));
                case BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax or EnumMemberDeclarationSyntax:
                    return at with { MemberKey = SymbolKeys.Key(SymbolKeys.Normalize(model.GetDeclaredSymbol(node))) };
                case VariableDeclaratorSyntax { Parent.Parent: BaseFieldDeclarationSyntax }:
                    return at with { MemberKey = SymbolKeys.Key(SymbolKeys.Normalize(model.GetDeclaredSymbol(node))) };
                default:
                    return at;
            }
        }

        private void Visit(SyntaxNode node, in WalkContext at)
        {
            switch (node)
            {
                case SimpleNameSyntax:
                case ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax:
                case ConstructorInitializerSyntax or PrimaryConstructorBaseTypeSyntax or AttributeSyntax:
                case ElementAccessExpressionSyntax or ImplicitElementAccessSyntax:
                case BinaryExpressionSyntax or PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax:
                case PositionalPatternClauseSyntax:
                    RecordInfo(model.GetSymbolInfo(node), at);
                    break;
                case AssignmentExpressionSyntax assignment:
                    RecordInfo(model.GetSymbolInfo(assignment), at);
                    if (assignment.Left is TupleExpressionSyntax or DeclarationExpressionSyntax)
                    {
                        RecordDeconstruction(model.GetDeconstructionInfo(assignment), at);
                    }

                    break;
                case CommonForEachStatementSyntax forEach:
                    ForEachStatementInfo info = model.GetForEachStatementInfo(forEach);
                    RecordUse(info.GetEnumeratorMethod, at);
                    RecordUse(info.MoveNextMethod, at);
                    RecordUse(info.CurrentProperty, at);
                    RecordUse(info.DisposeMethod, at);
                    if (forEach is ForEachVariableStatementSyntax deconstructing)
                    {
                        RecordDeconstruction(model.GetDeconstructionInfo(deconstructing), at);
                    }

                    break;
                case InitializerExpressionSyntax initializer when initializer.IsKind(SyntaxKind.CollectionInitializerExpression):
                    foreach (ExpressionSyntax element in initializer.Expressions)
                    {
                        RecordInfo(model.GetCollectionInitializerSymbolInfo(element), at);
                    }

                    break;
                case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
                    index.StringLiterals.Add(literal.Token.ValueText);
                    Emitted(literal.Token.ValueText);
                    break;
                case InterpolatedStringTextSyntax text:
                    Emitted(text.TextToken.ValueText);
                    break;
            }

            if (node is InvocationExpressionSyntax invocation)
            {
                Serialization(invocation);
            }

            if (userConversionsExist && node is ExpressionSyntax expression)
            {
                Conversion conversion = model.GetConversion(expression);
                if (conversion.IsUserDefined)
                {
                    RecordUse(conversion.MethodSymbol, at);
                }
            }
        }

        private void RecordInfo(SymbolInfo info, in WalkContext at)
        {
            if (info.Symbol is not null)
            {
                RecordUse(info.Symbol, at);
                return;
            }

            // A method group (nameof over an overload set) or an ambiguous binding: each candidate is named.
            foreach (ISymbol candidate in info.CandidateSymbols)
            {
                RecordUse(candidate, at);
            }
        }

        private void RecordUse(ISymbol? symbol, in WalkContext at)
        {
            index.Record(symbol, at, origin);

            // A positional pattern or deconstruction of a record binds its SYNTHESIZED Deconstruct, which reads every
            // positional property: those reads are uses no node names.
            if (symbol is IMethodSymbol { Name: "Deconstruct", IsImplicitlyDeclared: true, ContainingType.IsRecord: true } d)
            {
                foreach (IParameterSymbol p in d.Parameters)
                {
                    index.Record(d.ContainingType.GetMembers(p.Name).OfType<IPropertySymbol>().FirstOrDefault(), at, origin);
                }
            }
        }

        private void RecordDeconstruction(DeconstructionInfo info, in WalkContext at)
        {
            RecordUse(info.Method, at);
            foreach (DeconstructionInfo nested in info.Nested)
            {
                RecordDeconstruction(nested, at);
            }
        }

        private void Emitted(string text)
        {
            if (!emitter)
            {
                return;
            }

            foreach (Match m in IdentifierToken().Matches(text))
            {
                index.EmittedIdentifiers.Add(m.Value);
                if (index.authoredFile is not null)
                {
                    Add(index.EmittedIn, m.Value, index.authoredFile);
                }
            }
        }

        private void Serialization(InvocationExpressionSyntax invocation)
        {
            if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol
                {
                    ContainingType: { Name: "JsonSerializer", ContainingNamespace: var ns },
                } method || ns.ToDisplayString() != "System.Text.Json")
            {
                return;
            }

            foreach (ITypeSymbol t in method.TypeArguments)
            {
                Serialized(t);
            }

            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                Serialized(model.GetTypeInfo(argument.Expression).Type);
            }
        }

        private void Serialized(ITypeSymbol? type)
        {
            switch (type)
            {
                case IArrayTypeSymbol array:
                    Serialized(array.ElementType);
                    break;
                case INamedTypeSymbol named when named.Locations.Any(l => l.IsInSource):
                    if (SymbolKeys.Key(named.OriginalDefinition) is { } key && index.SerializedTypes.Add(key))
                    {
                        foreach (IPropertySymbol p in named.GetMembers().OfType<IPropertySymbol>())
                        {
                            Serialized(p.Type);
                        }
                    }

                    break;
                case INamedTypeSymbol generic:
                    foreach (ITypeSymbol argument in generic.TypeArguments)
                    {
                        Serialized(argument);
                    }

                    break;
            }
        }
    }
}
