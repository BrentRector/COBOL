## Entry 772 — 2026-07-11 11:02 PDT — P6 Step 7a: the scope-aware SymbolTable lands; the lookup quadruple becomes thin shims (byte-equivalent, proven by the full battery)

The singular-pattern fix begins. `Binding/Model/SymbolTable.cs` is THE one scope-aware resolver:
`TryResolve(name, Scope)` (data-names — §8.4.6.2.1 rule 3a method-overlay-first, REPLACES never unions),
`TryResolveCondition`, `TryResolveIndex` (the §8.4.6.2.3 data-name-shadows-index-name visibility check), and
`IndexCellOf` (the resolved-cell accessor). `Scope` is a readonly record struct carrying the `OoMethodDataScope?`
— the "which overload" decision `LookupData` vs `LookupDataInScopeOf` encoded in a METHOD NAME is now DATA
(`DataBinder.ActiveScope` / `ScopeOf(anchorRoot)`).

Two recorded deviations from the phase doc, both semantics-driven: (1) ONE table PER BINDER
(`DataBinder.Symbols`), not a single `BoundCompilation.Symbols` — COBOL name scopes are per-unit; the doc's
single-table sketch presumed a merged namespace that doesn't exist. (2) `IndexCellOf` stays a SEPARATE member
beside `TryResolveIndex` rather than folding into it: `IndexFieldFor`'s callers pass a TABLE'S DECLARED
index-name (never a user-written reference) where the data-name-shadow check does not apply — folding the shapes
would CHANGE behavior when a method data-name shares an object table's index spelling. The quadruple had two
index members for a reason; the fold keeps ONE resolver with two documented shapes.

The four quadruple methods are now one-line shims over `Symbols` — every call site untouched, so the full battery
IS the cross-check that the folded resolver is byte-equivalent: conformance 3158, characterization 32/32
byte-identical, unit 281 (+5 `SymbolTableTests` — the doc's five scope/shadowing cases (a)–(e), real items from a
bound fixture + hand-built method overlays pinning the precedence rules in isolation), legacy guard green.
Step 7b (migrate call sites with explicit Scopes, delete the quadruple) is next — the OO method-scope goldens are
its gate.

