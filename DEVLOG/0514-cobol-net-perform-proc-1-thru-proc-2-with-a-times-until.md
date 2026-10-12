## Entry 514 — 2026-06-09 14:20 PDT — COBOL.NET: PERFORM proc-1 THRU proc-2 with a TIMES/UNTIL phrase must iterate the range (ISO §14.9.28 GR9) — NC106A/NC134A/NC176A GREEN

Targeting the closest compile-but-mismatch programs from the Entry-513 sweep (NC106A / NC176A, diff 10 each), the
failure presented as a COMP ON SIZE ERROR not firing: `SUBTRACT 1 FROM WRK-CS-03V00 ON SIZE ERROR` (WRK-CS-03V00 is
`PIC S999 COMP`) never reported overflow as the field drained past −999. **Validated to the spec first:** §14.7.5
case 3 — a size error exists when the result is "further from zero than permitted for the associated resultant data
item"; the PICTURE digit count governs the permitted range regardless of USAGE, so −1000 in an S999 COMP is a size
error. A minimal single-step repro CONFIRMED COBOL.NET already does this correctly (`TryStore` bounds by
`Pow10(Digits)`, and a plain COMP profile is `NumericTruncation.DigitCount`). So the size-error path was not the bug.

The real defect was the **drain loop**: `PERFORM SUB-A-F1-20 THRU SUB-B-F1-20 200 TIMES` executed the range ONCE, so
the field only reached −981 and never overflowed. **Spec (§14.9.28):** GR4 — the out-of-line set of statements is
proc-1's first statement through proc-2's last (the THRU range); GR9 — with the TIMES phrase that set is performed
N times. The grammar parses the optional control phrase in two shapes — a direct child (`PERFORM proc TIMES`) or
wrapped in `performOptions` (the `PERFORM proc THRU proc [performOptions]` alternative and the inline form) — and
`StatementBinder.BindPerform` read only the DIRECT accessor, so when THRU was present the TIMES/UNTIL phrase lived
under `performOptions` and was dropped to `PerformOnce`. The generated C# made it visible: `PERFORM A THRU B 5 TIMES`
emitted a bare `__Dispatch(1, 2);` (no `for`), while `PERFORM A 5 TIMES` emitted the counted loop.

**Fix (singular pattern):** one `BindPerformControl(PerformStatementContext)` resolver replaces the two divergent
control-binding paths (the inline `BindControl` and the out-of-line inline expression). It resolves the phrase from
the direct child OR `performOptions().FirstOrDefault()`, so TIMES / UNTIL / VARYING bind identically for inline,
out-of-line, and out-of-line-THRU PERFORM. `PERFORM A THRU B 5 TIMES` now emits the `for` loop and runs the range
N times. (PERFORM VARYING out-of-line remains a separate, pre-existing loud gap.)

This greened **NC106A, NC176A, and NC134A** (NC134A was diff 118 — also a THRU-range loop). The corpus sweep went
12 → 15 byte-matching NC programs with zero regressions. **A coverage gap let this through:** ControlFlowDifferential
had `Perform_Times_OutOfLine` and `Perform_Thru` but never the *combination* — exactly the resume-prompt's
verification-confidence caveat (hand-picked tests dodge the untested spot).

**Tests — SPEC-ANCHORED:** `ControlFlowDifferentialTests` +2 (`Perform_Thru_Times_RunsRangeNTimes` → 033 = 3 × the
range per §14.9.28 GR9; `Perform_Thru_Until_RunsRangeUntilCondition` → 009 per GR10), each asserting the §-derived
value then cross-checking the legacy. NC106A/NC176A/NC134A locked into `NistDifferentialTests`. Conformance 287 →
292; 14 unit; greenfield-only.

NOTE (CI gap, for follow-up): CI runs only the LEGACY guard (`scripts/guard.sh`) + the CobolSharp unit/integration
suites — the greenfield `Cobol.Net.Tests.Conformance`/`Unit` (the active work) are NOT run in CI, so these
regressions are guarded locally only. Worth adding the greenfield suites to the workflow.

