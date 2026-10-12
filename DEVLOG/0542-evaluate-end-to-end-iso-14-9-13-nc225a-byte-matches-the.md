## Entry 542 — 2026-06-10 11:36 PDT — EVALUATE end-to-end (ISO §14.9.13) → NC225A byte-matches; the partial-class per-verb file pattern is live

**What:** EVALUATE bound at compile time to a chained `if / else if / else` — no dispatch tables, readable generated
C# (COBOLNET_DESIGN §5.3). New disjoint files `StatementBinder.Evaluate.cs` + `CSharpEmitter.Evaluate.cs` — the
template for the parallel verb waves: `StatementBinder` and `CSharpEmitter` are now `sealed partial`, each verb owns
its own bound records + binder partial + emitter partial, and only two one-line dispatch arms touch the shared files.

**Semantics (all from §14.9.13):** WHEN-phrase matches OR over a shared body (consecutive WHEN phrases, GR — the 1985
form); per phrase the subject↔object pairs AND together positionally across ALSO (SR: object count = subject count);
objects are operands (equality), THRU ranges (inclusive bounds, GR5), conditions (against TRUE/FALSE subjects — FALSE
negates), or ANY (an empty `BoundLogical("&&",[])` rendered `true` — new ConditionRenderer arm); a leading NOT negates
the whole group; WHEN OTHER is the else tail. **The subtle one: CONDITIONAL subjects.** `EVALUATE X NUMERIC WHEN
TRUE` — the subject carries its own class test; and `… ALSO IT-IS-81 ALSO TRUE ALSO FALSE WHEN … ALSO TRUE ALSO …` —
a level-88 condition-name as a selection subject (§14.9.13 selection subject `condition-1`), paired with TRUE/FALSE
objects. `SubjectCondition` recognizes both forms (class test → `BoundClassCondition`; sole 88 data-ref →
`ConditionOf` + `refs.ResolveForItem` → `BoundCondition88`, the exact resolution path condition-name conditions use)
and `SoleBooleanLiteral` walks the object's single-child parse chain to the TRUE/FALSE literal — object TRUE selects
the subject condition, FALSE its negation. Two loud-at-run-time rounds found both forms in NC225A's 6-subject ALSO
matrices before they were implemented (the loud-value discipline doing its job).

**Result:** NC225A GREEN (the EVALUATE feature program — 29 GF tests: value/literal/expression subjects, THRU,
NOT, ALSO matrices, ANY, TRUE/FALSE both directions) → locked as InlineData #55. 475 conformance green. NC219A
still DIFF 16 (its remainder is collating — step ③, not EVALUATE).

