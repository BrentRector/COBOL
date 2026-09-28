// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>⛔ ONE INDEX-NAME DECLARATION — the identity an index-name reference resolves to (kb/Work PB919).
/// §14.9.39.4 GR1 associates an index-name with the table whose OCCURS clause's INDEXED BY phrase specifies it, and
/// §8.4.2.2.3 SR6 qualifies it through that table: "The qualification of an index-name may include the name of the
/// table with which the index-name is associated, as well as any name by which that table may be qualified." So
/// the declaration is the pair (name, table), and it owns its OWN cell.
/// <para>⛔ THE SHAPE THIS REPLACED WAS KEYED BY SPELLING. The unit held <c>name → cell</c>, de-duplicated on
/// insert, so two tables that each declared <c>INDEXED BY IX</c> silently SHARED one C# cell, a written
/// <c>IX</c> compiled with no §8.4.2.2.3 SR1 ambiguity diagnostic, and the SR6 form that disambiguates it
/// (<c>IX OF EA</c>) was "not defined". A TYPE referenced twice cloned one index-name onto two tables and was
/// refused outright (the former COBOLNET1531 stage) for the same reason.</para></summary>
/// <param name="Name">The index-name as declared (compared case-insensitively, §8.1.3.2 GR3 a)).</param>
/// <param name="Table">The OCCURS entry whose INDEXED BY phrase declares it — the first qualifier SR6 admits.</param>
/// <param name="Cell">The C# <c>long</c> member holding the occurrence number (COBOLNET_DESIGN §3.5).</param>
public sealed record IndexDeclaration(string Name, DataItem Table, string Cell);

/// <summary>⛔ THE INDEX-NAME NAMESPACE OF ONE SCOPE (a unit, or a method's §11.7.4 GR5 overlay) — every
/// declaration VISIBLE there, each tagged with the nesting distance of the source element that declares it
/// (0 = this one; n = the n-th containing program, whose GLOBAL tables §8.4.6.2.3 makes visible: "the scope of an
/// index-name is identical to that of the data-name that names the table"). <see cref="Own"/> is what this scope
/// must EMIT; an inherited declaration's cell is reached through the container's bridge.</summary>
public sealed class IndexNameRegistry
{
    private readonly Dictionary<string, List<(IndexDeclaration Decl, int Depth)>> _byName =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IndexDeclaration> _own = [];

    /// <summary>The declarations this scope itself makes, in declaration order — the cells it emits.</summary>
    public IReadOnlyList<IndexDeclaration> Own => _own;

    /// <summary>Record a declaration made by this scope's own data division.</summary>
    public void Declare(IndexDeclaration decl)
    {
        _own.Add(decl);
        Add(decl, 0);
    }

    /// <summary>Record a GLOBAL declaration of a containing source element <paramref name="depth"/> levels out.</summary>
    public void Inherit(IndexDeclaration decl, int depth) => Add(decl, depth);

    private void Add(IndexDeclaration decl, int depth)
    {
        if (!_byName.TryGetValue(decl.Name, out var list)) _byName[decl.Name] = list = [];
        list.Add((decl, depth));
    }

    /// <summary>True when some visible declaration carries the spelling — the reference IS an index-name reference
    /// (qualified correctly or not), never a data-name one.</summary>
    public bool Declares(string name) => _byName.ContainsKey(name);

    /// <summary>The §8.4.2.2 candidate set of a written index-name reference: the visible declarations of
    /// <paramref name="name"/> that <paramref name="qualifies"/> admits, then ISO §8.4.6.2.1's rule for a name
    /// duplicated across nested source elements — "3) If more than one item is identified, no more than one of them
    /// may have a name local to source element B … a) If the name is declared in source element B, the item in
    /// source element B is the referenced item", else the nearest containing element that declares it (3 b). So the
    /// survivors at the NEAREST depth are the set; two there are still ambiguous.</summary>
    public NameCandidates<IndexDeclaration> Candidates(string name, Func<IndexDeclaration, bool> qualifies) =>
        Candidates(name, qualifies, out _);

    /// <summary><see cref="Candidates(string, Func{IndexDeclaration, bool})"/>, also reporting the nesting distance of
    /// the source element the survivors belong to (<see cref="int.MaxValue"/> when none survive) — what
    /// <see cref="SymbolTable.IndexCandidates"/> weighs against a same-spelled DATA-name's (kb/Work PB1047).</summary>
    public NameCandidates<IndexDeclaration> Candidates(string name, Func<IndexDeclaration, bool> qualifies,
                                                       out int nearestDepth)
    {
        nearestDepth = int.MaxValue;
        if (!_byName.TryGetValue(name, out var list)) return new([]);
        int nearest = int.MaxValue;
        foreach (var (d, depth) in list)
            if (depth < nearest && qualifies(d)) nearest = depth;
        List<IndexDeclaration> hits = [];
        foreach (var (d, depth) in list)
            if (depth == nearest && qualifies(d)) hits.Add(d);
        nearestDepth = nearest;
        return new(hits);
    }
}
