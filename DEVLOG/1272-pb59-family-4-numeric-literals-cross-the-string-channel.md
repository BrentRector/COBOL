## Entry 1272 — 2026-08-09 17:45 PDT — PB59 family 4: numeric literals cross the string channel where the rules admit them — per-function, never blanket

`FUNCTION BASECONVERT(255, 10, 16)` and `FUNCTION CONCAT(A 12)` compiled clean and aborted at run time —
the wrong-stage family on legal source: §15.12.3 r1 admits "an unsigned integer … literal" below base 11
and §15.18.3 r1 lists class NUMERIC, but the renderer's string channel had ONE function-blind visitor
whose BoundNumericLiteral arm was Loud, and PB49's drift test PINNED it as designed.

The fix is the shape the map prescribed: ONE visitor class, a second cached admitting instance (a second
CLASS would silently join the drift test's end-of-file body slice), the literal rendering through the ONE
OperandText.AsString image (raw source text — correct for §15.18.4 r1; r3's value conditions stay named
on the DeliberatelyUnscreened rows, not half-enforced in a renderer). StrArgList takes the admission as a
parameter — four callers, three different §15.x.3 rules — and ONLY Concat passes true; BaseConvert's
argument-1 rides the new StrNum entry. The DEFAULT stays loud on purpose: NUMVAL/NUMVAL-F carry open rows
DEMANDING a compile-time rejection, and widening the default would trade their wrong-stage defect for a
silently-wrong one.

The pin moved WITH the fix: the rewritten drift test asserts the arm is CONDITIONAL (admitNumeric ? the
OperandText image : Loud) and a NEW caller-set guard pins the admitting entry's reachers to exactly
[BaseConvert, Concat] — proven in the failing direction first, and instructively: its first draft's
forward scan used [^;]*? and the arms' own COMMENTS contain ';', so it matched NOTHING — only the
asserted expected list kept an empty result from reading as green. The guard now attributes each
call-site token backward to its enclosing arm label, and the diagnosis is written into it.

Golden pb59_numeric_literal_argument (numeric literal + the FOLDED nested numeric function —
IntrinsicBinder.OperandOf turns FUNCTION LENGTH of a fixed item into the same leaf — both producers).
Differential rows: the unquoted-argument-1 BASECONVERT row, the CONCAT numeric/folded test.
RV-15.12.4-1 → CONFORMS · RV-15.18.4-1 → CONFORMS · AR-15.12.3-1 leg (a) closed (rows (b)–(f) stay
DIVERGES under family 5's screens). GAP 4171 → 4169. Gates: Conformance
~Concat|~BaseConvert|~Intrinsic|~Corpus 697/697 · the golden 1/1 with its .out · the drift family 17/17 ·
SpecTraceabilityInventory 10/10 · work.py 126 well-formed.

