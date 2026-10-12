## Entry 1421 — 2026-08-31 16:47 PDT — The arithmetic wave's review fleet turns on the wave: r3 was written in two arms out of four, the loop-bound probe was being spent as a value, and the accuracy law published in four places named one driver of two

The D-B/D-C/PB194/PB195 working tree was green on its own gate and had never been read by anyone but its
implementer. A 106-agent review fleet read it — four lenses, each finding adversarially refuted by three
others — and returned 26 confirmed findings, 8 killed, 60 sweep notes. This entry is the landing of all of
them. Eleven became `kb/Work` notes (PB266–PB276) and every one of the eleven is fixed in this same commit.

The shape of the result is worth stating before the details, because it is the wave's real lesson: **five of
the eleven are defects the wave itself either introduced, widened, or CERTIFIED** — and the certification is
the worst of the three. `GR-8.8.1.5.4-3` was flipped PARTIAL → CONFORMS on a note whose *both* sentences were
false, pinned by five test-refs not one of which could reach the arm the rule is about.

### PB266 — §8.8.1.5.4 r3 was spelled in two of Pow's four arms

`cite.py --check 8.8.1.5.4 "When operand-2 is less than zero, the result shall be equivalent to the evaluation
of the arithmetic expression"` → OK; `--check 8.8.1.5.4 "(1 / (operand-1 ** FUNCTION ABS (operand-2)))"` → OK.

r3 is an **equivalence, not latitude** — r2's latitude is scoped *"When the value of operand-2 is greater than
zero"* — and under §8.8.1.5.2's per-operation 34-digit rounding the mandated expression is a DIVISION whose
divisor is the same development at |operand-2|. `exp(−|p| × ln b)` is a different value: one fewer
correctly-rounded division, one negated exp argument. That the standard means this at digit granularity is
settled inside the clause itself — r2d spells `((o1*o1)*(o1*o1))` rather than `(((o1*o1)*o1)*o1)`, a
distinction that exists only because each operation rounds.

`Pow` spelled r3 for a within-bound integer exponent and `PowCore` spelled it at exactly −½. The other two arms
— the general non-integer one and the past-loop-bound integer escape — carried the SIGN INSIDE the exponent.

I re-derived the divergence with my own 34-digit emulation of `CobolDec.cs`, written from the C# and validated
first against the shipped `pb167` goldens (R10, R2, Q1–Q4 and both parity rows reproduce digit for digit),
**before** running the probe:

```
                                              PREDICTED   BEFORE   AFTER
IF 2 ** -0.25     = 1 / (2 ** 0.25)              NE→EQ      NE      EQ
IF 7 ** -0.125    = 1 / (7 ** 0.125)             NE→EQ      NE      EQ
IF 2 ** -0.5      = 1 / (2 ** 0.5)               EQ→EQ      EQ      EQ
IF 1.0000001 ** -600001 = 1 / (… ** 600001)      NE→EQ      NE      EQ
IF 1.0000001 ** -500000 = 1 / (… ** 500000)      EQ→EQ      EQ      EQ
COMPUTE DLT = (2 ** -0.25 − 1/(2 ** 0.25)) * 10 ** 33   into 9V9(4)
                                              05000→00000  05000   00000
```

That last line is the part the finding itself understated and the refuters caught: the divergence was never
confined to a relation. An SDIDI subtraction of two near-equal values is EXACT, so an ordinary cancellation
lifts the 34th digit into a `DISPLAY`. 0.50 where 0.00 is owed, in a `PIC 9V9(4)`.

**The fix is structural, not local.** r3's reciprocal is hoisted to the top of `Pow`, over
`PowMagnitude(b, |operand-2|)`. Nothing below ever sees a negative exponent, and `PowCore`'s −½ twin is
DELETED — r3 reaches it through the same one division now. That is CLAUDE.md rule 5's shape: the arm added
next is r3-correct without anyone remembering to make it so.

