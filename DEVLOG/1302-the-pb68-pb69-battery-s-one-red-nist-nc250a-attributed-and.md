## Entry 1302 — 2026-08-18 02:50 PDT — The PB68+PB69 battery's one red (NIST NC250A), attributed and closed: PB84 — one landing and one store for every consumer of a rendered intermediate — plus PB85, and PB86 registered

**Battery first (PB68+PB69 batch, tree `5c429e98`):** Conformance **4632/4633** · Unit **4192/4192** ·
Characterization 33/33 · NIST-legacy 353 MATCH / 0 · GnuCOBOL differential 1323 cases, **0 per-case flips**. The ONE
red: `NistDifferentialTests.NistProgram_MatchesGolden(NC250A)` — the whole program failed at the backend with five
`error CS0019: Operator '>' cannot be applied to operands of type 'CobolDec' and 'int'`, every one a sign condition
over an integer power: `IF 9 ** TWO + (180 - 90) IS NOT POSITIVE`, `IF 10 ** THREE + 99 - (1500 - 400) IS NEGATIVE`.
(The guard's "NIST: 353 MATCH" is the LEGACY compiler's leg — the two subjects differ; COBOL.NET's NIST verdict is the
Conformance leg's.)

**PB84 — the mechanism, and why it is a class.** PB69 (1301) made every native integer power an SDIDI (`Dec`)
intermediate. The relation renderer, the arithmetic store and the MOVE store each already had a `Dec` arm; FIVE
other consumers of a rendered `NumX` read it as the native Int128 carrier and had none — the sign condition
(`{v.Expr} > 0`), SET pointer UP/DOWN BY and ALLOCATE … CHARACTERS (`(long)({x.Expr})`), CALL … BY VALUE
arithmetic-expression (the same cast), and INVOKE … BY CONTENT arithmetic-expression (`CobolNum.Store(ex.Expr,
ex.Scale, profile)`). **And every one of them was ALREADY a Roslyn error for every arithmetic expression under
`ARITHMETIC IS STANDARD-DECIMAL`** — `IF A * B - 60 IS ZERO`, `SET P UP BY A + 1`, `CALL … BY VALUE A / 8`: 14 backend
errors on the twin golden, measured by stashing the fix and rebuilding. PB69 did not create the defect; it made the
native mode reach it. The two-arm-dispatch shape (feedback_two_arm_dispatch — now 6×), on the carrier axis.

- **One landing.** `NumericRenderer.Landed(x, rcv)` — the intrinsic-argument landing moved out of
  `IntrinsicRenderer` (which now delegates) — is THE landing of an intermediate into the Int128 lane: a `Dec` lands
  CHECKED at `rcv.WorkingScale(NumvalScaleFloor)` (EC-SIZE-OVERFLOW past the carrier, never modular digits — the
  PB69 discipline), a float under a standard mode converts to SDIDI first (§8.8.1.5.1), a native passes through. The
  CALL BY VALUE, SET UP/DOWN BY and ALLOCATE emitters funnel through it — so `CALL … BY VALUE 2 ** -2 + 1` passes
  1.25 (the fraction survives at the working scale) and `ALLOCATE A ** 2 CHARACTERS` allocates 144.
- **One store.** `NumericRenderer.StoreArgs`/`StoreExpr` are THE carrier switch at a fixed-point store — the
  arithmetic store's `args`, the numeric MOVE and BOTH INVOKE BY CONTENT arms spell it once; `RuntimeApi.NumStoreDec`
  is deleted as dead (the four inventory rows that named it as a code-location now name `StoreExpr`). The MOVE store
  now spells its default mode (`CobolNum.Store(v, 0, _P, CobolRounding.Truncation)`) exactly as the arithmetic store
  always did — six emitted-C# snapshots re-baselined, the diff being that one argument nine times.
- **The sign condition** gained its arm: `RuntimeApi.DecSign` = `Int128.Sign(dec.Sig)` — exact at every exponent
  and never a landing that could overflow (a 45-digit power's sign is its significand's).

**PB85 — found by the sweep, fixed in the same commit.** `DIVIDE F BY 2 GIVING Q REMAINDER R` with F FLOAT-LONG was
`CS0266: Cannot implicitly convert type 'double' to 'System.Int128'`: `EmitDivideRemainder` snapshots both senders
into `Int128` locals (total over Dec/Int128 only) and calls the exact kernel directly, bypassing `Combine`'s float
lane. §14.9.12.3 SR1 admits any category-numeric elementary item and GR7 defines the remainder. Now
`NumericRenderer.FixedLane` — a native float lands truncated at the quotient receiver's `FloatWorkingScale`, an
unsigned-wide read through `Widen`, everything else `Landed` — and the snapshot is total over the four carriers.
7.5 / 2 → Q 3 (GR6c truncates the subsidiary quotient at the receiver's scale), R 1.50 (GR7: 7.5 − 3 × 2); 17 / 7.5
→ Q 2, R 2.00.

**PB86 — found by the same probe, registered, NOT fixed here.** `PERFORM COUNT-IT INTEGER(3.7) TIMES` (the
keyword-omitted intrinsic under `FUNCTION ALL INTRINSIC`) ran the body ONCE: `ControlFlowEmitter.CountExpr` has arms
for a literal, a field and an error operand and a `_ => "1"` default that swallows the computed operand. The
`FUNCTION` spelling is a parse error (`performTimes` has no `functionCall` arm), and a non-integer identifier count
(`X PIC 9V9 VALUE 1.2`) is accepted against §14.9.28.3 SR2 and iterates its UNSCALED digits (12 times). The note
carries the design (grammar arm; SR2 at bind for both shapes; `Align(…, 0)` for the computed count; the swallow
made loud). Next.

**Measured** (goldens `pb84_sdidi_intermediate_consumers` — native — and `pb84_standard_decimal_intermediate_consumers`
— the twin, both with a nested subprogram and a class): seven sign shapes over powers (POSITIVE / NEGATIVE / ZERO /
NOT ZERO false / 9⁵ − 59049 ZERO / 0 − 9999³ NEGATIVE / a compound), the pointer round-trip (`SET P UP BY A ** 2`
then DOWN — back to P2), ALLOCATE 144, CALL BY VALUE 0144.0000 and 0001.2500, INVOKE 000144 and 1.25, DIVIDE
REMAINDER 3 r 1.50 / 2 r 2.00 / 2 r 2.00 (the integer control); the twin: 12×5−60 ZERO, 12/5 POSITIVE, 5−12 NEGATIVE,
12/8 − 1.5 ZERO exactly on the SDIDI, CALL BY VALUE 1.5 and 61, INVOKE 61 and 1.50, DIVIDE REMAINDER 3 r 1.50.

**Gates (wave-local).** Solution build · Characterization 33/33 (after the snapshot re-baseline) · Conformance
`Corpus|Negative|Nist|Arith|Numeric|Sign|Divide|Pointer|Call|Invoke|SpecTraceability|Standard|Float|Oo`
**1502/1502** (NC250A green) · Unit `SpecTraceabilityInventory|Numeric|Arith|Emit|RuntimeApi|Manifest|Snapshot|Guard|Registry`
**341/341** (after repointing the four `NumStoreDec` code-locations) · CLI probes. Docs: COBOLNET_NUMERIC_DESIGN.md
D19 (the ⛔ one-landing/one-store paragraph), kb/Work PB84 + PB85 (landed) + PB86 (open), plan §0 (the battery line,
NEXT = PB86 → PB70 → …).


