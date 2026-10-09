// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;

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
    private readonly HashSet<string> _claimed;

    private CsNameScope(IEnumerable<string> reserved) => _claimed = new HashSet<string>(reserved, StringComparer.Ordinal);

    /// <summary>A record struct's member scope (<see cref="CsNames.RecordStructMembers"/>).</summary>
    public static CsNameScope RecordStruct() => new(CsNames.RecordStructMembers);

    /// <summary>A program class's member scope (<see cref="CsNames.ProgramClassMembers"/>).</summary>
    public static CsNameScope ProgramClass() => new(CsNames.ProgramClassMembers);

    /// <summary>Claim names this scope already holds from elsewhere — a container's GLOBAL roots, bridged into this
    /// unit's class under their own member names (kb/Work PB1047).</summary>
    public void Reserve(IEnumerable<string> names) => _claimed.UnionWith(names);

    /// <summary>Claim <paramref name="name"/> when it is free; false when the scope already holds it.</summary>
    internal bool TryClaim(string name) => _claimed.Add(name);
}
