## Entry 611 - 2026-07-04 22:35 PDT - Phase 4 track (e) arithmetic: the PROHIBITED-inexact edited-receiver cure + ARITHMETIC IS STANDARD positive behavior

The first Phase-4 implementation track (the smallest, per the DEVLOG-610 reconciliation's wave sizing) -
two spec-grounded fixes, both closing PENDING 2014 goldens.

**M2-ARITH-1 - the PROHIBITED-inexact leak (a real correctness bug).** `COMPUTE X ROUNDED MODE IS
PROHIBITED = 2.25` into `X PIC 9.9` silently truncated to 2.2 and reported NO size error - it must raise
EC-SIZE-TRUNCATION and leave X unchanged (ISO §14.7.4.3 r7). Root cause: the numeric-EDITED store path
(CSharpEmitter.StoreArith) called plain `CobolNum.Rescale(..., Prohibited)`, whose `RoundDiv` truncates
silently under Prohibited - while the NUMERIC (non-edited) path uses `TryStore`'s `IsInexactAtScale`
check and the standard-decimal path's `CobolDec.ToUnscaled` throws. The edited path was the ONE category
that didn't detect the inexactness. Cure: new `CobolNum.RescaleChecked` (throws CobolSizeError on
Prohibited-inexact, mirroring the other two categories); the emitter's edited CHECKED branch uses it
(the unchecked branch stays silent, matching the numeric Store path's no-phrase behavior). The fix lives
in the ONE shared StoreArith edited branch, so it holds across COMPUTE / ADD-GIVING / DIVIDE / every
verb - verified by adversarial probe (feedback_scan_all_similar: the fix IS the pattern, at the
chokepoint). rounded_mode_prohibited.cob (2014) ENABLED; OnSizeErrorDifferentialTests +5 (a 3-verb
theory + the exact counterpart).

**M2-ARITH-2 - ARITHMETIC IS STANDARD positive behavior (track e's ratified mandate).** Plain STANDARD
was staged loud (0807). Per §8.8.1.2/§8.8.1.4, STANDARD performs operations in the standard intermediate
data item, which for FIXED-POINT operands IS the standard DECIMAL form - so STANDARD and STANDARD-DECIMAL
produce identical results for non-float operands. Implemented by routing STANDARD to the same CobolDec
engine (NumericRenderer.StandardDecimal now matches both modes) at <=2014; STANDARD was DROPPED by 2023
(§8.8.1 names only NATIVE/STANDARD-BINARY/STANDARD-DECIMAL) so the removed-feature 0807 stays at --std
2023. The float-operand divergence (STANDARD may use an IEEE-binary intermediate) rides the deferred
float USAGE families (phase 6). options_paragraph.cob (2014) rebased from a bare parse test into a REAL
behavior test: `2 / 7 * 7` = 2.00000 under STANDARD (the intermediate keeps full precision) vs 1.99997
native - proving the routing genuinely engages the decimal engine.

**Reconciliation flipped:** M2-ARITH-1 → LANDED, M2-ARITH-2 → STANDARD landed (float residual → phase 6)
in docs/PHASE4_RECONCILIATION.md.

**Battery:** greenfield conformance **1598/1598** (+6: the 2 enabled goldens + 5 OnSizeError tests, net of
the strengthened options golden) · unit **101/101** · guard-fast **ALL GREEN** (551 integration; no grammar
change ⇒ guard-fast, not the FULL guard). The frozen-legacy ConformanceTests can't reproduce the
STANDARD-as-decimal routing (it gives native 1.99997), so options_paragraph (2014) joined the
GreenfieldOnly set — the same protocol as the OO goldens.


