## Entry 811 — 2026-07-11 19:00 PDT — P7 Step 10a: the DEVLOG-773 pickups — the last two SymbolTable-bypassing lookups routed through `data.Symbols`

**What (the audit's ordering rule: shrink the ambient-state surface FIRST, before any binder extraction).**
The two residual inline lookups that duplicated the SymbolTable's §8.4.6.2.1 rule-3a overlay-first precedence
now route through the ONE scope-aware resolver: `ReferenceResolver.ResolveUnqualified` →
`data.Symbols.TryResolve(name, data.ActiveScope)` and `StatementBinder.ConditionOf`'s inline
overlay-else-global head → `Symbols.TryResolveCondition`. Byte-for-byte the same precedence (method overlay
when non-empty, else the unit multimap, else null) — the duplication is gone, and `ActiveMethodScope` now has
the SymbolTable as the ONE lookup reader (the Step-10 scoped-EnterMethodScope conversion later rides on
that). Behavior-neutral, hot path — proven by the FULL battery per DEVLOG 773's requirement.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

