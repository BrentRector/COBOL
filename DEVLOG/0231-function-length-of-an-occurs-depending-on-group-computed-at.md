## Entry 231 — FUNCTION LENGTH of an OCCURS DEPENDING ON group computed at runtime (the last open spec-gap bug)

Closes the one genuine open bug from the spec-conformance sweep (spec-gaps.md #3, DEVLOG 229/230):
`FUNCTION LENGTH` of a group containing a subordinate `OCCURS … DEPENDING ON` table returned the
**maximum** layout instead of the **current** depending-on length.

Reproduced (`/e/tmp/repro/ODOLEN.cob`): `ELT PIC X(4) OCCURS 1 TO 10 DEPENDING ON N` inside group
`TBL`; `FUNCTION LENGTH(TBL)` returned 40 for both N=3 and N=7 — spec requires 12 and 28
(ISO §15.50.4 rule 4(b): a subordinate DEPENDING ON makes the length follow the OCCURS rules for a
*sending* item; rule 7: a variable-length group's length sums the fixed parts plus each subordinate
table's length at its *current* capacity).

Root cause: `ExpressionBinder.BindLength`/`StaticLength` folded `FUNCTION LENGTH` to the compile-time
constant `Symbol.ElementSize` — which `StorageLayoutComputer` sets to the **max** layout
(`childrenSize` already includes the ODO child's `elementSize × MaxOccurs`) — with no ODO branch.

Fix (`ExpressionBinder.cs`): `BindLength` now, for an identifier argument, calls the new
`BuildVariableLengthExpression`, which returns a **runtime** expression when the argument has any
subordinate ODO table and null otherwise (so non-ODO operands keep the constant fold). The runtime
value reuses the existing static max layout and subtracts each variable table's unused tail:

  length = maxLength − Σ over subordinate ODO tables T of (maxOccurs_T − depValue_T) × repetition_T × elementSize_T

`CollectDependingTables` walks the subtree gathering each `OCCURS … DEPENDING ON` table together with
its **repetition factor** — the product of the (fixed) OCCURS counts of the tables enclosing it —
so a variable table nested inside fixed OCCURS levels is counted once per enclosing occurrence
(e.g. `HDR X(5)` + `GRP OCCURS 3` over `ELT X(4) OCCURS 1 TO 10 DEPENDING ON N` →
5 + 3×(N×4) = 5 + 12N, not the max 125). The depending-on symbol is already resolved before binding
(`DataItemClassifier.Validate` runs at Compilation.cs:70, before the binder at :75); a null
DependingOnSymbol falls back to the constant fold. RENAMES (66) / condition-name (88) entries are
skipped (aliases, not storage).

Scope notes: the `LENGTH OF` special register does **not** exist in the grammar (only
`FUNCTION LENGTH`), so it is out of scope. Complex ODO (a DEPENDING ON table nested *inside* another
DEPENDING ON table — COBOL-2002+) uses the inner table's maximum repetition rather than the outer
current count; documented as an approximation, and disallowed in the default COBOL-85 dialect anyway.

Verified end-to-end (`ODOLEN.cob` → 12/28; `ODOLEN2.cob` mixed-header/nested → 41/89; fixed group
and elementary item unchanged at their constant sizes). Two guarding tests added
(`IntrinsicFunctionTests.Function_Length_OdoGroup_UsesCurrentDependingValue` and
`…_WithFixedHeaderAndNesting`), emitting both lengths on one line to stay newline-agnostic.

Guard ALL GREEN: 1000 unit / 345 integration (+2; +1 unrelated skip) / 148 NIST baselines 0 FAIL*.
spec-gaps.md updated — all three originally-suspected silent-correctness bugs are now resolved.

