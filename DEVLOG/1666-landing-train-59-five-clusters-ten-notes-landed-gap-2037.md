## Entry 1666 — 2026-09-23 11:21 PDT — Landing train 59: five clusters, ten notes landed, GAP 2037 → 2026

**Landing train 59 carried FIVE clusters and TEN notes to `landed` in one landing** (wave 58: KA, KE, KG, KD, KF), one
commit per cluster, gated once on the merged tree. **GAP 2037 → 2026 (−11).** New diagnostic codes COBOLNET2409,
COBOLNET2418 and COBOLNET2419.

**KA — PB999 + PB1000 (LOG/trig past binary64; PRESENT-VALUE's discount base).** PB999 is back after train 57 dropped
it for +20 `cobolnet-no-biginteger` hits. On re-probe, LOG and LOG10 of 10^±400 answered 0, SQRT answered 0 or
Infinity, and SIN/COS/TAN of 10^400 answered 0 (§15.55.4 1), §15.56.4 1), §15.82.4 1), §15.20.4 1), §15.89.4 1)). The
fix reduces the argument with `CobolIntrinsics.ReduceTwoPi`, a decimal Payne–Hanek on Int128 over a 6200-place 1/(2π)
constant. `InverseTwoPiDigitsDriftTests` re-derives that constant independently in the test assembly. The exact intake
now also covers SCALED operands: `WholeRangeBodies` is an `ExactIntake` map, and `IntrinsicRenderer.ExactArg` is the one
place an exact operand enters. semgrep `cobolnet-no-biginteger` stayed at 46.

PB1000's scalar refusal did not reproduce, but the value was wrong. A legal rate of −0.99999999999999999 narrowed to −1,
which made the base 1 + rate zero, so the answer was 0 (§15.74.4 1) a)). The body now takes the discount base formed on
the exact rate. PRESENT-VALUE's table(ALL) lead is screened on the exact first element through `CobolTable.AllArgs`'s
new lead lambda (§15.74.3 2), §15.3 14)).

Goldens: the new `85/pb1000_present_value_rate_near_minus_one`, plus added arms in pb999 and pb952. The rows got
witnesses only.