**⚠ And honouring r3 costs accuracy.** The true 34-digit 2^−¼ is 0.8408964152537145430311254762332149. The old
direct form was 3 ulp out; r3's reciprocal form is 8. The rule constrains a RELATIONSHIP, not an error budget —
which is exactly why the fix had to be the literal expression and could never have been a tolerance, and why
the "≈31 correct digits" determination was no defence.

One visible consequence, pinned: an out-of-range NEGATIVE exponent now takes its EC name from r3's own
expression, because the operation that leaves the range is the INNER power. `2 ** -600000` reports
EC-SIZE-OVERFLOW and `0.5 ** -600000` EC-SIZE-UNDERFLOW; before the hoist they answered the other way round.

### PB267 — the loop-bound probe was being spent as a value

`TryIntegerValue` CLAMPS an out-of-`long` magnitude at `long.MaxValue`, and its own doc asserted the caller
*"rejects it before the MAGNITUDE is used"*. It does not — the loop bound only ROUTES it, and the escape then
built `PowCore`'s exponent from the clamp. So **every exponent past 2⁶³−1 was silently replaced by
9223372036854775807**.

```
                                                  BEFORE                   AFTER
(1+10⁻³³) ** 1.0E+20  into 9V9(13)        10000000000000           10000000000001
(1+10⁻³³) ** 1.0E+36  into 9V9(13)        10000000000000           SE EC-SIZE-TRUNCATION
0.9999999999999999 ** 1.0E+25             00000000000000 (silent)   SE EC-SIZE-UNDERFLOW
1.0000000000000001 ** 1.0E+25             SE EC-SIZE-TRUNCATION     SE EC-SIZE-OVERFLOW
```

Pre-existing — the deleted `PowByLogs` consumed the same clamped value — but D-C **certified** it by flipping
GR-8.8.1.5.4-2 to CONFORMS with this arm inside the claim.

The fix is that there was nothing to reconstruct: an SDIDI carries |operand-2| exactly, so `PowMagnitude` takes
the exponent OPERAND and the clamped number's only surviving consumer is the `> LoopBound` comparison.

**⛔ And the disposition the finding proposed would have been PB145's defect again.** "A clamped exponent with
|base| ≠ 1 is out of range" sounds right and is false: the closest SDIDI value to one is 1 ± 10⁻³³, and
(1 + 10⁻³³) ** 10²⁰ = 1.0000000000001 is comfortably representable. That screen is the spurious size error
PB145 spent a wave removing. `pb167`'s NEARU leg pins the value precisely so it cannot come back by accident.

With the true exponent flowing, a second problem opened that the clamp had been hiding: `Mul(p, ln b)` can
itself leave the decimal128 range (`1.0E-6176 ** 1.0E+6144` has |p·ln b| ≈ 1.4E+6148), and `Round34Wide`'s
clamp says EC-SIZE-OVERFLOW whichever way the RESULT is out of range. `PowCore` now screens the ORDER of the
product before forming it — `Order(p) + Order(ln b) > 6` ⇒ |q| > 43 000, past both of `Exponential`'s screens,
so out-of-range is certain and, p being a magnitude, the direction is the sign of ln b alone.

### PB269 — the accuracy law named one driver of two, and called the other one "inherent"

`LnReduced` reduced its argument by three 34-digit-rounded square roots to u = m^(1/8), then took
z = (u−1)/(u+1). For m = 1 + δ the roots put ~1.5×10⁻³⁴ of absolute error into u while u − 1 is only ≈ δ/8, so
`Sub(u, One)` keeps a relative error of ~10⁻³³/δ. At δ = 10⁻³³ — the closest an SDIDI value gets to one — the
first root rounds straight back to 1 and **ln m comes out exactly zero**.

Measured against 120-digit truth, ln m's correct significant digits:

