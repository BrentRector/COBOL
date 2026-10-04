// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Binding.Model;

/// <summary>The name-resolution SCOPE of one lookup (P6 Step 7): <see cref="Program"/> for program/object-level
/// code, or a METHOD scope carrying the method's own name overlay (ISO §11.7.4 GR5 — method-local names SHADOW
/// object/program names and are invisible to sibling methods). The scope is an EXPLICIT parameter of every
/// <see cref="SymbolTable"/> lookup — the "which overload" decision the old <c>LookupData</c>/
/// <c>LookupDataInScopeOf</c> pair encoded in the METHOD NAME is now data.</summary>
public readonly record struct Scope(OoMethodDataScope? Method)
{
    /// <summary>The program/object-level scope (no method overlay).</summary>
    public static Scope Program => new((OoMethodDataScope?)null);
}

/// <summary>
/// THE ONE scope-aware name resolver (P6 Step 7 — collapses the <c>LookupData</c> / <c>LookupDataInScopeOf</c> /
/// <c>TryGetVisibleIndexField</c> / <c>IndexFieldFor</c> quadruple; the singular-pattern fix). Semantics are the
/// quadruple's, verbatim:
/// <list type="bullet">
/// <item>§8.4.6.2.1 rule 3a / §11.7.4 GR5 — a method-local name REPLACES (never unions with) the object/program
/// name: a lookup consults the scope's method overlay FIRST and falls through to the global maps only when the
/// overlay has NO entry for the name. ⛔ §11.7.4 GR5 VERBATIM, re-derived on this tree (kb/Work PB467 doubted
/// it after probing for wording GR5 does not use): "If a given user-defined word is defined in the data division
/// of this method definition and in the data division of the containing object definition, the use of that word
/// in this method refers to the declaration in this method. The declaration in the containing object definition
/// is inaccessible to this method." The rule is real, it is this clause, and it says exactly what this table
/// implements.</item>
/// <item>§8.4.6.2.3 — a method-local DATA-name shadows an object-level INDEX-name of the same spelling
/// (<see cref="IndexCandidates"/> returns null; without this every index-first consumer would silently
/// bind the subscript/SET target to the OBJECT's index cell — a torn read/write of the wrong storage).</item>
/// <item>§11.7.4 GR5 index privacy — a method-local index-name has its OWN cell, never the shared global one.</item>
/// <item>§8.4.6.2.1 3) for a CONTAINED program — the unit map holds its own names and every container's global
/// names, each at its nesting depth, and <see cref="NearestDeclaring{T}"/> narrows a qualified candidate set to the
/// nearest declaring source element (kb/Work PB1047 / PB1243).</item>
/// </list>
/// <para>Backed by the owning <see cref="DataBinder"/>'s live name maps (the P6 Step-7a wrapper stage — no data
/// moves; <c>SymbolTableBuilder</c>-owned storage is deferred to P7 per the phase doc). One table per binder:
/// COBOL name scopes are PER-UNIT (each program/class forest has its own namespace), so the table lives on
/// <see cref="DataBinder.Symbols"/> rather than the compilation record — recorded as a deviation from the
/// PHASE-06 doc's single <c>BoundCompilation.Symbols</c> sketch, which presumed a merged namespace that does
/// not exist.</para>
/// </summary>
public sealed class SymbolTable
{
    private readonly DataBinder _data;

    internal SymbolTable(DataBinder data) => _data = data;

    /// <summary>Resolve a DATA-name in <paramref name="scope"/>: the method overlay first (§8.4.6.2.1 rule 3a),
    /// else the unit's global multimap. False when the name is unknown in both. (← <c>LookupData</c> /
    /// <c>LookupDataInScopeOf</c> — the anchor-root-vs-active-method decision is now the caller's explicit
    /// <see cref="Scope"/>.)</summary>
    public bool TryResolve(string name, Scope scope, out List<DataItem> items)
    {
        if (scope.Method is { } m && m.ByName.TryGetValue(name, out var mlist) && mlist.Count > 0)
        {
            items = mlist;
            return true;
        }
        if (_data.ByName.TryGetValue(name, out var list) && list.Count > 0)
        {
            items = list;
            return true;
        }
        items = [];
        return false;
    }

