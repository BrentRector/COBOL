## Entry 1116 - 2026-07-29 20:05 PDT - I read the batch before it finished, and published 3778 when the answer was 3781

**CORRECTION TO ENTRY 1115, and the mistake is the useful part.**

1115 reported the first Phase-B batch as **GAP 3790 → 3778**, 23 CONFORMS with 12 closed. That was a **MID-FLIGHT
READ**. The workflow is a two-stage pipeline — adjudicate, then an independent agent told to OVERTURN — and I
polled the scratchpad, saw all 11 `out-*.json` files present, and merged. The out file is written by stage one and
REWRITTEN by stage two. Files existing is not files finished.

The workflow's completion notification arrived after I had committed and pushed. Re-running the merge against the
final artifacts moved **10 rows**:

    RV-15.70.4-1  CONFORMS → PARTIAL          RV-15.71.4-2  CONFORMS → PARTIAL
    RV-15.72.4-3  CONFORMS → PARTIAL          RV-15.76.4-1  CONFORMS → PARTIAL
    AR-15.77.3-2  CONFORMS → PARTIAL          RV-15.78.4-1  CONFORMS → PARTIAL
    AR-15.79.3-1  CONFORMS → PARTIAL          AR-15.79.3-4  CONFORMS → PARTIAL
    AR-15.79.3-5  CONFORMS → DIVERGES         AR-15.75.3-4  CONFORMS → NEEDS-OWNER-DECISION

**Three of those were rows I had counted as CLOSED.** The true figures: **GAP 3781**, 9 closed — 13 CONFORMS
(9 closed · 4 CONFORMS-but-untested) · 19 DIVERGES · 21 PARTIAL · 2 NEEDS-OWNER-DECISION. Plan §0, the fix queue
and the inventory now carry the corrected numbers; 1115 stays as written with this entry as its correction, since
the DEVLOG is a narrative and rewriting it would hide the error rather than record it.

**⛔ WHAT SAVED IT, AND WHAT DID NOT.** The gate did not catch this and could not: every one of those rows was
internally consistent, its symbols resolved, its state matched its verdict. A stale-but-valid batch is invisible
to a referential integrity check. What caught it was the completion notification — the one signal I had bypassed.
The rule going forward is flat: **wait for the workflow's completion notification; never poll its output
directory for readiness.** A pipeline's intermediate artifact is indistinguishable from its final one.

⚠ And note the direction of the error: every single overturn was a DOWNGRADE. The adversarial pass only ever
removes confidence, so reading a two-stage pipeline early is not a neutral race — it systematically reports a
better result than the truth. That is the worst possible bias for a completion metric.

**THE REFUTE STAGE PAID FOR ITSELF SEVERAL TIMES OVER**, which is the other half of the lesson. A sample of what
the second pass produced that the first missed:

· **`RV-15.70.4-1` ORD, CONFORMS → PARTIAL — a real defect the first agent excused.** It saw the `c & 0xFF`
  masking in the alphabet builder, charged it to §12.3.7 and reasoned "ORD faithfully reports whatever the table
  holds". The refuter read the runtime instead: `Ord(s, weights)` is `c < weights.Length ? weights[c]+1 : c+1`, so
  a code unit past the 256-entry table never consults the collating sequence at all. It then RAN two programs:
  with `ALPHABET AL IS "A" ALSO "B"`, `ORD(X"FF")` gives 255 and `ORD(U+0100)` gives 257 — skipping 256, where
  §12.3.7 GR7 k3 requires distinct ascending positions with no gap. And the correct arithmetic **already exists on
  the NATIONAL twin**, which returns the right value for the identical alphabet. One rule, two implementations,
  one wrong. Filed as PB3 — and it means **CA26's Unicode fix (DEVLOG 1018) is incomplete on the ORD path**.
· **`AR-15.7.3-1` ABS — verdict survived, but the note contained a false claim**: "No CONFORMANCE-FIX-QUEUE item
  owns this yet." PB1 already named that rule id. Left standing it would have invited a duplicate queue entry.
· **Editions widened on the ABS rows, 2014,2023 → 2002,2014,2023, for a reason worth keeping.** The first agent
  derived the edition window from `IntrinsicCatalog`'s own ABS row — deriving the answer from the code under
  review, the inherited-citation failure in a new costume. The refuter established 85's exclusion on real evidence
  (the 1989 Intrinsic Function Module is the 42 functions of NIST `IF101A`..`IF142A`, matching the catalog's 42
  `IntroducedIn: 85` rows exactly; ABS is in neither) and left 2002 IN, because excluding it asserts something
  nobody established — and `cobol --std 2002` rejects `FUNCTION ABS` today, which is a §4.2.2 violation if ABS is
  a 2002 function. An honest open question beats a confident guess.
· A refuter also caught **`cite.py --check 4.2.2 "shall be indicated"` FAILING** — the first agent's §4.2.2 hook
  was paragraph 3, not the phrase it implied. Exactly the mechanical-validation rule doing its job one level down.

**A SECOND PATTERN CAME OUT OF THE COMPLETED DATA, which the mid-flight read had also obscured.** Clustering the
42 open rows by root cause: **11 are PB1** (the dead argument-class table), **19 are PB2** — a FLOATING-POINT
argument falling off the end of the intrinsic result path, producing no value, a Roslyn `CS1503`, or a silent
requantization — **1 is PB3**, and only **11 are genuinely per-function**. Three named seams own three quarters of
the batch. That ratio is the argument for the whole exercise: 42 rows of symptoms, three places to fix.

**GATES.** Inventory re-merged from the final artifacts; `SpecTraceabilityInventoryDriftTests` 10/10 over all 55
adjudicated rows; probe and writer agree at 3781 / 4 test-needed. No compiler code changed.

