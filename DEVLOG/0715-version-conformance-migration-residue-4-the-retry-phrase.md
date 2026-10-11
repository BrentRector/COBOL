## Entry 715 — 2026-07-09 13:39 PDT — Version-conformance migration, residue #4: the RETRY phrase → superset parse + bind-time gate (forward-detect for OPEN); a real 4-site gap closed

PHASE-03 Step 12(a) — the first half of Batch C (`DESIGN-version-conformance-pipeline.md` §4/§5 Stage 1). The RETRY
phrase (ISO §14.7.9, COBOL-2002) on all six I/O statements moves off the parse-time predicates + the reverse-signature
guesser onto the single bind-time mechanism:
  - Grammar (`Core/CobolIO.g4`, SIX sites): the FIVE statement sites (`readStatement`/`writeStatement`/
    `rewriteStatement`/`deleteStatement`/`deleteFileStatement`) drop their `{is2002()}?` predicate → `(retryPhrase)?`
    (superset — the file is named BEFORE the phrase, so no name-list ambiguity). The OPEN site
    (`openClause`) becomes `{is2002() || retryPhraseAhead()}?` — RETRY sits inside the `openFileSpec+` name-list where
    it is a legal §8.9 user word below 2002, so it needs a forward detect. ANTLR regen.
  - `retryPhraseAhead()` (new peer of `boolExprAhead()` on `CobolParserCoreBase`): true only on an UNAMBIGUOUS numeric
    tail (`n TIMES` | `FOR? n SECONDS` — an integer can never be a file name) with a trailing file name still to come.
    `RETRY FOREVER` (FOREVER is §8.10 user-legal) and a bare `RETRY name` are ambiguous → false → defer to `is2002()`.
    (Corrected the design §2.3 spec, which loosely listed `FOREVER` as a complete tail — it is excluded; the
    consequences already stated the ambiguous-FOREVER resolution.)
  - Bind — a real latent gap CLOSED: only OPEN and keyed-READ actually bind the retry phrase; sequential-READ, WRITE,
    REWRITE, DELETE, DELETE FILE parse it as a documented no-op residue and never bound it — so the parse predicate was
    the ONLY gate for those four. New `GateRetryIntro(rp)` → `Check(RetryPhrase2002)` fires once per statement carrying
    the phrase, called at the entry of all six statement binders (BindOpen per-clause · BindRead · BindWrite ·
    BindRewrite · KeyedBindDelete · KeyedBindDeleteFile), before any organization-based routing/early-return. Below 2002
    every site now yields an exact COBOLNET0900 that four of them previously missed.
  - Guesser (`ReservedWordEditionHints`): deleted the `CobolLexer.RETRY` arm — only the boolean family remains (#2).
  - Fixture: `tests/conformance/negative/retry_below_2002.cob` (reject-at 85 → COBOLNET0900). The `retry-phrase-2002`
    matrix row (`expectDiagnostic COBOLNET0900`) covers both directions.
Verified: `READ … RETRY 3 TIMES` / `WRITE … RETRY 5 TIMES` (a formerly-ungated site) @85 → exact 0900; @2002 → clean;
the OPEN collision — a file NAMED RETRY, `OPEN INPUT RETRY. CLOSE RETRY.` @85 → compiles CLEAN (exit 0); the ambiguous
`OPEN INPUT RETRY FOREVER F.` (three files) @85 → all file names, no 0900; the real `OPEN INPUT RETRY 3 TIMES F.` @85 →
0900. Battery: greenfield conformance **3113** (+1) · unit 227 · characterization 32 GREEN; FULL legacy guard ALL GREEN
(shared grammar). RESUME AT Step 12(b): the boolean family #2 (the shared comparison DFA — highest scrutiny, DEVLOG 621).

