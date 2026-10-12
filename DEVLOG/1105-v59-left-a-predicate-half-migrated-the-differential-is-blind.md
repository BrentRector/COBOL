## Entry 1105 - 2026-07-29 14:05 PDT - V59 left a predicate half-migrated, the differential is blind to output, and a fan-out reported a false clean

Three findings, none of which was the thing I set out to do.

**THE DIFFERENTIAL IS RE-RUN AND CLEAN - AND MY PREDICTION ABOUT IT WAS WRONG.** I had written into plan section 0
that DA2 would move the GnuCOBOL per-case verdicts, because it changed how a numeric FUNCTION renders in every
string context and GnuCOBOL renders those differently on purpose. Re-ran it against the preserved before-image:
**0 per-case flips, 0 regressions**, totals identical at 474/173/572/104 over 1323 cases. The prediction was not
merely pessimistic, it was structurally impossible: the differential's verdict is a COMPILE-TIME accept/reject
comparison, so it CANNOT see a change in runtime output. The affected cases were already AGREE_ACCEPT because they
always compiled; DA2 changed what they print and turned a runtime throw into a success, and neither is visible to
this net. Corrected in plan section 0 with the reason, because "re-run the differential" was about to become
standing advice for a class of change it can never validate. For output semantics the instrument is the goldens.

**V59 LEFT ITS IMAGE PREDICATE HALF-MIGRATED - MY OWN DEBT, FOUND BY REVIEWING MY OWN WORK.** V59 introduced
`DataItem.IsImageCapable` (a BINARY/PACKED leaf HAS a pinned byte image, so a group containing one qualifies; only
float / COMP-5 / INDEX are genuinely imageless) beside the pre-V59 `DataItem.IsCharacterImage` (a COMP/binary leaf
qualifies only via `StoreAsImage` promotion). `TierCIsland` documents the pair as a deliberate P1/P2 distinction -
and that WAS true before V59.

What makes it a defect now is `RecordStructEmitter.cs:123`: it emits `AsImage()`/`FromImage()` for exactly
`IsImageCapable`. So the codec EXISTS for a plain COMP group, while nine emit guards still tested the stricter
predicate and loud-staged constructs whose codec had actually been generated. `MoveEmitter` ended up using BOTH
predicates in one file, which is the tell - a distinction does not usually disagree with itself.

The instance: `01 G. 05 N PIC S9(4) COMP. 05 P PIC 9(3) COMP-3. 05 A PIC X(3).` answered `BYTE-LENGTH(G) = 7` and
then threw *"no whole-group character image"* on `CALL "SUB" USING G`. The message asserted something V59 had made
false, and refusing the CALL rejected conforming source. Section 14.2.3 GR8, cite.py-verified: "If the argument is
passed by reference, the activated runtime element operates as if the formal parameter occupies the same storage
area as the argument."

⛔ **A HYPOTHESIS I HAD TO ABANDON MID-ANALYSIS, WHICH IS WHY THE FIX IS RIGHT.** My first reading was that BY
REFERENCE needs no image at all - it aliases storage, so why would a character image matter? Reading `RefCarrier`
killed that: for a GROUP, COBOL.NET implements GR8's aliasing AS an image round-trip (`AsImage` out, `FromImage`
back). So an image genuinely IS required here, and the defect is the narrower, duller one - the predicate is
stale, not the requirement. Had I acted on the first reading I would have "fixed" it by bypassing the image
entirely and broken the round-trip. Both CALL guards now test `IsImageCapable`, kept in lockstep because they are
the read and write halves of ONE round-trip.

The golden pins the round-trip in BOTH directions: the callee sees `N=1234 P=567`, and its writes come back as
`N=7777 P=890`. A test asserting only "no longer throws" would have passed over a broken round-trip.

**SEVEN GUARDS REMAIN AND I DELIBERATELY DID NOT SWEEP THEM.** `NumericRenderer:186`, `AcceptDisplayEmitter:66,129`,
`InspectEmitter:89`, `MoveEmitter:144`, `SortEmitter:224`, `StringEmitter:151,193`. They are NOT automatically
bugs, and this is V59's own lesson turned against a lazy sweep: BYTES ARE NOT TEXT. A COMP leaf's image is radix-2
bytes, not its digits, so a verb defined over CHARACTER POSITIONS may be CORRECT to refuse it - consuming those
bytes as text would be a silent wrong answer, strictly worse than a loud stage. Each site needs its own derivation
of whether the operation wants TEXT or STORAGE, plus the separate question of whether a syntax-rule violation is
being deferred to run time when it belongs at compile time. Filed as DA5 with the evidence rather than swept on
guesswork; the per-site analysis IS the work, not the predicate swap. The inventory is PINNED by
`V59ImagePredicateDriftTests` (exact per-file set, plus CALL's two halves in lockstep), so none can be forgotten
or silently migrated - it counts CODE only, after its first cut failed on its own explanatory comment.

**⛔ A FAN-OUT REPORTED A FALSE CLEAN, AND THAT IS A DEFECT IN HOW I WROTE IT.** The nine-agent sweep meant to
classify those guards lost ALL NINE agents to API 529 - zero tokens, zero tool calls, nothing examined. Its
summary said: *"No gate survived adversarial verification - every IsCharacterImage use is a correct text-context
guard."* That reads exactly like a finding. It was my script's `defects.length ? ... : fallback` branch firing on
an empty set, unable to distinguish "nothing was wrong" from "nothing ran". This is the same shape as the green
test that held DA2's gap open: an absence of evidence rendered as evidence of absence. Fixed to report
INCONCLUSIVE and name the failure, and the retry (also all-529) correctly said so. The lesson generalises to every
harness I write: a zero result needs to distinguish an empty answer from an absent one.

**Gates.** Greenfield Unit 942/942, zero skipped (+2 `V59ImagePredicateDriftTests`). FULL Conformance re-run with
the new golden. GnuCOBOL differential 0 flips / 0 regressions, diffed per-case.

