## Entry 1343 — 2026-08-28 23:18 PDT — Phase-B batch 7: the §15 CLOSE-OUT — 137 rules adjudicated (72 CONFORMS · 51 PARTIAL · 8 DIVERGES · 3 NOT-IMPLEMENTED · 3 NEEDS-OWNER-DECISION), GAP 3823 → 3794, the ENTIRE intrinsics clause is now adjudicated; eleven new kb/Work notes (PB115–PB125), eight of them the refute stage's finds

The first Phase-B batch since 2026-08-04, run as a 40-agent workflow (one adjudicator + one INDEPENDENT
refuter per subject, Opus 5 per the standing order; 20 subjects from `phase_b_batch.py 15.2-15.4 15.10
15.83-15.100`; 7.1M subagent tokens, 2,490 tool uses, ~35 min; 0 agent errors). The refute stage overturned
**73 verdicts** — every playbook prediction held: overturns were overwhelmingly DOWNGRADES, and the two
sharpest finds came from re-reading code the adjudicator had sampled around. Merged with
`record_verdicts.py --dry-run` first (the GAP moved DOWN — the anti-glob tell), by name, never the glob;
the SpecTraceabilityInventory gate is green.

THE FINDS, clustered into kb/Work notes before these paragraphs (rule 8):
- **PB115** (wrong answer) — `NumericRenderer.Pow10D` builds 10^scale by repeated double multiplication,
  exact only through 1e22: a conforming scale-23+ argument shifts one ulp and a LEGAL `ASIN(|x| ≤ 1)`
  evaluates NaN → the §15.3 default 0, or a thrown EC-ARGUMENT-FUNCTION under checking. `CobolDec.ToDouble`
  (`Sig * Math.Pow(10, Exp)`) overshoots at a DIFFERENT scale — the two lanes fail independently, so each
  lane's green was no evidence about the other.
- **PB116** (wrong answer) — SQRT is the ONE §15 function whose standard-mode value the standard fixes
  EXACTLY (§15.84.4 r2, 34 digits), and it gets Math.Sqrt's ~16 through FromDouble; r1's "argument-1 is not
  rounded" is broken by the binary64 funnel.
- **PB117** (wrong answer) — TRIM's several argument-2s trim the SET UNION where r5 requires SEQUENTIAL
  folds; the standard's own NOTE example agrees by accident, so a NOTE-derived golden would pass over it.
- **PB118** — SUBSTITUTE enforces r2's class agreement only on the FIRST pair (the variadic tail is outside
  MatchedPositions).
- **PB119** (wrong answer) — YEAR-TO-YYYY's float arm passes an omitted argument-3 as an explicit 0 — the
  TWO-ARM DISPATCH shape's sixth occurrence.
- **PB120** — WHEN-COMPILED's stamp is process-static (unit N inherits unit 1's time) and the PE's COFF
  stamp is nondeterministic with nothing tying it to the returned value.
- **PB121** — TEST-NUMVAL-F has no SDIDI capacity leg: "1E+9999" reports CONFORMING under standard-decimal.
- **PB122** — SMALLEST-/LARGEST-ALGEBRAIC's IN-ARITHMETIC-RANGE content has no implementation (vacuous only
  while every float argument is refused — and under the standard modes that refusal is itself over-rejection).
- **PB123** — EXCEPTION-FILE resolves against `Files` where every sibling uses `FilesByName`: a contained
  program's legal GLOBAL-FD reference is rejected — two-arm dispatch, seventh occurrence.
- **PB124** (analysis) — the §15.3 kind-screens' finer-axes cluster (a dozen AR-15.3-* PARTIALs: no
  class-alphabetic member; boolArgAhead's missing space boundary; Index → Numeric result-category fold;
  Table 2's third pointer category absent; TRIM/LENGTH/SUBSTITUTE keyword-order laxity).
- **PB125** — FACTORIAL(34) under STANDARD-DECIMAL returns 0 where the SDIDI holds the EAE's 34-digit value
  — the PB13 saturation class surviving in the one function PB13 routed around the float lane.
Three NEEDS-OWNER-DECISION rows joined the standing standard-binary / CURRENT-DATE questions (no new notes).
Seven CONFORMS rows are test-needed (recorded by the merge). `work.py next` re-ranks: PB115–PB117 lead.

