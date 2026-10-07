// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;

namespace CobolNet.Tools.ArchCensus;

/// <summary>One census type as the compiler declares it: every partial, with the document each lives in.</summary>
internal sealed record CensusType(
    INamedTypeSymbol Symbol,
    string Key,
    string Id,
    LoadedProject Project,
    IReadOnlyList<(LoadedDocument Document, SyntaxReference Syntax)> Partials)
{
    /// <summary>A type no one authored: every declaration is in a generated document.</summary>
    public bool Generated => Partials.All(p => p.Document.Generated);
}

/// <summary>
/// Every named type DECLARED in a census project's compilation — authored, generated and source-generated alike,
/// nested types included — which is the population the census must equal the built assembly's
/// (<see cref="CompiledPopulation"/>). Sizes are measured over the declarations themselves.
/// </summary>
internal sealed class TypeInventory
{
    private readonly Dictionary<SyntaxTree, LoadedDocument> documents;

    public TypeInventory(LoadedSolution solution)
    {
        documents = solution.Projects.SelectMany(p => p.Documents).ToDictionary(d => d.Tree);
        var types = new List<CensusType>();
        foreach (LoadedProject project in solution.Census)
        {
            foreach (INamedTypeSymbol type in AllTypes(project.Compilation.Assembly.GlobalNamespace))
            {
                var partials = type.DeclaringSyntaxReferences
                    .Select(r => (Document: DocumentOf(r.SyntaxTree), Syntax: r))
                    .ToList();
                types.Add(new CensusType(type, SymbolKeys.Key(type)!, SymbolKeys.MetadataName(type), project, partials));
            }
        }

        Types = types;
        Namespaces = types.ToDictionary(t => t.Key, t => t.Symbol.ContainingNamespace?.ToDisplayString() ?? "",
            StringComparer.Ordinal);
    }

    public IReadOnlyList<CensusType> Types { get; }

    public bool IsCensusType(string key) => Namespaces.ContainsKey(key);

    /// <summary>Each census type's namespace, by key.</summary>
    public IReadOnlyDictionary<string, string> Namespaces { get; }

    public LoadedDocument DocumentOf(SyntaxTree tree) =>
        documents.TryGetValue(tree, out LoadedDocument? d)
            ? d
            : throw new CensusRefusal($"a declaration lives in a document the workspace did not list: {tree.FilePath}");

    public static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol ns)
    {
        foreach (INamespaceOrTypeSymbol member in ns.GetMembers())
        {
            IEnumerable<INamedTypeSymbol> found = member switch
            {
                INamespaceSymbol child => AllTypes(child),
                INamedTypeSymbol type => Nested(type),
                _ => [],
            };
            foreach (INamedTypeSymbol t in found)
            {
                yield return t;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> Nested(INamedTypeSymbol type)
    {
        yield return type;
        foreach (INamedTypeSymbol inner in type.GetTypeMembers())
        {
            foreach (INamedTypeSymbol t in Nested(inner))
            {
                yield return t;
            }
        }
    }

    /// <summary>The members a person wrote: every non-implicit field, method, property, indexer and event, without
    /// accessors (they belong to their property or event) and without nested types (counted separately).</summary>
    public static IEnumerable<ISymbol> AuthoredMembers(INamedTypeSymbol type) =>
        type.GetMembers().Where(m => !m.IsImplicitlyDeclared && m is not INamedTypeSymbol && m is not IMethodSymbol
        {
            MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd
                or MethodKind.EventRemove or MethodKind.EventRaise,
        });

    public static string KindOf(INamedTypeSymbol type) => type.TypeKind switch
    {
        TypeKind.Class when type.IsRecord => "record",
        TypeKind.Struct when type.IsRecord => "record struct",
        TypeKind.Class => type.IsStatic ? "static class" : "class",
        TypeKind.Struct => "struct",
        TypeKind.Interface => "interface",
        TypeKind.Enum => "enum",
        TypeKind.Delegate => "delegate",
        var k => k.ToString().ToLowerInvariant(),
    };
}
