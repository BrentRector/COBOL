## Entry 712 — 2026-07-08 22:01 PDT — Version-conformance migration, residue #1: XOR/EXCLUSIVE-OR operator → superset parse + bind-time gate — BATCH A COMPLETE

Fourth and final Batch-A construct (DESIGN-version-conformance-pipeline.md). The logical XOR / EXCLUSIVE-OR operator
(ISO §8.8.4.9, COBOL-2023) moves off the parse-time predicate + reverse-signature guesser onto the bind-time gate:
  - Grammar (CobolExpressions.g4:73): dropped `{is2023()}?` from the `logicalXorExpression` connective — XOR parses at
    all editions (superset; a bare user-word XOR is never valid in the connective slot). ANTLR regen.
  - Bind (StatementBinder.BindXorSequence, new helper on the BindCondition XOR arm): `Check(LogicalXorOperator2023,
    "the logical XOR operator")` guarded on `xorExpr.ChildCount > 1` (an XOR/EXCLUSIVE_OR terminal genuinely present
    between two operands), so a bare below-2023 `logicalAndExpression` is untouched.
  - Guesser (ReservedWordEditionHints): deleted the `CobolLexer.XOR or CobolLexer.EXCLUSIVE_OR` arm.
  - Fixture: `tests/conformance/negative/xor_below_2023.cob` (reject-at 85 2002 2014 → COBOLNET0900); manifest → 51.
Verified: real `IF A = 1 XOR B = 1` @2014 → exact bind-time 0900; @2023 → clean; the mis-fire is GONE (`PERFORM XOR
XOR.` with `01 XOR PIC 9` @2014 → NO spurious 0900); a data item named XOR compiles CLEAN @2014, and the §8.9 funnel
rejects the user-word XOR @2023 (COBOLNET0901). Battery: greenfield conformance **3111** (+1) · unit 227 ·
characterization 32 GREEN; FULL legacy guard ALL GREEN.

**BATCH A COMPLETE** — the four pure-gating residue constructs (UNLOCK #5, PROPERTY #7, PD-RAISING #6, XOR #1) are
migrated to the single bind-time mechanism, and the DEVLOG-708 mis-fire is eliminated for each. `ReservedWordEditionHints`
now holds only the SHARING/RETRY arms (Batch B/C) + the boolean-operator + COMPUTE arms (Batch C) + the vendor JSON/XML
disposition. **NEXT: Batch B — SHARING** (bind-time for SELECT + OPEN; the OPEN name-list collision needs the
adversarial characterization fixtures), then **Batch C — RETRY + the boolean family** (forward-detection for OPEN RETRY
and the boolean-condition ENTRY; highest-scrutiny per DEVLOG 621).

