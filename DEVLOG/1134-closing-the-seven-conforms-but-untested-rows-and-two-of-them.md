## Entry 1134 - 2026-07-30 18:07 PDT - Closing the seven CONFORMS-but-untested rows, and two of them taught the test how to be written

`session-probe` had reported "7 CONFORMS still test-needed (each is one spec-derived golden from OK)" for a
while. That state is deliberate and useful — the rule was verified against the code, but only a SPEC-DERIVED test
closes a row, so the row stays a GAP. Seven rows, two goldens, GAP 3813 → 3806.

**Five were GENERAL FORMATS** (ACOS, CHAR, CONCAT, COS, DAY-OF-INTEGER). A CONFORMS verdict on an FMT row is a
claim that the grammar accepts EXACTLY the printed format, so the golden writes each call in the form the
standard prints and asserts the returned value from that function's own returned-value rule. Two of the five are
the §15.4.1 floating-math family, whose result is an implementor-defined APPROXIMATION, so those are pinned by an
inclusive range rather than by printing our own double back at ourselves — the PB7 convention. The exact three
are pinned exactly, and DAY-OF-INTEGER's expected 1601001 is DERIVED (§15.5.2 fixes the integer date form's epoch
at 1 January 1601; §15.24.4 r2 fixes the YYYYDDD shape), not sampled from the compiler.

**Two were ARGUMENT RULES, and both state an EQUIVALENCE rather than a value** — §15.19.3 r2 "ALPHANUMERIC and
ANUM are equivalent", §15.25.3 r3 "evaluated as though 50 were specified". So the test asserts the equivalence
directly: write it both ways, require identical results. That is stronger than pinning one answer, because it
fails if EITHER spelling drifts and it needs no knowledge of what the shared answer is.

**Three things the writing of those two corrected:**

- **My first CONVERT call was illegal and the compiler was right to reject it.** I wrote
  `CONVERT(SRC ALPHANUMERIC HEX)` reading `HEX` as the destination. §15.19.2's destination-format is TWO words —
  one of `{ALPHANUMERIC|ANUM|NAT|NATIONAL}` followed by the required `HEX` — or the single word `BYTE`. Rendering
  the figure rather than skimming the prose is what settled it. The long spellings do work in BOTH positions.
- **An equality assertion can pass VACUOUSLY.** `IF L1 = L2 DISPLAY "OK"` prints OK when both sides are empty or
  identically broken. Each line now prints the shared value too, so the golden pins the conversion result and not
  merely the agreement.
- **…but printing the value made the OTHER test clock-dependent.** §15.25.3 r5 defaults argument-3 to the CURRENT
  YEAR, so the YYYY half of DAY-TO-YYYYDDD's result is a clock reading that would drift the golden. The equality
  stays clock-independent (both sides read the same clock in the same run) and the DDD half — exactly what was
  supplied — is printed as the non-vacuity witness. Non-vacuous and reproducible are BOTH requirements, and the
  fix for one broke the other until the assertion was split.

**And the windowing rule needed more than one sample.** Argument-2 IS the cut, so the omitted-vs-explicit-50
comparison is probed below it, AT it, and above it (years 12/49/50/51/75). One sample on one side would pass
while the windowing was wrong — the PB5 lesson (its adjudicator sampled only small receivers, where the clamp
could not bite) applied to a date window instead of a quantizer.