| | three-root reduction | series over δ |
|---|---|---|
| worst over \|m−1\| ≤ ¼ | **17.05** | 32.74 |
| m = 1 + 10⁻³³ | **0.00** | 34.00 |

End to end: `(1 + 10⁻³³) ** 10³⁰` had 3.0 correct digits and answered a flat 1 against a true 1.0010005…;
`1.00001 ** 1000000` had 26.6; a 31-digit near-unit base ** 10¹⁴ had 18.5. After: 33.2, 32.4, 29.1.

The fix is ln(1 + δ) = 2·atanh(δ/(2+δ)) taken over δ = m − 1 directly inside |m − 1| ≤ ¼, where `Sub(m, One)`
is EXACT (both operands within a factor of two, so there is nothing to cancel). It is also CHEAPER — no square
root at all and ≤20 series terms against three BigInteger roots plus ~10 — so the band was chosen where the
direct arm is both more accurate and less work, **measured across 400 drawn bases rather than argued**.

The determination this falsifies was published in FOUR places (the `CobolDec.cs` r2e banner, CONFORMANCE.md §7,
`PB167.md`, the GR-8.8.1.5.4-2 inventory note), all saying "≈31 correct significant digits … degrading as
|operand-2 × ln operand-1| grows and for a base very near 1 — … inherent to the equivalent expression, not to
this coding of it". Three things wrong: the digits were understated, "for a base very near 1" hid a collapse to
ZERO rather than a degradation, and that driver was **not inherent — it was this coding of it**. All four now
publish the measured law: ≈33 digits (33.00–33.58 over the six dev rows, 30.87 the floor over a 400-case
randomized sweep), degrading as ≈ 33 − log₁₀|operand-2 × ln operand-1| — 32.15 at 7.1, 31.12 at 69, 30.11 at
693, 28.78 at 13 863 — and that driver *is* inherent, because an exp development turns absolute argument error
into relative result error. The near-unit driver is gone, not reduced.

### PB271 — a float receiver that could not notice an overflow

§14.9.25.4 **GR6 d) 4. a.** (not "GR4 step 4a", which five sites in this repo had inherited and which is the
elementary-move definition — all five corrected here): *"If the algebraic value of the sending operand is
farther from zero than is permitted by the usage specifications of the receiving data item, the
EC-DATA-OVERFLOW exception condition is set to exist, and the content of the receiving data item is
undefined."*

`MoveEmitter`'s float arm collapsed the sender to a double first. An SDIDI reaches ±9.999…E+6144, so the
magnitude was already ±Infinity when the checks ran: the single-precision arm's `double.IsFinite(src)` guard
was false, and the double arm was a bare cast whose stated premise — "a double receiver cannot overflow from a
finite double" — is true and assumes the thing that stopped being true. `MOVE 1.0E+400 TO` a FLOAT-BINARY-64
stored +Infinity and raised nothing, at every checking level. D-B made that literal legal in the DEFAULT
arithmetic mode, which is what put a decimal128 sending value in front of a binary64 receiver in ordinary
programs.

The screen is now on the `CobolDec` itself, before the conversion, and covers both precisions. Scope: GR6 d)4.a
binds "a standard floating-point usage" (FLOAT-BINARY-32/-64); for COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED
§14.6.8.3 r1 delegates it, and COBOL.NET's determination is the same condition — one rule for every float
receiver, which the single-precision arm has already followed since D21. **The default build is unchanged**:
checking is off by default (§14.6.13.1.1) and GR6 d)4.a makes the content undefined, so ±Infinity remains what
it stores. Both halves are pinned now — `ec_data_overflow` for checking ON, the new
`pb271_float_receiver_overflow` for the half a default build actually sees.

### PB272 — the exact `**` arm was keyed on spelling

`Power`'s exact-integer arm asked `e.Scale == 0 && !e.Dec && !e.Real` — three properties of how the operand was
WRITTEN. So `10 ** 30` took the exact Int128 power and `10 ** 30.0` and `10 ** 3.0E+1` both fell to the
§8.8.1.2 binary64 approximation:

