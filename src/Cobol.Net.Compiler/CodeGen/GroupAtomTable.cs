// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

/// <summary>
/// ⛔ THE ONE PLACE A §8.5.1.12 ATOM ARRAY IS WRITTEN INTO EMITTED C# (kb/Work PB2690). Every shape a statement or an
/// activation boundary hands the runtime — <c>CobolVarGroup.Reshape</c> / <c>Overlay</c> / <c>Compare</c>, the
/// fixed ⇄ variable-length conversions, <c>CobolArg.Atoms</c>, a formal's adapter — is a <see cref="GroupAtom"/> array
/// that the compiler knows in full, so the module states each DISTINCT array ONCE as a <c>static readonly</c> field of
/// <c>__GroupAtoms</c> and every use names the field. Written inline, a literal allocated its array and one record per
/// atom on every execution of a variable-length MOVE, comparison or CALL; and the runtime's per-pair caches (the
/// correspondence walk and the carrier geometry, <c>CobolVarGroup</c>) key on the array's reference, which an inline
/// literal never repeats.
/// <para>One instance per run unit, shared by every per-unit <see cref="Emit.EmitContext"/> like the
/// <see cref="NameAllocator"/>, and emitted once, after every unit, by <see cref="Emit"/>. Arrays are identified by their
/// rendered text (<see cref="RuntimeApi.GroupAtomsNew"/>), so two descriptions of one shape share a field.</para>
/// </summary>
internal sealed class GroupAtomTable
{
    /// <summary>The emitted static class that holds the arrays. A name no COBOL word spells (a user word never begins with
    /// an underscore followed by a letter, ISO §8.3.2.1), like the module registrar's.</summary>
    public const string ClassName = "__GroupAtoms";

    private readonly Dictionary<string, string> _fieldOfLiteral = new(StringComparer.Ordinal);
    private readonly List<(string Field, string Literal)> _fields = [];

    /// <summary>The C# expression naming the field that holds <paramref name="atoms"/>; the first use of a shape adds
    /// the field.</summary>
    public string Ref(GroupAtom[] atoms)
    {
        string literal = RuntimeApi.GroupAtomsNew(atoms);
        if (!_fieldOfLiteral.TryGetValue(literal, out string? field))
        {
            field = $"A{_fields.Count}";
            _fieldOfLiteral[literal] = field;
            _fields.Add((field, literal));
        }
        return $"{ClassName}.{field}";
    }

    /// <summary>Write the class holding every array <see cref="Ref"/> named — nothing when none was. Called once, after the
    /// last unit has been emitted.</summary>
    public void Emit(CodeWriter w)
    {
        if (_fields.Count == 0) return;
        using (w.Block($"internal static class {ClassName}"))
            foreach (var (field, literal) in _fields)
                w.Line($"internal static readonly {nameof(GroupAtom)}[] {field} = {literal};");
        w.Line();
    }
}
