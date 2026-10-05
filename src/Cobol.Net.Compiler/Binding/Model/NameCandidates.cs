// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>⛔ THE ISO §8.4.2.2 CANDIDATE SET OF A QUALIFIABLE NAME REFERENCE — AND IT HAS NO INDEXER (kb/Work PB978;
/// generalized over the name class by kb/Work PB919). §8.4.2.2.1 makes the number of survivors the whole answer
/// ("uniqueness shall be established through qualification for each user-defined name explicitly referenced";
/// §8.4.2.2.3 SR1 — "uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity
/// of reference"): one survivor resolves, and zero or several is a resolution failure. Every DATA DIVISION resolver
/// that returned a <c>List&lt;DataItem&gt;</c> invited its caller to read <c>[0]</c>, and three did — the report
/// binder's CONTROL / SOURCE / SUM lookup, the OCCURS DEPENDING ON fallback and the file-control key and ASSIGN
/// USING operands — so an ambiguous reference compiled clean and ran on whichever item was declared first. This type
/// is the fix the way <see cref="ReferenceResolver.ProbeResult"/> was PB221's: the set can be COUNTED and NARROWED,
/// and its one member read only when there is exactly one (<see cref="Single"/>). The first-declared member is
/// reachable only through <see cref="DataBinder.UniqueOrReportAmbiguous{T}"/>, the ONE ambiguity verdict, which
/// grants it solely under <c>--permissive</c> (the procedure division's disposition, kb/Work R33).
/// <para>⛔ ONE TYPE FOR EVERY NAME CLASS SR1 COVERS, NOT ONE PER CLASS. SR1 is written about "each non unique
/// user-defined name", so the data-name (<see cref="DataItem"/>) and index-name (<see cref="IndexDeclaration"/>)
/// sets are the same type over different members and meet the same verdict — kb/Work PB919 measured the cost of
/// the alternative: index-names had no candidate set at all, and two tables' <c>INDEXED BY IX</c> silently shared
/// one cell.</para></summary>
public readonly struct NameCandidates<T> where T : class
{
    private readonly List<T>? _items;

    internal NameCandidates(List<T> items) => _items = items;

    /// <summary>How many declarations survive the reference's qualification.</summary>
    public int Count => _items?.Count ?? 0;

    /// <summary>The declaration the reference identifies — non-null only when exactly one survives.</summary>
    public T? Single => Count == 1 ? _items![0] : null;

    /// <summary>The survivors a clause's OWN rule narrows the set to (e.g. §12.4.5.12 SR2's "a record of this
    /// file"); the result is counted like any other set.</summary>
    public NameCandidates<T> Where(Func<T, bool> predicate) =>
        new(_items is null ? [] : _items.Where(predicate).ToList());

    /// <summary>Whether any survivor satisfies <paramref name="predicate"/>.</summary>
    internal bool Any(Func<T, bool> predicate) => _items is not null && _items.Exists(i => predicate(i));

    /// <summary>The survivors followed by <paramref name="more"/> — further declarations that COUNT for the
    /// uniqueness verdict (ISO §8.4.2.2.1 rule 4's lapsed type-declaration members); the existing survivors keep
    /// their places, so <see cref="FirstDeclaredForPermissive"/> is unchanged.</summary>
    internal NameCandidates<T> Including(IReadOnlyCollection<T> more) =>
        more.Count == 0 ? this : new([.. _items ?? [], .. more]);

    /// <summary>The first-declared survivor — read ONLY by <see cref="DataBinder.UniqueOrReportAmbiguous{T}"/>'s
    /// <c>--permissive</c> arm (<c>DataNameResolutionDriftTests</c> holds every other reader out).</summary>
    internal T FirstDeclaredForPermissive => _items![0];
}
