## Entry 710 — 2026-07-08 21:37 PDT — Version-conformance migration, residue #7: PROPERTY clause → superset parse + bind-time gate (guesser arm deleted)

Second Batch-A construct (DESIGN-version-conformance-pipeline.md). The PROPERTY data-description clause (ISO §13.18.42,
COBOL-2002 OO) moves off the parse-time predicate + reverse-signature guesser onto the bind-time gate:
  - Grammar (CobolData.g4): dropped `{is2002()}?` from the `propertyClause` alternative of `dataDescriptionClause`.
    KEPT the two VALUE-list guards `{!(is2002() && LA(1)==PROPERTY)}?` — those are value-operand-loop disambiguation
    (PROPERTY terminates a VALUE clause at 2002+), NOT an edition gate. ANTLR regen.
  - Bind (DataBinder data-description clause loop, beside BASED/TYPEDEF/TYPE): `Check(PropertyClause2002, "the PROPERTY
    clause")` when `clause.propertyClause() is not null`. The OO property SEMANTICS stay in
    DataBinder.Oo.OoBindPropertyClauses — this branch only gates.
  - Guesser (ReservedWordEditionHints): deleted the `CobolLexer.PROPERTY when InRule(...)` arm.
  - Fixture: `tests/conformance/negative/property_below_2002.cob` (reject-at 85 → COBOLNET0900); manifest 49→50.
Verified: `01 FOO PIC X PROPERTY.` @85 → exact bind-time 0900; `01 PROPERTY PIC X.` @85 → NO 0900 (PROPERTY is the
data-NAME — greedy `dataName?` takes it; only the CLAUSE is gated); @2002 the §8.9 funnel rejects the user-word
PROPERTY (COBOLNET0901); a real OO class PROPERTY @2002 compiles CLEAN (property path intact). Battery: greenfield
conformance **3110** (+1) · unit 227 · characterization 32 GREEN; FULL legacy guard **ALL GREEN**. 5 residue constructs
remain (PD-RAISING, XOR next in Batch A; then SHARING/RETRY/boolean in B/C).

