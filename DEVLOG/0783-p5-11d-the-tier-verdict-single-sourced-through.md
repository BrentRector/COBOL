## Entry 783 — 2026-07-11 13:47 PDT — P5.11d: the tier verdict single-sourced through `RedefinesClass.Classify`; a guard-race false alarm (transparency)

**What.** PHASE-05 Step 11d (DESIGN-data-model §2.3):
- `RedefinesClass.Tier` / `.Width` / `.RejectReason` → PRIVATE setters; ONE named mutator
  `Classify(tier, width, rejectReason)` with exactly two documented callers: `ClassifyRedefinesClasses` applies
  the §13.18.44 verdict once per class (the `ComputeTier` reason table carries the citations — the
  float/COMP-5/BINARY-*/INDEX Tier-C island §13.18.60, the 2-byte national overlay D-N1/D-N2, the dynamic-table
  §13.18.44 SR5), and `ForceStringCanonical` — the ONE cell forcer — RE-classifies for the EXTERNAL/BASED/
  ADDRESS-OF cell re-base (§13.18.22.4 GR5; its accept leg now also CLEARS a stale RejectReason, previously
  left behind when force-accepting a formerly-rejected class). `DataItem.ClassOffset` → `internal set`, its one
  writer `AssignClassOffsets` named in the doc (the P5.10 `Storage` pattern — init-only inexpressible).
- **DEVIATIONS (recorded in the phase ledger):** (1) strict "init-only, written once by the classifier" is
  UNIMPLEMENTABLE — the cell forcer's re-classification is a REAL second write, by design; the honest landing is
  the single named mutator. (2) The doc's "~10 scattered inline Tier-C guards to delete" (stale line refs) do
  not exist as tier VERDICTS today: Phase 1E narrowed Tier-C to the float/COMP-5/BINARY-*/INDEX island inside
  `ComputeTier` (already the one verdict), and the emitter's per-verb "Tier-C byte island" loud guards key off
  `DataItem.IsImageCapable` — the whole-group-image capability fact, one definition, NOT scattered re-inference.
  (3) A `TierCWindow.Read/Write` throw-backstop adds nothing: a Tier-C class is Rejected at classification and
  every VIEW resolve over a Rejected class already fails loud in `ReferenceResolver` (:243/:285); the
  `StorageForm.TierCWindow` case stays as the (unreachable-today) P11 seam.

**Transparency — a guard RACE false alarm.** The 11c guard run reported 2 NIST regressions (351/353) and my
`&&`-chained commit+push fired on the tail's exit code, not the verdict — 11c went out against a red guard line.
Root cause of the "regressions": I had begun the 11d source edits WHILE guard-fast's parallel NIST leg (JOBS=32,
~4 min) was compiling from the LIVE tree — two programs compiled against a half-edited source. The identical
tree re-run clean: 353 MATCH, 0 regressions; CI for the pushed 11c is green both configs. TWO lessons, both now
in the guard memory: (1) never edit sources while guard-fast runs — stage-then-edit is NOT enough, the NIST leg
reads the working tree; (2) never `&&`-chain a commit onto a guard tail — gate on the VERDICT line.

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit · 32 characterization; legacy guard 353 MATCH
+ legacy unit 1196 + integration 609 (the full-output re-run against this exact tree).

