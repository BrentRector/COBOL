// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;

namespace CobolNet.Tools.ArchCensus;

/// <summary>
/// One identity for a symbol across every compilation of the solution. Each project has its own compilation, and a
/// symbol seen through a project reference (or retargeted into the netstandard2.0 generator) is a different object
/// from its declaration, so the census keys symbols by <c>assembly|documentation-id</c>, never by reference.
/// </summary>
internal static class SymbolKeys
{
    /// <summary>The declaration a use stands for: the generic definition, the static form of a reduced extension
    /// call, the defining half of a partial member, and the property or event behind an accessor. Locals,
    /// parameters, lambdas, local functions, labels and namespaces are not census subjects and normalize to null.</summary>
    public static ISymbol? Normalize(ISymbol? symbol)
    {
        switch (symbol)
        {
            case null:
                return null;
            case IMethodSymbol method:
                method = method.ReducedFrom ?? method;
                method = method.OriginalDefinition;
                method = method.PartialDefinitionPart ?? method;
                return method.MethodKind switch
                {
                    MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove
                        or MethodKind.EventRaise => Normalize(method.AssociatedSymbol),
                    MethodKind.LocalFunction or MethodKind.AnonymousFunction or MethodKind.LambdaMethod => null,
                    _ => method,
                };
            case IPropertySymbol property:
                property = property.OriginalDefinition;
                return property.PartialDefinitionPart ?? property;
            case IFieldSymbol field:
                return field.CorrespondingTupleField is not null && field.ContainingType.IsTupleType
                    ? null
                    : field.OriginalDefinition;
            case IEventSymbol e:
                return e.OriginalDefinition;
            case INamedTypeSymbol type:
                return type.IsTupleType || type.IsAnonymousType ? null : type.OriginalDefinition;
            default:
                return null;
        }
    }

    /// <summary>The key of a normalized symbol, or null when it has no documentation id (an anonymous or
    /// error symbol).</summary>
    public static string? Key(ISymbol? symbol)
    {
        if (symbol?.ContainingAssembly is null)
        {
            return null;
        }

        string? id = DocumentationCommentId.CreateDeclarationId(symbol);
        return id is null ? null : $"{symbol.ContainingAssembly.Name}|{id}";
    }

    /// <summary>The type a reference to <paramref name="symbol"/> depends on: itself for a type, its container for a
    /// member.</summary>
    public static INamedTypeSymbol? TargetType(ISymbol symbol) =>
        symbol as INamedTypeSymbol ?? symbol.ContainingType?.OriginalDefinition;

    /// <summary>The full metadata name (<c>Ns.Outer+Nested`1</c>) — what the built assembly's TypeDefinition table
    /// calls the same type, so the population check compares like with like.</summary>
    public static string MetadataName(INamedTypeSymbol type)
    {
        var chain = new Stack<string>();
        for (INamedTypeSymbol? t = type; t is not null; t = t.ContainingType)
        {
            chain.Push(t.MetadataName);
        }

        string nested = string.Join("+", chain);
        INamespaceSymbol ns = type.ContainingNamespace;
        return ns is null || ns.IsGlobalNamespace ? nested : $"{ns.ToDisplayString()}.{nested}";
    }

    /// <summary>The keys of <paramref name="type"/> and of every type enclosing it, innermost first.</summary>
    public static IReadOnlyList<string> Chain(INamedTypeSymbol type)
    {
        var keys = new List<string>();
        for (INamedTypeSymbol? t = type.OriginalDefinition; t is not null; t = t.ContainingType?.OriginalDefinition)
        {
            if (Key(t) is { } key)
            {
                keys.Add(key);
            }
        }

        return keys;
    }
}