```
COMPUTE R = 10 ** 30       1000000000000000000000000000000
COMPUTE R = 10 ** 3.0E+1   1000000000000000071935427891953   →  …000000000000
COMPUTE R = 10 ** 30.0     1000000000000000071935427891953   →  …000000000000
```

Pre-existing and independent of D-B (`10 ** 30.0` fails on `e.Scale == 0` with no float literal anywhere); what
the wave added was the CLAIM that made it worth fixing rather than noting. §8.3.3.3.3 rule 5 makes a
floating-point literal's value *"the algebraic product of the value of its significand and the quantity derived
by raising ten to the power of the exponent"* and nothing more, so trailing zeros are notation and 30, 30.0 and
3.0E+1 are ONE operand. The arm is keyed on the literal's value now.

### PB274 — an invariant that was a determination

"A literal's NOTATION never changes the arithmetic, only its VALUE does" was written in four places as an
invariant. It is false:

```
01 A18 PIC 9(18) VALUE 999999999999999999.        (A18 * B18 = 36 significant digits)
IF A18 * B18 + 0.0E+0 = A18 * B18   →  NE          the decimal128 lane rounds to 34
IF A18 * B18 + 0.0    = A18 * B18   →  EQ          the Int128 lane carries all 36
```

Disposition: **document, do not "fix"**. §8.8.1.3 leaves *"the method of evaluating an arithmetic statement"* to
the implementor, so both lanes conform. All four copies now say the true thing — the VALUE a literal contributes
is its exact §8.3.3.3.3 rule-5 value in every position and mode; the LANE it selects under native arithmetic is
a published determination, including its consequence that INTERMEDIATE ROUNDING reaches a native statement
carrying a float literal. `pb156`'s new LANE/LANEF legs make it observable in the corpus for the first time.

### The rest, briefly

* **PB268** — ln 10 is a pure function of the rounding mode and was re-derived per exponentiation; the general
  arm paid for SIX BigInteger square roots where PB167's Cost paragraph claimed three. Cached per mode, with
  **PROHIBITED deliberately uncached**: §11.9.11.2 r3d requires that development to RAISE every time, and the
  raise IS the observable.
* **PB270** — the r2e drift guard's comment claimed a five-substring scan was EQUIVALENT to call-graph
  reachability. Restated to what it proves; banned list widened to the near-miss spellings (`Math.Log2(`,
  `double.Pow(`, …); the one premise that can be mechanical — `CobolDec` is not `partial` — is now asserted.
  ⛔ `ToDouble`/`FromDouble` deliberately NOT banned, against the original finding and with its refuters: they
  are the type's own §8.8.1.5.1 conversion API.
* **PB273** — `CombineCore` said the only native Dec producer is an integer power. There are three, and D-B made
  the third (a float literal operand) much the most common.
* **PB275** — `IntermediateExponentRange`'s summary stated a floor(log₁₀) rule its own SDIDI rows do not obey.
  The ROWS are right; no single floor/ceil sentence describes all four, because the column is read under two
  caller conventions. Both are now written down, with the consequence: a true decimal128 carrier row in the
  ITEM arm must be 6144, not 6145.
* **PB276** — `pb99-floating-literal-range` carried both lanes in one file, and the negative harness asserts
  only that SOME diagnostic contains the code, so the four VALUE-clause entries satisfied it alone: deleting
  `CheckLiteral`'s statement screen outright would have left the negative GREEN. Split into two fixtures.

### The register's own gate did its job, and it cost a row