**KE — PB548 + PB460 + PB454 (SET's pointer and switch arms).**
- **PB548:** the plain `SET p TO q` Format-7 route now asks the same restricted-pointer screen that `BindSetAddress`
  already used (§14.9.39.3 SR19, §13.18.60.4 GR23).
- **PB460:** EC-BOUND-SET now has a raise site, in `CobolDynTable.SetCapacity` (§14.9.39.4 GR30). Two DETERMINATIONS
  are recorded in the A.4.4 row: the exception is raised on every SET past the expected capacity, before the change,
  and it is EC-BOUND-SET alone. DOC-A.1-175 is documented as NONE.
- **PB454:** re-probed as already fixed by PB862. The second, switch-only mnemonic map is deleted, and a drift test
  guards against it coming back.

Six rows moved to CONFORMS.

**KG — PB510 + PB569 (the lexer was erasing distinctions the standard draws).**
- **PB510:** each figurative spelling is now its own token. ZERO, ZEROS and ZEROES (and the other four singular/plural
  pairs) used to share one token, so BLANK WHEN ZEROS, the sign condition's IS ZEROS and OPTIONS INITIALIZE … BINARY
  ZERO all compiled. The §8.3.3.6.2 interchangeability is now written once, as five family rules under
  `figurativeConstant`. CobolErrorStrategy arm 0d names the misspelling as COBOLNET2418. PIC/PICTURE is the one fold
  left, and `KeywordSpellingDriftTests` pins it.
- **PB569:** §13.18.40.3 SR7 is now enforced for both symbols by `PictureSeparatorPeriodRule` (COBOLNET2419).
  Contrary to the note's refutation, the '.' arm was also silent before this fix.

Design doc: DESIGN-frontend-grammar §3.12.

**KD — PB444 + PB446 (SEARCH's grammar now spells its two printed formats).**
- The NOT AT END arm is gone, SEARCH ALL takes exactly one WHEN, and the dead `searchAllKeyPhrase` is deleted. Before,
  `KEY IS K` was accepted and then discarded. Whatever fits neither format is re-coded as COBOLNET2269 from the parser
  state.
- NEXT SENTENCE is admitted only as the whole of an IF Format-2 arm or of a SEARCH WHEN body
  (`StatementBinder.IsNextSentenceArm`).
- §14.9.37.3 SR4 is now one check covering both formats (COBOLNET2409).
- Rows: SR-14.9.37.3-4 is CONFORMS. FMT-14.9.37.2 stays DIVERGES on PB351's undelimited-conditional residue, so PB446
  closes no row; its `closes_rows_reason` records this.

**KF — PB422 (§8.3.3.6.3 SR3, a multi-character ALL literal associated with a numeric item).** On re-probe, SR8 and
SR6 were already closed. SR3 is now one predicate, `FigurativeAssociation`, on its own construct row. It is asked by
the MOVE gate and by the one relation checkpoint; the comparison association was the missed sibling. DETERMINATION
(VCR 7.13): the construct was deleted in ISO 2002, so the compiler accepts it at 85 and refuses it at 2002 and later.
Three rows moved to CONFORMS.

**The train itself.**
- **Merge.** The five cluster diffs were applied in manifest order onto 68bbdb00f. The train was rebased onto ed699e767,
  which touched docs and skills only, and then onto train 58 (a53bce791) once that train landed. KD conflicted with KG in four places, and every conflict took both sides:
  - `DiagnosticDescriptors` got both descriptor blocks.
  - The negative manifest, `docs/DIAGNOSTICS.md` and the design doc each got both sides; KD's SEARCH paragraph closes
    §3.11, then KG's §3.12 follows.
  - Both clusters had added a `CobolErrorStrategy` arm named **0d**. Both arms are kept, and KD's is renumbered
    **0e**. Their ranks are unchanged: 2418 at −1, 2269 at 0.

  KF's manifests took both sides as whole elements, with element counts checked against the base. Constructs, the VCR
  and DIAGNOSTICS were regenerated, with no content drift.
- **Verdicts.** Six batch files were re-applied: 21 records. On the first merged tree that was GAP 2060 → 2049. After the
  rebase onto train 58, the inventory was taken from main and the batches were re-applied: GAP 2037 → 2026.
- **First gate: red on one test, attributed to KD.** `RedefinesSubscriptedViewDifferentialTests.NextSentence_SkipsTrailOfOwnSentence`
  failed on COBOLNET2269. Its source wrote `IF W = 1 NEXT SENTENCE DISPLAY "TRUE-TAIL".`, which puts a statement after
  NEXT SENTENCE inside one THEN phrase. That matches neither §14.9.19.2 format, and KD refuses it by design. None of
  the implementers' filters selected that class. The test now closes the IF with END-IF (the §3.11 determination),
  keeps the trailing statement it measures, and its differential golden was rebaked as SENTENCE-2 / ELSE-ARM /
  SENTENCE-4 (§14.9.19.4 GR4–GR5).
- **Pre-existing red on base.** `audit_code_citations` flagged registrar 13's `kb/Work/PB1036.md` (it read "SORT GR12 b)"
  as §13.18.43.4 GR12b). Train 58 landed the same repair first, and its text was kept at the rebase.
- **Second gate: GREEN.** Whole Conformance assembly unfiltered: 8,393/8,393. Unit: 29,148. Characterization: 33.
  Legacy integration: 503, plus 1 skipped.
- **Third gate, after the rebase onto train 58: GREEN.** It was re-run because the rebase conflicted in the 2002, 85
  and negative manifests, `DiagnosticCatalog.cs` and DIAGNOSTICS, all resolved by taking both sides as whole elements.
  Train 58 took codes 2403–2406 and 2421–2423, so there was no code collision. Whole Conformance: 8,484/8,484. Unit:
  29,190. Characterization: 33. Legacy integration: 503, plus 1 skipped. semgrep did not move (46 / 2 / 308 / 3). The KG and KD implementers each
  saw a first-gate red on `GrammarDiagramGeneratorDriftTests.Generator_RunsClean`, caused by the shared
  `%TEMP%\cobolnet-grammar-check` directory. It passed in this gate.
- **Dropped clusters:** none.

