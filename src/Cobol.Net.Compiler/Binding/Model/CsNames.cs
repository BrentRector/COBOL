// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>
/// THE ONE C# NAME ALLOCATOR for a member named after a COBOL word (docs/rearchitecture/DESIGN-external-repository.md
/// §4.5; kb/Work PB2093). Uniqueness bites on a REFERENCE, not on a declaration (ISO §8.4.2.1: "In order to use a
/// resource, a statement shall contain a reference that uniquely identifies that resource"), so two subordinates of
/// one group may share a data-name; and a data-name may legally be spelled like a member the emitter
/// itself writes into the same C# type — a subordinate named <c>AsImage</c> or <c>Equals</c>, a record named
/// <c>CloseFiles</c>. So every such name is ALLOCATED in a <see cref="CsNameScope"/> that is SEEDED with the scope's
/// reserved names: the base name if it is free, else the smallest free <c>_2</c>, <c>_3</c>, … in source order. The
/// COBOL word itself is untouched (<see cref="DataItem.CobolName"/>); the C# name is display only.
/// <para>⛔ A NEW MEMBER THE EMITTER WRITES INTO A RECORD STRUCT OR A PROGRAM CLASS IS ADDED TO ITS SET HERE, in the
/// same change — <c>CsNameReservationDriftTests</c> reads every member declaration the code generator writes and is
/// red when one is spelled like a COBOL word and is in neither set.</para>
/// <para>⛔ EVERY SCOPE IS ALSO SEEDED WITH <see cref="ImportedTypeNames"/> (kb/Work PB3270): the generated file imports
/// <c>System</c> and the runtime's namespaces and names their types by SIMPLE name (<c>CobolString.Store(…)</c>,
/// <c>HostLanding.LandText(…)</c>, <c>Int128</c>), and a member, field or parameter spelled like one of them shadows the
/// type for every such reference in its scope — the CS errors that rejected legal COBOL.</para>
/// </summary>
public static class CsNames
{
    /// <summary>The members a group's <c>record struct</c> carries that are not its COBOL members: the image and
    /// leaf-vector facilities the emitter writes (<c>RecordStructEmitter</c>, <c>GroupImageCodec</c>) and the members
    /// C# synthesizes for every <c>record struct</c> (a field spelled like one is CS0102 — the PB2093 crash).</summary>
    public static readonly FrozenSet<string> RecordStructMembers = FrozenSet.ToFrozenSet(
    [
        // written by the emitter (RecordStructEmitter.EmitLeafMethods; GroupImageCodec)
        "AsImage", "FromImage", "AsBits", "FromBits", "AsNat", "FromNat", "CurrentImage", "AsVarImage", "FromVarImage",
        "CurrentExtents", "FromContiguousImage", "AsLeaves", "OfLeaves",
        // synthesized by C# for a record struct (and the operators' metadata names)
        "Equals", "GetHashCode", "ToString", "PrintMembers", "Deconstruct", "EqualityContract",
        "op_Equality", "op_Inequality",
    ], StringComparer.Ordinal);

    /// <summary>The members a program class carries that are not its records: the <c>ICobolProgram</c> surface the
    /// emitter implements (<c>ProgramEmitter</c>; <c>CobolNet.Runtime.ICobolProgram</c>).</summary>
    public static readonly FrozenSet<string> ProgramClassMembers = FrozenSet.ToFrozenSet(
        ["Call", "CloseFiles", "EndStorage", "Activate", "DescribeExternals"], StringComparer.Ordinal);

    /// <summary>The members a generated HOST-ENTRY class carries (<c>HostEntryEmitter</c>; DESIGN-external-repository
    /// §4.3): <c>Call</c>, the .NET counterpart of a CALL statement, and <c>Cancel</c>, of a CANCEL statement. Its
    /// published record structs AND the <c>Call</c> parameters are allocated in the same scope — the <c>Call</c> body
    /// names each record type by its simple name, which a parameter of that spelling would shadow — so a description
    /// whose word spells any of them is given the next free name.</summary>
    public static readonly FrozenSet<string> HostEntryMembers = FrozenSet.ToFrozenSet(["Call", "Cancel"], StringComparer.Ordinal);

