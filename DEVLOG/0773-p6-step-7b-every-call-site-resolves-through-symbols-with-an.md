## Entry 773 — 2026-07-11 11:18 PDT — P6 Step 7b: every call site resolves through Symbols with an EXPLICIT Scope; the lookup quadruple is DELETED

The phase's headline risk closes. All ~16 quadruple call sites migrated: the StatementBinder sites (SEARCH/
SEARCH ALL table + index-cell resolution, IndexFieldOf, RefFailure, INITIALIZE dynamic-table + RENAMES checks,
SORT Format-2 table, the intrinsic data-item-wins + bare-index-argument checks) and ReferenceResolver (recv-item,
unqualified index) pass `data.ActiveScope`; OdoModel's DEPENDING resolution passes `ScopeOf(RootOf(item))` — the
scope decision the old method NAMES encoded is now visible data at each site. `LookupData`,
`LookupDataInScopeOf`, `TryGetVisibleIndexField`, `IndexFieldFor` are deleted; grep returns doc-comment history
only (exit criterion #3). One shape-note: `LookupData(x) is { Count: > 0 }` collapsed to `TryResolve(x, scope,
out _)` — TryResolve's true arm guarantees a non-empty list, same predicate.

The gate the doc calls the phase's most important: FULL conformance 3158 green (all OoSpineTests method-scope
shadowing goldens within), unit 281, characterization 32/32 BYTE-IDENTICAL (a shadowing regression would show as
a changed .g.cs storage reference — none moved), FULL legacy guard NIST 353 MATCH. Residual noted for P7's
decomposition (not 7b scope, recorded): ReferenceResolver.ResolveUnqualified:378 and StatementBinder:1766 carry
the same overlay-first precedence INLINE as their own resolution cores — candidates to route through
Symbols.TryResolve/TryResolveCondition when P7 splits the resolver (the shapes are identical; migrating them is
behavior-neutral but touches the resolver's hot path, better done under P7's review).

