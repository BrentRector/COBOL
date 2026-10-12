## Entry 716 — 2026-07-09 14:14 PDT — Version-conformance migration, residue #2 (LAST): the boolean family → superset parse + bind-time gate — BATCH C COMPLETE, all 7 residue gates migrated

PHASE-03 Step 12(b) — the second half of Batch C and the HIGHEST-scrutiny residue (the shared comparison DFA, DEVLOG
621). The boolean operators (B-AND/B-OR/B-XOR/B-NOT, ISO §8.7.2/§8.8.2) + the boolean COMPUTE Format 2 (§14.9.8) move
off the parse-time predicates + the reverse-signature guesser onto the single bind-time mechanism:
  - Grammar (`Core/CobolExpressions.g4` + `CobolParserCore.g4`): dropped `{is2002()}?` from all five operator-tier
    sites (booleanExpression B_OR, booleanXorTerm B_XOR, booleanAndTerm B_AND, booleanFactor B_NOT + LPAREN) and from
    the COMPUTE F2 alt — the tiers/F2 are superset-parsed (F1 arithmetic is tried first; only a genuine boolean RHS
    falls to F2). The condition ENTRY `{is2002() && boolExprAhead()}?` → `{boolExprAhead()}?`. ANTLR regen.
  - `boolExprAhead()` (`CobolParserCoreBase`) generalized to fire at ALL editions, WITH OPERAND-ADJACENCY: a binary
    B-op counts only with a completed operand immediately before it; a unary B-NOT only with an operand immediately
    after. This is load-bearing — below 2002 `B-AND`/`B-OR`/`B-XOR` are legal §8.9 user words, so `IF B-AND = 5` (a
    plain comparison on a data item named B-AND) must NOT enter the boolean channel (a leading, operand-less B-op is a
    name, not the operator). The shared `comparisonExpression` rule is untouched (the DEVLOG-621 invariant): a B-op-free
    condition still returns false and falls through unchanged.
  - Bind: `Check(BooleanOperators2002)` fires ONCE per boolean expression — in `BindPrimaryBoolean` (guarded by
    `be.Any(HasBoolOp)`, covering the simple + relation condition paths) and in `BindComputeBoolean` (guarded by
    `HasBoolOp`) — chosen over BindBoolExpr to avoid a duplicate on nested boolean parens. A bare boolean literal RHS
    (no B-op) is still gated by the existing `BooleanData2002`.
  - Guesser (`ReservedWordEditionHints`): deleted the B-AND/B-OR/B-XOR/B-NOT arm and the COMPUTE-with-B-op-ahead arm.
    **ALL 7 reservation-word residue arms are now gone**; the switch retains only the vendor JSON/XML COBOL0313 path
    (Step 13 deletes the whole file). `NextWithin` is now unused (removed with the file at Step 13).
  - Fixture: `boolean_operators_below_2002.cob` (reject-at 85 → COBOLNET0900); the `boolean-operators-2002` matrix row
    covers both directions.
Verified: `IF A B-AND B` / `COMPUTE C = A B-AND B` / `COMPUTE C = B-NOT A` @85 → exact 0900; @2002 → clean; the
mis-fire is GONE — `01 B-AND PIC 9. … IF B-AND = 5` (data item named B-AND) @85 → plain comparison, compiles CLEAN, no
0900; normal + subscripted comparisons (`IF X = 5`, `IF E(I) = 0`, `IF X = 5 AND E(I) = 0`) @85/@2002/@2023 → clean,
never mis-gated (DEVLOG-621 surface intact). Battery: greenfield conformance **3114** (+1) · unit 227 · characterization
32 GREEN; FULL legacy guard 353 MATCH, 0 regressions.

**BATCH C COMPLETE — ALL 7 residue gates migrated** (UNLOCK #5, PROPERTY #7, PD-RAISING #6, XOR #1, SHARING #3, RETRY
#4, boolean #2). `ReservedWordEditionHints` now holds only the non-ISO vendor JSON/XML disposition. NEXT: Step 13 —
delete `ReservedWordEditionHints` entirely (relocate the JSON/XML COBOL0313 hint to `CobolErrorStrategy`); then Step 14
(the pipeline skeleton) and Step 15 (phase close). (Step 12(c) — refreshing the migrated rows' stale constructs.json
citations + grammar comments — folds into the Step 13/14 change sets.)

