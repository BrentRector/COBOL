## Entry 711 — 2026-07-08 21:48 PDT — Version-conformance migration, residue #6: PROCEDURE DIVISION RAISING → superset parse + bind-time gate (the DEVLOG-708 mis-fire ELIMINATED)

Third Batch-A construct (DESIGN-version-conformance-pipeline.md) — and the one whose mis-fire (Entry 706/708) started
the whole redesign. PROCEDURE DIVISION … RAISING (ISO §14.2.2, COBOL-2002) moves off the parse-time predicate + the
mis-firing reverse-signature arm onto the bind-time gate:
  - Grammar (CobolParserCore.g4:487): dropped `{is2002()}?` before `raisingClause` in `procedureDivision` — RAISING
    parses at all editions (superset), like its already-bind-gated sibling `returningClause`. ANTLR regen.
  - Bind (DataBinder.Linkage, beside the RETURNING gate): `if (pd.raisingClause() is not null)
    Check(ProcedureRaising2002, "the PROCEDURE DIVISION RAISING phrase")`. The RAISING clause's EC SEMANTICS stay in
    StatementBinder.Exceptions.
  - Guesser (ReservedWordEditionHints): deleted the `CobolLexer.RAISING when InRule(procedureDivision)` arm — the one
    with the ineffective `InRule` narrowing that mis-fired on a user-word RAISING in a garbled body statement.
  - Fixture: the existing `raising_below_2002.cob` (reject-at 85 → COBOLNET0900) is unchanged and now exercises the
    bind-time gate.
Verified: real PD RAISING @85 → exact bind-time 0900; **the DEVLOG-708 mis-fire is GONE** (`PERFORM RAISING RAISING.`
with `01 RAISING PIC 9` @85 → NO spurious 0900); PD RAISING @2002 → compiles; GOBACK RAISING @2002 (EC statement) →
clean. Battery: greenfield conformance 3110 · unit 227 · characterization 32 GREEN; FULL legacy guard ALL GREEN. The
mis-fire that motivated the redesign is now structurally impossible for this construct. 4 residue constructs remain
(XOR next in Batch A; then SHARING/RETRY/boolean in B/C).

