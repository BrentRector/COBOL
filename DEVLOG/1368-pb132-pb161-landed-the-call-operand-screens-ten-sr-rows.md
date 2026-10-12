## Entry 1368 — 2026-08-29 03:56 PDT — PB132 + PB161 LANDED: the CALL operand screens — ten SR rows through ONE chokepoint over the RESOLVED mode, and the bit-run image codec's OCCURS gap the new golden flushed out

PB132 was the two-arm-dispatch note made systematic: every predicate already existed in the tree and CALL
reached none of them. ScreenCallOperand is now the ONE chokepoint — run for every Place-carrying USING
argument and the RETURNING item with the RESOLVED mode (after GR5's transitivity or GR9's formal
derivation), so both arms of every dispatch meet the same law. Landed: SR1's target-category screen
(COBOLNET1681 — a numeric target used to fall through to a garbage name-string read at RUN time), SR3's
object-data ban (OoIsObjectData had two consumers, both INVOKE — 1678), SR10's three BY REFERENCE kinds
(strongly-typed / object / pointer — 1679), SR12's variable-length group (1680, through THE one predicate:
IntrinsicBinder's PB124-era copy now delegates to ReferenceResolver.HasVariableLengthSubordinate), SR13's
context rule (1682 — host.InMethod / host.UdfSelfName, one predicate, function and method both pinned),
SR7's screen-name RETURNING at BIND time (1677, ending the PB88 wrong-stage posture on that arm), SR11/SR18's
ANY LENGTH citations switched per format (the old loop cited Format 1's rule for every argument), and SR22
run on BOTH arms of its own disjunction (ValueClassScreen — the explicit phrase now classifies an index-name
as class index instead of letting BoundIndexRef slip the BoundNumRef guard, and the implied side reads the
corresponding formal through PB131's callee table).

SR6/SR8 — the bit-alignment rules — got the machinery the batch-8 note predicted they needed:
BitLayout.StartBitWithin rides the SAME §8.5.1.6.3 cursor walk as ExtentBits (one walk, one law), and
ScreenBitAlignment proves the referenced occurrence's start bit statically — constant subscripts times
element stride plus a ref-mod's leftmost — rejecting a non-literal subscript as the rules' second clause
demands and accepting-unproven only the exotic carriers it cannot model (a screen must never reject what it
cannot prove misaligned; the REDEFINES-view corner keeps the two rows PARTIAL).

PB161, found by the new positive golden: a `PIC 1(8) USAGE BIT OCCURS 3` member made the generated record
fail BACKEND compilation (CS1503/CS0029) — the bit-run image codec's AsImage packer and FromImage
distributor each lacked the OCCURS handling their EmitBitMethods siblings had. The SIXTH instance of the
two-arm-dispatch shape, fixed the same way as the other five: BitCarrierOf is the one carrier law,
EmitRunMemberFromBits the one distributor, both faces ride them. The golden proves the whole path
end-to-end: B8=01011110 / BT2=01011110, the callee's store through the reference formal visible in the
plain AND the subscripted element.

Verdicts: SR1, SR7, SR10, SR12, SR13, SR18, SR22 → CONFORMS; SR3, SR6, SR8 → PARTIAL (the ADDRESS OF
grammar residue and the REDEFINES-view bit corner, both named). Sixteen fixtures: fifteen negatives — one
per screen arm — and two positives (the aligned-bit crossing, the dynamic target whose only prior witness
was a non-qualifying legacy-differential test).

