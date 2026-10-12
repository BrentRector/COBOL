## Entry 1115 - 2026-07-29 19:40 PDT - The first Phase-B batch: 55 rules, 12 GAPs closed, and the moment I nearly closed 7 more on evidence the standard forbids

First real use of the mechanism from 1114. **§15.7 + §15.70–15.79 — 55 normative rules over 11 intrinsic
functions (24 AR · 20 RV · 11 FMT)**, fanned out one agent per function, each with its OWN input file of rule
text, each verdict then handed to an independent agent told to OVERTURN it and to default to overturning when
uncertain.

**RESULT: 3790 → 3778 GAP.** 23 CONFORMS (12 closed, 11 CONFORMS-but-untested) · 18 DIVERGES · 13 PARTIAL ·
1 NEEDS-OWNER-DECISION. The gate passes on all 55: every `code-location` symbol resolves, every `test-ref` names
a test really on disk, every `state` equals the derived one.

**⛔ THE FINDING: 18 DIVERGES IS NOT 18 BUGS. IT IS ONE.**

`IntrinsicCatalog.cs` gives each of its **79** rows an `ArgKinds` string declaring every argument's required class,
and exposes `IntrinsicSig.ArgKind(int)` to read it. **`ArgKind` has zero callers.** The only read of `ArgKinds`
anywhere in `src/` is the `sig.ArgKinds == "p"` MAX/MIN polymorphism test. The table that exists precisely to
enforce §15's argument rules enforces nothing; class checking survives only as hand-written arms for a handful of
functions. One rule, written in a few places and declared-but-unread in all the rest — the DA3/DA5/DA6 shape
again, except this time the general mechanism was already built and simply never wired in.

I did not take that from the agents. **Reproduced by hand at the CLI, both directions:**
`MOVE FUNCTION REVERSE(N) TO R` with `N PIC 9(4) VALUE 1234` compiles clean and prints `4321` (§15.78.3 r1 allows
alphabetic/alphanumeric/national only); `COMPUTE R = FUNCTION ABS(A)` with `A PIC X(4) VALUE "ABCD"` compiles
clean and prints `0000000{` (§15.7.3 r1 requires class numeric). No diagnostic, at any edition.

⚠ **And it is a hole in DA6, which landed hours earlier the same day.** DA6 installed the §8.8.1.1 reject for an
alphanumeric ARITHMETIC operand. `COMPUTE R = A` is correctly rejected; `COMPUTE R = FUNCTION ABS(A)` is not,
because `BindFunctionArgumentExpr` deliberately suppresses that screen for function arguments. Filed as **PB1**,
the first item the traceability review has fed into the fix queue — which is the design working as intended.
Scope: §15 holds 216 AR rules over 43 functions, 47 of them explicit class constraints; 11 functions adjudicated
so far.

**⛔ AND THE THING I ALMOST GOT WRONG, WHICH IS THE REAL LESSON OF THE BATCH.**

The batch came back with 19 CONFORMS rows carrying a covering test. I was about to merge. Then I read the
test-refs: `nist:IF128A`, `nist:IF129A`, `IntrinsicFunctionDifferentialTests.ExactFamily_MatchesLegacy`,
`ChannelMatrix_IfEvaluateMove_MatchesLegacy`.

**Seven of those nineteen rows would have closed on a NIST golden or a `*_MatchesLegacy` differential** — exactly
the artifacts CLAUDE.md rule 1 names as "regression NETS with known holes, never authority", and exactly what
`DESIGN-spec-conformance-review.md` §1(c) rules out: the expected value must be COMPUTED FROM THE SPEC, never
copied from an implementation oracle. A differential is structurally blind to a violation both implementations
share, and many of this compiler's defects were ported FROM the legacy — so agreeing with it is evidence of
nothing. Closing rows that way would have moved the v1.0 burn-down on precisely the evidence the mission forbids.

**It was my fault, in the prompt and in the schema.** I told agents which test-ref forms RESOLVE and never that a
cover must be spec-derived, and the schema listed `nist:` as a legal form with nothing marking it differential.
The agents did what I asked.

Fixed structurally, not row by row:
· Every `test-ref` form now carries **`spec-derived`** — `conformance`/`unit`/`conformance-test` true;
  `nist` and `characterization` false, each with a `why-not` saying so. A non-qualifying ref is still worth
  RECORDING (knowing NIST exercises a rule is useful); it just cannot be what closes the row.
· An xUnit form CAN be spec-derived, so the form alone cannot decide it — but this repo names the two kinds apart
  by convention, so **`disqualifying-method-patterns`** (`MatchesLegacy$`, `MatchesGnuCobol$`, `MatchesOracle$`)
  strikes a test whose own name says its expected value came from an oracle.
· Both evaluators enforce it, and `EveryResolvedRow_CitesASpecDerivedTest` states the rule independently of the
  derivation.

**⛔ WHICH EXPOSED A CONFLATION IN MY OWN SCHEMA, and this is the better half of the fix.** `requires` was doing
two jobs: the evidence a RECORD needs to be well-formed, and the evidence a ROW needs to CLOSE. Splitting them
makes **CONFORMS-but-untested** expressible — and the design doc §3 Phase C names that category outright
("CONFORMS-but-untested → write the spec-derived golden"). So `CONFORMS` now requires only `code-location` to be
recorded, and closes only once a spec-derived test covers it. The 7 differential-only rows land honestly: verdict
CONFORMS, NIST golden recorded as corroboration, row still open. Had I folded the test into `requires`, each
would have had to be misrecorded as PARTIAL or closed on the differential.

**⚠ A THIRD INSTRUMENT WAS LYING, IN THE SAME DIRECTION.** `session-probe` reported "4 CONFORMS still
test-needed" against the writer's 11, because I had keyed it on an EMPTY test-ref rather than on the row not
closing — so the 7 rows citing a differential looked covered. Three places measured "is this tested" and one of
them measured something else. Now keyed on `state -eq 'GAP'`, and probe and writer agree at 11.

**PROCESS NOTES.** The writer's all-or-nothing validation earned itself immediately: two records carried a
`verified-by` field an agent invented, and the whole 55-record batch was refused rather than half-applied. The
field's content was good evidence, so it was folded into `notes` rather than dropped. And the writer's own report
was overstating — it labelled all 23 CONFORMS "(closes the GAP)" when 12 did; it now reports per-verdict what
ACTUALLY closed, because printing the nominal number as if it were the real one is how a burn-down gets
overstated by the tool meant to measure it.

**GATES.** Solution build clean. `SpecTraceabilityInventoryDriftTests` 10/10 over the 55 adjudicated rows,
`TestRepoDriftTests` 2/2. Unit full suite green at 1114's commit; the only code added since is test code.

