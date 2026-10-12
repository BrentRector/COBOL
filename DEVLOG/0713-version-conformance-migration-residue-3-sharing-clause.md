## Entry 713 — 2026-07-08 22:48 PDT — Version-conformance migration, residue #3: SHARING clause/phrase → superset parse + bind-time gate (Batch B; OPEN name-list collision proven byte-safe)

Batch B (DESIGN-version-conformance-pipeline.md). The SHARING clause (SELECT, ISO §12.4.5.15) and the OPEN SHARING
phrase (§14.9.27), COBOL-2002, move off the parse-time predicates + reverse-signature guesser onto bind-time gates:
  - Grammar (CobolIO.g4): dropped `{is2002()}?` from the `sharingClause` SELECT alternative (:68) and from the
    `sharingPhrase` in `openClause` (:232). The sibling `retryPhrase` predicate is LEFT gated (its own migration #4).
    ANTLR regen.
  - Bind: SELECT → `Check(FileSharingClause2002, "the SHARING clause")` in the DataBinder file-control loop; OPEN →
    `Check(FileSharingClause2002, "the OPEN SHARING phrase")` in BindOpen when `clause.sharingPhrase() is not null`.
  - Guesser (ReservedWordEditionHints): deleted the `CobolLexer.SHARING` arm (RETRY is the last file-family arm left).
  - Fixture: `tests/conformance/negative/sharing_below_2002.cob` (reject-at 85 → COBOLNET0900).
**The OPEN name-list collision — the one risk the design flagged as not certifiable by static reasoning — is proven
byte-safe:** a file legitimately NAMED SHARING at 85, `OPEN INPUT SHARING. CLOSE SHARING.`, compiles CLEAN (exit 0) —
ANTLR's ALL(*) correctly disambiguates a bare `SHARING` (file name) from the `SHARING WITH <mode>` phrase (the phrase
needs a `sharingMode` after it). Verified: real SELECT SHARING @85 → exact 0900; real OPEN `SHARING WITH NO OTHER F`
@85 → 0900; @2002 → clean; mis-fire garble @85 → no spurious 0900. The full legacy guard (whole NIST corpus compiled
at 85) is the backstop for the collision. Battery: greenfield conformance · unit 227 · characterization 32 GREEN; FULL
legacy guard ALL GREEN. 2 residue constructs remain: **Batch C — RETRY + the boolean family** (RETRY needs a forward
`retryPhraseAhead()` for OPEN; the boolean family touches the shared comparison DFA — highest scrutiny, DEVLOG 621).