    /// <summary>The namespaces the generated file's using directives import (<c>ProgramEmitter.Emit</c> writes each as
    /// <c>using global::&lt;namespace&gt;;</c>, the runtime's inside the module's namespace), so the types of each are named
    /// by SIMPLE name in the emitted C#. The ONE list <see cref="ImportedTypeNames"/> is derived from;
    /// <c>ImportedTypeNameDriftTests</c> is red when the emitter writes a using directive naming a namespace that is
    /// not here.</summary>
    public static readonly IReadOnlyList<string> ImportedNamespaces =
    [
        nameof(System),
        typeof(ICobolProgram).Namespace!,                                          // the substrates and the inter-program ABI
        typeof(CobolNet.Runtime.IO.CobolFile).Namespace!,                          // the file-I/O facade
        typeof(CobolNet.Runtime.Exceptions.CobolFatalException).Namespace!,        // the EC signal type
        typeof(CobolNet.Runtime.Globalization.CharacterClassification).Namespace!, // OBJECT-COMPUTER CHARACTER CLASSIFICATION
    ];

    /// <summary>The simple name of every type the generated file's using directives import
    /// (<see cref="ImportedNamespaces"/>): the public, non-nested types of those namespaces in the runtime assembly and,
    /// for <c>System</c>, in the core library the generated code is compiled against. DERIVED by reflection, never listed
    /// by hand, so a type added to the runtime is reserved by the build that adds it. A COBOL word spelled like one is
    /// given the next free name by <see cref="Allocate"/> in every <see cref="CsNameScope"/> — the type it would shadow
    /// is named by simple name in the scope's code. Case-sensitive, as C# is: <c>HostLanding</c> is reserved,
    /// <c>HOSTLANDING</c> is a different identifier.</summary>
    public static readonly FrozenSet<string> ImportedTypeNames = DeriveImportedTypeNames();

    private static FrozenSet<string> DeriveImportedTypeNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var runtimeNamespaces = ImportedNamespaces.Where(static n => n != nameof(System)).ToHashSet(StringComparer.Ordinal);
        foreach (Type t in typeof(ICobolProgram).Assembly.GetExportedTypes())
            if (!t.IsNested && t.Namespace is { } ns && runtimeNamespaces.Contains(ns))
                names.Add(SimpleName(t));
        foreach (Type t in typeof(object).Assembly.GetExportedTypes())
            if (!t.IsNested && t.Namespace == nameof(System))
                names.Add(SimpleName(t));
        return names.ToFrozenSet(StringComparer.Ordinal);
    }

    /// <summary>A type's name as C# source writes it: without a generic type's arity (<c>Func`1</c> is written <c>Func</c>).</summary>
    private static string SimpleName(Type t)
    {
        int tick = t.Name.IndexOf('`');
        return tick < 0 ? t.Name : t.Name[..tick];
    }

    /// <summary>The namespace every type a module emits lives in (docs/rearchitecture/DESIGN-external-repository.md
    /// §4.5; kb/Work PB2097): <c>Cobol.&lt;S&gt;</c>, where <c>&lt;S&gt;</c> is the assembly simple name made ONE C#
    /// identifier by <see cref="DataItem.Sanitize"/> (each character an identifier cannot hold, <c>.</c> included,
    /// written <c>_uXXXX_</c>; a leading digit prefixed; a keyword escaped), so <c>PAY.V2.dll</c> is
    /// <c>Cobol.PAY_u002E_V2</c> and never meets a type <c>V2</c> in <c>Cobol.PAY</c>, and no emitted type lives directly
    /// in <c>Cobol</c>. Two COBOL assemblies one host references therefore never collide on a program or class name or on
    /// <c>__CobolModule</c>.</summary>
    public static ModuleNamespace ModuleNamespaceOf(string assemblySimpleName)
    {
        string segment = DataItem.Sanitize(assemblySimpleName);
        return new ModuleNamespace($"Cobol.{segment}", $"Cobol.{segment.TrimStart('@')}");
    }

    /// <summary>Allocate <paramref name="baseName"/> (a <see cref="DataItem.Sanitize"/>d word, or a synthesized base
    /// such as <c>_impliedRecordF</c>) in <paramref name="scope"/>: itself when it is free, else the smallest free
    /// <c>baseName_n</c>, n ≥ 2. The result is claimed, so a later allocation in the scope never returns it.</summary>
    public static string Allocate(string baseName, CsNameScope scope)
    {
        string name = baseName;
        for (int n = 2; !scope.TryClaim(name); n++) name = $"{baseName}_{n}";
        return name;
    }
}

