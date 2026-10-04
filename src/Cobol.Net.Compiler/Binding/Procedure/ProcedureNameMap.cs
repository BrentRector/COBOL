// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// ⛔ THE ONE procedure-name declaration map — a name → definition map that <b>REMEMBERS THAT A NAME WAS
/// DECLARED MORE THAN ONCE</b> (kb/Work PB466).
/// <para><b>Why the type exists.</b> Every procedure-name scope here used a bare
/// <c>Dictionary&lt;string, T&gt;.TryAdd</c>, whose contract is "keep the first, DISCARD the rest silently". A
/// map built that way cannot fail to diagnose ambiguity — it cannot even <i>represent</i> it: by the time
/// anything could ask "is this spelling unique?", the multiplicity is gone. ISO §8.4.2.2.1 makes that question
/// the whole of reference resolution — "uniqueness shall be established through qualification for each
/// user-defined name explicitly referenced", excused by rule 1 ("No other name has the identical spelling") or,
/// for a paragraph, rule 6 ("the section containing the reference also contains the named paragraph") — and
/// §8.4.2.2.3 SR1 repeats it as a syntax rule. So the declaration side owes the resolution side the COUNT, and
/// it is one type's job to keep it rather than five call sites' habit.</para>
/// <para><b>Shape.</b> The first declaration lives in <see cref="_first"/> — the hot map, one probe, exactly the
/// dictionary that was there before. A repeated spelling additionally lands in <see cref="_duplicated"/>, which
/// is EMPTY for every well-formed source unit, so <see cref="IsDuplicated"/> costs one <c>Count</c> test on the
/// ordinary path and <see cref="Definitions"/> allocates nothing until a program actually collides. The
/// duplicate list carries the first definition too, so a diagnostic can name every candidate in declaration
/// order without re-walking the pc space.</para>
/// <para>Ordinal-case-insensitive by construction: a COBOL word's spelling is case-insensitive (§8.1.2), and the
/// comparer is fixed HERE rather than passed in, so no scope can be built with a different one.</para>
/// </summary>
/// <typeparam name="T">What a declaration resolves to — a pc for a paragraph, a
/// <see cref="SectionInfo"/> for a section.</typeparam>
internal sealed class ProcedureNameMap<T>
{
    private readonly Dictionary<string, T> _first = new(CobolNames.Comparer);

    /// <summary>Every definition of a spelling declared MORE THAN ONCE, in declaration order (including the
    /// first). Absent — and the dictionary itself empty — for a source unit with no duplicated procedure-name,
    /// which is the overwhelmingly common case.</summary>
    private readonly Dictionary<string, List<T>> _duplicated = new(CobolNames.Comparer);

    /// <summary>Record one declaration. A repeat is KEPT (that is the entire point), not discarded: the first
    /// stays the one <see cref="TryResolve"/> answers with, and the spelling becomes
    /// <see cref="IsDuplicated"/>.</summary>
    public void Declare(string name, T definition)
    {
        if (_first.TryAdd(name, definition)) return;
        if (!_duplicated.TryGetValue(name, out var all)) _duplicated[name] = all = [_first[name]];
        all.Add(definition);
    }

    /// <summary>The FIRST declaration of <paramref name="name"/>. ⛔ Says nothing about uniqueness — a caller
    /// that resolves a REFERENCE shall ask <see cref="IsDuplicated"/> too (§8.4.2.2.1).</summary>
    public bool TryResolve(string name, out T definition) => _first.TryGetValue(name, out definition!);

    /// <summary>True when this scope declares <paramref name="name"/> more than once — §8.4.2.2.1 rule 1 is then
    /// false for it and qualification is required (or, within a section, §8.4.2.2.3 SR7 is violated outright).</summary>
    public bool IsDuplicated(string name) => _duplicated.Count != 0 && _duplicated.ContainsKey(name);

    /// <summary>How many times this scope declares <paramref name="name"/> — 0, 1, or the duplicate count. The
    /// candidate arithmetic of §8.4.2.2.1 rule 1 is a SUM over the scopes a reference can reach (paragraphs plus
    /// sections), so the count, not a boolean, is what a resolver needs.</summary>
    public int CountOf(string name) =>
        _duplicated.TryGetValue(name, out var all) ? all.Count : _first.ContainsKey(name) ? 1 : 0;

    /// <summary>Every definition of <paramref name="name"/>, in declaration order — for naming the candidates in
    /// an ambiguity diagnostic. Empty when the name is not declared here; a single-element view (never a new
    /// list) when it is unique.</summary>
    public IReadOnlyList<T> Definitions(string name) =>
        _duplicated.TryGetValue(name, out var all) ? all
        : _first.TryGetValue(name, out var one) ? new[] { one }
        : [];
}