    /// <summary>⛔ ISO §8.4.6.2.1 3) — THE NEAREST-DECLARING-ELEMENT RULE for data-names and condition-names
    /// (kb/Work PB1047 / PB1243), the data-name twin of <see cref="IndexNameRegistry.Candidates"/>'s depth tiers. A
    /// contained program's name set holds its own names AND every container's global names (rule 1), so after the
    /// reference's qualifiers have been applied over that WHOLE set (rule 1: "the normal rules for qualification …
    /// are applied until one or more items is identified"), a plural survivor set is narrowed to the survivors
    /// declared in the NEAREST source element: "a) If the name is declared in source element B, the item in source
    /// element B is the referenced item. b) Otherwise … 1. The item in source element A if the name is declared in
    /// source element A. 2. The item in the containing source element …". Two survivors in that one element are still
    /// ambiguous — the count stays the caller's verdict. <paramref name="declarationOf"/> maps a candidate to the data
    /// item that places it (a condition-name is placed by its conditional variable). Returns
    /// <paramref name="candidates"/> itself when there is nothing to narrow — one survivor, or a unit that inherits
    /// no global data — so the common reference allocates nothing.</summary>
    public List<T> NearestDeclaring<T>(List<T> candidates, Func<T, DataItem> declarationOf)
    {
        if (candidates.Count < 2 || !_data.InheritsGlobalData) return candidates;
        int nearest = int.MaxValue;
        bool mixed = false;
        foreach (var c in candidates)
        {
            int depth = _data.DeclaringDepth(declarationOf(c));
            if (nearest != int.MaxValue && depth != nearest) mixed = true;
            if (depth < nearest) nearest = depth;
        }
        if (!mixed) return candidates;
        List<T> tier = [];
        foreach (var c in candidates)
            if (_data.DeclaringDepth(declarationOf(c)) == nearest) tier.Add(c);
        return tier;
    }

    /// <summary>The candidates an UNQUALIFIED data-name reference can name: <see cref="TryResolve"/>'s set narrowed
    /// by <see cref="NearestDeclaring{T}"/> (ISO §8.4.6.2.1 3) — with no qualifier to apply, the nearest declaring
    /// source element's items are the whole set). ⛔ A caller that picks among the candidates BY KIND (the
    /// FUNCTION-POINTER of §8.4.3.2.3 SR4, a dynamic-capacity table) filters THIS set, never the raw one: filtering
    /// first skips a nearer declaration of another kind — or counts a container's hidden global of the same kind as
    /// a rival — and so resolves the name to an item §8.4.6.2.1 3) a) makes inaccessible (wave 69 Z, found by the
    /// train-68b review on <c>IntrinsicBinder.FunctionPointerNamed</c>).</summary>
    public bool TryResolveUnqualified(string name, Scope scope, out List<DataItem> items)
    {
        if (!TryResolve(name, scope, out items)) return false;
        items = NearestDeclaring(items, static i => i);
        return true;
    }

    /// <summary>Resolve a level-88 CONDITION-name in <paramref name="scope"/>: the method overlay first, else the
    /// unit's global multimap (the same §8.4.6.2.1 rule-3a precedence the data-name lookup applies).</summary>
    public bool TryResolveCondition(string name, Scope scope, out List<Condition88> conds)
    {
        if (scope.Method is { } m && m.Conditions.TryGetValue(name, out var mlist) && mlist.Count > 0)
        {
            conds = mlist;
            return true;
        }
        if (_data.Conditions.TryGetValue(name, out var list) && list.Count > 0)
        {
            conds = list;
            return true;
        }
        conds = [];
        return false;
    }

