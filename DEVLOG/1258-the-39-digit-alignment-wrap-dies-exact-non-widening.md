## Entry 1258 — 2026-08-09 11:22 PDT — The 39-digit alignment wrap dies: exact non-widening comparison, selection-by-index, and the escape-checked Align — five more rows close

PB65's highest-harm member is out. Aligning two operands to their common scale widened with an
UNCHECKED Int128 multiply, so at (max integer digits) + (max fraction digits) > 38 the operand
wrapped silently: IF BIGV > SMLV over a PIC 9(24) and a PIC 9V9(15) — two legal, in-range,
positive items — answered FALSE, and FUNCTION MIN of the pair returned a NEGATIVE value, the
content of NO argument (§15.63.4 r1 violated on its face). The triage prober found it by reading
the implementation; the probe pinned the threshold at 39 aligned digits.

THE FIX, three pieces at their single sites: (1) CobolNum.Compare — the Int128-lane exact
non-widening comparison (sign split, then the overflow-means-greater magnitude trick CompareU
always used), emitted for every differing-scale numeric relation; (2) Align's widening arm is
escape-CHECKED (RescaleEscape → the size-error condition at the D1 escape boundary, never a wrap) —
value-semantics consumers only, the arithmetic store path keeps its documented item-179 wrap;
(3) MAX/MIN/ORD-MAX/ORD-MIN are SELECTION-BY-INDEX over unaligned (value, scale) pairs
(CobolIntrinsics.Select.cs): each argument compares at its own scale, only the selected value
rescales — with store semantics into a known receiver (COMPUTE RB = MAX(BIGV SMLV) returns BIGV
exactly, previously impossible), LOUD receiverless (a capped value inside a comparison would
silently compare the wrong number — PB13's receiver-blind-scale lesson applied in both
directions; my own first cut aimed selection at the common scale and the probe caught it raising
on a defined MOVE, the same lesson pointed at me).

The full gates earned their run: characterization went red twice, both attributable — the emitted-
C# snapshot pinned the old MaxScaled emit (re-baked, one line; thirteen EOL-only rewrites
restored), and the bare-runtime-access guard caught my hardcoded CobolNum.Compare emit strings
bypassing the RuntimeApi nameof discipline — fixed by routing, not by bumping the baseline. FULL
Conformance 4,309/4,309 · Unit 4,122/4,122 · characterization 33/33 · SpecTraceability 10/10 (it
also caught one invented symbol in my own re-verdict batch — the gate works on me too).

Documented residue on the note: a 39-digit-escape selection consumed through a receiverless MOVE
raises EC-SIZE rather than answering (MOVE renders sources under ReceiverContext.None by design;
threading the receiver there is its own measured change) — loud, never wrong. FIVE rows re-verdict
CONFORMS+tested (the Rescale trio + the two value-vs-index selection rows). GAP 4,208 → 4,203;
the defect backlog stands at 201 bad-verdict rows.