The four status flips (PB156, PB167, PB194, PB195 → `landed`) turned the wave-local gate RED on exactly one
test: `DefectiveRowCoverageDriftTests.EveryDefectiveRow_IsClaimedByALiveWorkNote`. `PB167.md` carries
`inventory_rows: ["GR-8.8.1.5.4-2", "GR-8.8.1.5.4-3", "RV-15.4.1-3"]`, and the third of those was still
PARTIAL/GAP — so flipping the note orphaned it. This is `DESIGN`'s intent working: **when a note flips to
`landed`, its rows must be re-verdicted in the same change set**, and the gate is the thing that will not let
you forget.

Reading `RV-15.4.1-3`'s refutation was the point. It said, verbatim: *"CobolDec.cs#Pow, non-integer-exponent
branch (line 139): `return FromDouble(Math.Pow(b.ToDouble(), e.ToDouble()));` — both operands leave SDIDI form
for binary64 before the development begins."* That line is precisely what owner decision D-C deleted. The row
had been PARTIAL for a reason that stopped existing, and nothing but this gate would have said so.

It also carried an objection worth honouring rather than arguing with: *"No leg asserts
`FUNCTION EXP10(0.5) = 10 ** 0.5`."* True — pb167 pinned the function against SQRT and the operator against
SQRT, transitively but not directly. It asserts the identity itself now (the EAE10 leg). Only then was the row
re-verdicted PARTIAL → CONFORMS, **through `record_verdicts.py` and not by hand** — dry-run first, as the tool's
own doctrine requires: `rows changed 1`, `RV-15.4.1-3: PARTIAL → CONFORMS`, **GAP 3637 → 3636**.

### Two things this landing did on purpose

**Every numeric expectation was derived before the run that confirmed it.** The emulator was written from the
C# source, validated against the shipped goldens first, and then used to predict each probe leg and each new
pinned digit. Every prediction matched. No golden digit moved as a result of PB266 or PB269 — I checked all six
arithmetic goldens byte-for-byte — because no corpus case exercised a general negative exponent and the
near-unit fix moves digits below `pb167`'s 15-fraction-digit truncation. The values that moved are the ones the
new legs pin.

**Each new assertion was proved able to fail, then reverted.** Un-hoisting r3 turned six theory rows red;
disabling `IntegerValuedLiteral` printed 1000000000000000071935427891953 into `pb156`'s IPOW legs. A pin that
has never failed is a decoration.

### Friction, logged honestly

Two things cost time and are worth writing down. **The doc sweep after the first green gate found two more
copies of sentences I had already corrected elsewhere** — `IntrinsicRenderer`'s own "the only native Dec
producer is an integer power" and `PB156.md`'s copy of the notation invariant. Both were found by grepping for
the corrected phrases rather than by remembering where they lived, which is the only method that works; the
first pass had corrected the sites the FINDINGS named and stopped there. `feedback_scan_all_similar` says sweep
the pattern, and the pattern here is a SENTENCE, not a code shape.

And a self-inflicted one: a Python one-liner passed to `bash -c` had backticks inside its string literal, so
the shell substituted `NumericRenderer.CombineCore` and `IntrinsicRenderer.RenderNum` as COMMANDS and wrote the
note with two empty holes where the identifiers should be. It printed "command not found" twice and then
"note updated" — a success line immediately after its own failure. Caught by reading the file back, which is
the only reason it is not in this commit. The standing rule (`heredoc_backslash_mangling`) says write patch
scripts with the Write tool; it applies to `python -c` with a shell-quoted body just as much.

### Deviation from the disposition contract, stated

The contract's A2 disposition read "detect saturation and dispose by magnitude analysis (|b|>1 → overflow;
|b|<1 → underflow)". I did not implement that, because it is measurably wrong: (1 + 10⁻³³) ** 10²⁰ has a
perfectly representable value, and screening by the base's side of 1 would raise a spurious size error on it —
PB145's defect, restored. What landed instead computes the right answer where one exists and names the right
condition where none does, from the same §8.8.1.5.2 range. Likewise A13's "optionally add the conversion-bridge
strings": the refuters explicitly refused `ToDouble`/`FromDouble` as ban candidates, and being binding, they win.

