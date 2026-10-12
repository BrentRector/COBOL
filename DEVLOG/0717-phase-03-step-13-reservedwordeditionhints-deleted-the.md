## Entry 717 — 2026-07-09 14:31 PDT — PHASE-03 Step 13: ReservedWordEditionHints DELETED — the reverse-signature recogniser is gone

With all 7 residue gates migrated to bind-time (Batch C, DEVLOG 715/716), the parse-layer reverse-signature recogniser
`ReservedWordEditionHints` had no remaining ISO consumer — its `id` switch was already all-`_ => null`. Deleted the
whole file (`DESIGN-version-conformance-pipeline.md` §5 Stage 2):
  - The one remaining LIVE path — the vendor JSON/XML `COBOL0313` disposition — relocated INLINE into
    `CobolErrorStrategy` (a token-keyed `token.Type is CobolLexer.JSON or CobolLexer.XML` hint). It is a parse-error
    re-diagnosis of hard-reserved lexer tokens (a misplaced vendor statement), NOT an ISO edition gate, so it belongs
    with the frontend's other generic-error hints — not in a version-gating file.
  - `src/Cobol.Net.Frontend/Parsing/ReservedWordEditionHints.cs` removed (`git rm`); no code referenced it (the
    remaining name mentions are stale doc-comments — the two most prominent, in `EditionGateDiagnosticTests`, refreshed
    to the bind-time / `CobolErrorStrategy` reality; the incidental ones ride the Step-15 doc sweep).
Verified: `JSON GENERATE … @2014` → COBOL0313 "not an ISO/IEC 1989 construct" (unchanged behaviour, new home). Battery:
greenfield conformance 3114 · unit 227 · characterization 32 GREEN; FULL legacy guard 353 MATCH, 0 regressions.
Exit criterion 7 (all residue migrated + the guesser deleted) now HOLDS. NEXT: Step 14 — the pipeline skeleton (the
`VersionConformancePass`; funnel all 88 compiler-embedded Checks; absorb + delete `EditionValidator`; edition-agnostic
binder; split bind/emit in `CompilerDriver`) — a large architectural step, taken next with fresh focus.