    /// <summary>⛔ THE ONE INDEX-NAME RESOLUTION (kb/Work PB919) — the §8.4.2.2 candidate set of a WRITTEN
    /// index-name reference <c>name [OF|IN q] …</c> (§8.4.2.2.2 Format 3), or <see langword="null"/> when the
    /// spelling names no index-name visible in <paramref name="scope"/> (the reference is then a data-name one).
    /// Visibility is the data-name's (§8.4.6.2.3): a method-local DATA-name of the spelling shadows every
    /// index-name (§11.7.4 GR5 — the caller must not treat the reference as an index); else the method's own
    /// declarations (§11.7.4 GR5 privacy); else the unit's, where a nearer source element's declaration wins
    /// (§8.4.6.2.1 3), <see cref="IndexNameRegistry.Candidates"/>). The qualifiers are matched from the TABLE
    /// upward (§8.4.2.2.3 SR6). The COUNT is the caller's verdict — <see cref="DataBinder.UniqueOrReportAmbiguous{T}"/>
    /// — so two tables' <c>INDEXED BY IX</c> referenced as a bare <c>IX</c> is §8.4.2.2.3 SR1's ambiguity, never a
    /// silently shared cell.
    /// <para>A table's OWN declared index (SEARCH's first index-name, the SET/PERFORM of a known table) is read off
    /// <see cref="DataItem.Indexes"/> directly — it is a declaration, not a reference, so it needs no
    /// resolution.</para></summary>
    public NameCandidates<IndexDeclaration>? IndexCandidates(string name, IReadOnlyList<string> qualifiers, Scope scope)
    {
        if (scope.Method is { } m)
        {
            if (m.ByName.TryGetValue(name, out var mlist) && mlist.Count > 0) return null;   // the method data-name wins
            if (m.IndexNames.Declares(name))
                return m.IndexNames.Candidates(name, d => _data.IndexQualifierChainMatches(d, qualifiers));
        }
        if (!_data.IndexNames.Declares(name)) return null;
        var indexes = _data.IndexNames.Candidates(name, d => _data.IndexQualifierChainMatches(d, qualifiers),
            out int indexDepth);
        // §8.4.6.2.1 3) across the two name classes (kb/Work PB1047): a DATA-name of the spelling that the written
        // qualifiers reach, declared in a NEARER source element than every such index-name, is the referenced item —
        // a contained program's `01 IX` hides its container's GLOBAL table's `INDEXED BY IX`, exactly as its
        // `01 X` hides the container's global `X`. At EQUAL depth the index-name keeps the reference, as before.
        if (indexes.Count > 0 && NearerDataNameHides(name, qualifiers, indexDepth)) return null;
        return indexes;
    }

    /// <summary>⛔ ISO §8.4.6.2.1 3) ACROSS NAME CLASSES — the ONE answer to "does an ordinary DATA-name of this spelling,
    /// reached by the written <paramref name="qualifiers"/>, sit in a source element NEARER than
    /// <paramref name="depth"/>?" A name class resolved BEFORE the ordinary data-name lookup — an index-name
    /// (kb/Work PB1047), a named OCCURS DYNAMIC CAPACITY register (kb/Work PB1674) — asks it of its own nearest
    /// candidate, and yields the reference to the data-name when the answer is yes: a contained program's `01 IX` or
    /// `01 CAP` hides its container's global table's index-name or register exactly as its `01 X` hides the
    /// container's global `X`. At EQUAL depth the other class keeps the reference. Free for a unit that inherits
    /// nothing (every candidate is then depth 0).</summary>
    public bool NearerDataNameHides(string name, IReadOnlyList<string> qualifiers, int depth)
    {
        if (depth == 0 || !_data.InheritsGlobalData || !_data.ByName.TryGetValue(name, out var dataNames)) return false;
        foreach (var item in dataNames)
            if (_data.DeclaringDepth(item) < depth && _data.QualifierChainMatches(item, qualifiers))
                return true;
        return false;
    }
}