/// <summary>A module's namespace (<see cref="CsNames.ModuleNamespaceOf"/>) in its two spellings.</summary>
/// <param name="CsName">The C# spelling the generated source declares (<c>namespace Cobol.@class;</c> for an assembly
/// named <c>class</c>).</param>
/// <param name="ClrName">The metadata name a reflection lookup or an attribute string names (<c>Cobol.class</c>).</param>
public sealed record ModuleNamespace(string CsName, string ClrName)
{
    /// <summary>The module's registrar type, as metadata names it: what <c>[assembly: CobolRepository(Registrar = …)]</c>
    /// records and <c>RegisterModule</c> keys the run unit's registration set on (design §11.2).</summary>
    public string RegistrarClrName => $"{ClrName}.{RegistrarClass}";

    /// <summary>The registrar class's simple name.</summary>
    public const string RegistrarClass = "__CobolModule";
}

/// <summary>One C# name scope (the members of one record struct, or of one program class): the names already
/// claimed in it, seeded with the scope's reserved names (<see cref="CsNames"/>). Names are allocated only through
/// <see cref="CsNames.Allocate"/>.</summary>
public sealed class CsNameScope
{
    /// <summary>The names every scope of a kind holds before it claims one: the kind's own members and
    /// <see cref="CsNames.ImportedTypeNames"/>. Shared, never copied — a program with a thousand groups makes a thousand
    /// record-struct scopes.</summary>
    private readonly FrozenSet<string> _reserved;

    private readonly HashSet<string> _claimed;

    private CsNameScope(FrozenSet<string> reserved, IEnumerable<string> claimed)
    {
        _reserved = reserved;
        _claimed = new HashSet<string>(claimed, StringComparer.Ordinal);
    }

    private static readonly FrozenSet<string> RecordStructReserved =
        CsNames.RecordStructMembers.Union(CsNames.ImportedTypeNames).ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> ProgramClassReserved =
        CsNames.ProgramClassMembers.Union(CsNames.ImportedTypeNames).ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> HostEntryReserved =
        CsNames.HostEntryMembers.Union(CsNames.ImportedTypeNames).ToFrozenSet(StringComparer.Ordinal);

    /// <summary>A record struct's member scope (<see cref="CsNames.RecordStructMembers"/> and
    /// <see cref="CsNames.ImportedTypeNames"/>).</summary>
    public static CsNameScope RecordStruct() => new(RecordStructReserved, []);

    /// <summary>A program class's member scope (<see cref="CsNames.ProgramClassMembers"/> and
    /// <see cref="CsNames.ImportedTypeNames"/>).</summary>
    public static CsNameScope ProgramClass() => new(ProgramClassReserved, []);

    /// <summary>A host-entry class's member scope (<see cref="CsNames.HostEntryMembers"/>), seeded with the class's
    /// own name, which no member may spell (CS0542; DESIGN-external-repository §4.3 "Names"). Its <c>Call</c>
    /// parameters are allocated in it too, so a parameter never shadows a published record type the body names.</summary>
    public static CsNameScope HostEntry(string className) => new(HostEntryReserved, [className]);

    /// <summary>A module namespace's type scope (DESIGN-external-repository §4.5): the host-entry classes are allocated in
    /// it, seeded with every type name the module already declares there (<paramref name="reserved"/> — the
    /// registrar, the entry class, the atom table, the program classes and the OO classes and interfaces) and with
    /// <see cref="CsNames.ImportedTypeNames"/>: a host class spelled like an imported type would capture that type's
    /// simple name for the whole namespace, since a namespace's own types win over a using directive's.</summary>
    public static CsNameScope ModuleTypes(IEnumerable<string> reserved) => new(CsNames.ImportedTypeNames, reserved);

    /// <summary>Claim names this scope already holds from elsewhere — a container's GLOBAL roots, bridged into this
    /// unit's class under their own member names (kb/Work PB1047).</summary>
    public void Reserve(IEnumerable<string> names) => _claimed.UnionWith(names);

    /// <summary>Claim <paramref name="name"/> when it is free; false when the scope already holds it.</summary>
    internal bool TryClaim(string name) => !_reserved.Contains(name) && _claimed.Add(name);
}
