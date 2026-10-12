## Entry 260 — Symbol-based control transfer + return-address PERFORM…THRU → SQ114A/NC102A/NC208A

Took on the entangled pair flagged in Entry 259. Two distinct bugs, fixed together because they overlap
in NC102A (a half-measure regresses it):

**(1) Duplicate paragraph names across sections — make all control transfer symbol-based.** Each
paragraph has a distinct `ParagraphSymbol` and its own true index (`ParagraphSymbolIndices`), but for
duplicate names `ParagraphIndices[name]` collapses to the last-defined one. Fall-through and PERFORM were
already symbol-based; the **dispatch table order** (Binder) and **GO TO / GO TO DEPENDING** resolution
(ControlFlowLowerer) were name-based — so for duplicate names they disagreed with each other and with
fall-through. Fixed: `ParagraphDispatchOrder` now built from `ParagraphSymbolMethods[para.Symbol]`
(position == true index), and GO TO / GO TO DEPENDING resolve through a new
`TryResolveParagraphIndex(ParagraphSymbol)` helper (`ParagraphSymbolIndices` first, name fallback). The
bound GO TO already carries the section-scope-qualified target symbol (`BoundGoToStatement.Targets`,
resolved in `BindGoTo`), so a *qualified* GO TO now lands on the right duplicate.

**(2) Inverted / non-contiguous PERFORM…THRU — replace the physical-range model with a return-address
model via a single shared dispatch helper.** The old `EmitPerformThru` ran a switch over the physical
range `[start,end]` and exited the moment `pc` left that range. That is wrong for two real CCVS patterns:
a GO TO that leaves the range and returns (exited the PERFORM prematurely), and an **inverted** range
`PERFORM proc-1 THRU proc-2` where proc-2 physically precedes proc-1 (ISO §14.9.30 allows it — enter at
proc-1, return when proc-2 falls off its end). The lowerer even *swapped* inverted ranges into a
contiguous `[min,max]` block, executing entirely the wrong paragraphs (NC102A `PFM-TEST-F1-10` → "RETURN
MECHANISM LOST"). Replaced with one shared `Dispatch(int startPc, int exitPc)` static helper per program
(`CilEmitter.EmitDispatchHelper`): it switches over the FULL paragraph table, follows each paragraph's
returned next-pc anywhere, and returns only when the exit paragraph (`exitPc`) completes by falling
through to `exitPc+1` — a return-address model, not a range model. The **main program loop** now calls
`Dispatch(EntryParagraphIndex, -1)` (no exit paragraph → runs until STOP RUN/off-end), and every
PERFORM…THRU calls `Dispatch(trueStart, trueEnd)` (the swap was removed; `IrPerformThru.Paragraphs` is now
vestigial). PERFORM single stays a direct call; PERFORM N TIMES / VARYING / UNTIL THRU all funnel through
the same helper. STOP RUN/EXIT semantics preserved exactly (paragraph returns −1 → helper returns −1 → the
PERFORM site discards it, as before; the IR's "throws StopRunException" comment is stale — it returns −1).

**Results — full guard ALL GREEN, 1000 unit / 347 integration / 214 NIST, zero regressions:**
- **SQ114A** (duplicate names): hang → **15/15**, baselined.
- **NC102A** (inverted THRU + dup names): vacuous 39/39 → **42/42** — `PFM-TEST-F1-10 PERFORM GO TO PARAS`
  now PASSes, plus 3 previously-skipped tests. Baseline regenerated.
- **NC208A** (qualified GO TO to a duplicate name — `GO TO PAR-3B IN QUAL-SECTION-1`): the poster child
  for fix #1. Its prior baseline was a **latent failing capture** — `023 OF 024, 1 FAILED` — that slipped
  past the guard because the failing test's detail line was suppressed (so `grep FAIL*` saw 0). Now
  **024 OF 024, 0 FAILED**; baseline regenerated. (Notes a guard-criterion gap: it counts `FAIL*` detail
  lines, not the footer "TEST(S) FAILED" total — a pre-existing baseline-quality hole, not addressed here.)
- SQ105A 22/22 and SQ213A 7/7 (Entry 259) preserved.

One unit test needed updating (not a behavior change): `CilEmitterDecompositionTests` asserted CilEmitter
contains `EmitParagraphDispatchInline`, which was renamed to `EmitDispatchHelper`.

