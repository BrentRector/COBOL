// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace CobolNet.Tools.ArchCensus;

/// <summary>
/// The other half of the population check: the type definitions of a project's BUILT assembly, read from its
/// metadata tables with <c>System.Reflection.Metadata</c> — a reader independent of the Roslyn workspace, so a
/// project, a document or a source generator the workspace failed to load shows up as a difference instead of as a
/// smaller census. Types the C# compiler synthesizes are not authored and are left out by ONE stated rule: the
/// <c>&lt;Module&gt;</c> pseudo-type and any type carrying <c>[CompilerGenerated]</c> (closures, iterators, async
/// state machines, anonymous types, <c>&lt;PrivateImplementationDetails&gt;</c>, the embedded attributes).
/// Names are full metadata names (<c>Ns.Outer+Nested`1</c>), matching <see cref="SymbolKeys.MetadataName"/>.
/// </summary>
internal static class CompiledPopulation
{
    public static IReadOnlyList<string> Read(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new CensusRefusal($"the built assembly is missing (build the solution first): {assemblyPath}");
        }

        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        MetadataReader md = pe.GetMetadataReader();
        var names = new List<string>();
        foreach (TypeDefinitionHandle handle in md.TypeDefinitions)
        {
            if (Authored(md, handle))
            {
                names.Add(FullName(md, handle));
            }
        }

        names.Sort(StringComparer.Ordinal);
        return names;
    }

    private static bool Authored(MetadataReader md, TypeDefinitionHandle handle)
    {
        for (TypeDefinitionHandle h = handle; !h.IsNil; h = md.GetTypeDefinition(h).GetDeclaringType())
        {
            TypeDefinition t = md.GetTypeDefinition(h);
            if (md.GetString(t.Name) == "<Module>" || IsCompilerGenerated(md, t))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCompilerGenerated(MetadataReader md, TypeDefinition type)
    {
        foreach (CustomAttributeHandle a in type.GetCustomAttributes())
        {
            EntityHandle ctor = md.GetCustomAttribute(a).Constructor;
            EntityHandle attributeType = ctor.Kind switch
            {
                HandleKind.MemberReference => md.GetMemberReference((MemberReferenceHandle)ctor).Parent,
                HandleKind.MethodDefinition => md.GetMethodDefinition((MethodDefinitionHandle)ctor).GetDeclaringType(),
                _ => default,
            };
            string name = attributeType.Kind switch
            {
                HandleKind.TypeReference => md.GetString(md.GetTypeReference((TypeReferenceHandle)attributeType).Name),
                HandleKind.TypeDefinition => md.GetString(md.GetTypeDefinition((TypeDefinitionHandle)attributeType).Name),
                _ => "",
            };
            if (name == "CompilerGeneratedAttribute")
            {
                return true;
            }
        }

        return false;
    }

    private static string FullName(MetadataReader md, TypeDefinitionHandle handle)
    {
        TypeDefinition t = md.GetTypeDefinition(handle);
        string name = md.GetString(t.Name);
        TypeDefinitionHandle outer = t.GetDeclaringType();
        if (!outer.IsNil)
        {
            return $"{FullName(md, outer)}+{name}";
        }

        string ns = md.GetString(t.Namespace);
        return ns.Length == 0 ? name : $"{ns}.{name}";
    }
}
