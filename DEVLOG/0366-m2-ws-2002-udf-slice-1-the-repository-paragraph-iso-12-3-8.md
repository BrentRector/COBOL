## Entry 366 — M2 (WS-2002-UDF, slice 1): the REPOSITORY paragraph (ISO §12.3.8) is accepted

With CALL … RETURNING fixed (365), WS-2002-UDF is unblocked — but the full feature (FUNCTION-ID…END FUNCTION
compilation units, the `FUNCTION user-name(args)` expression invocation, cross-unit result typing) is irreducibly
multi-commit. This is the sanctioned first slice: the **REPOSITORY paragraph**, a UDF prerequisite and itself
ubiquitous in 2002 source (`REPOSITORY. FUNCTION ALL INTRINSIC.`).

Empirically `REPOSITORY. FUNCTION ALL INTRINSIC.` failed to compile ("unexpected 'FUNCTION'") — REPOSITORY was not
even a token (it lexed as IDENTIFIER, and the vendor-paragraph fallback then choked on the FUNCTION keyword).
Added:
- lexer tokens `REPOSITORY` and `INTRINSIC` (`ALL`/`FUNCTION` already existed). Corpus-safe: both have **zero**
  standalone occurrences in NIST/fixtures (INTRINSIC appears only inside comments) — verified, and the full guard
  confirms no tokenization regression.
- `repositoryParagraph : REPOSITORY DOT (repositoryEntry DOT?)*` with
  `repositoryEntry : FUNCTION ALL INTRINSIC | FUNCTION functionName INTRINSIC?`, wired into
  `configurationParagraph` (before the vendor catch-all). Each entry begins with the FUNCTION specifier keyword,
  so the rule cannot over-run into the next section; the optional per-entry period tolerates both the
  one-period-per-paragraph and period-per-entry styles.

Scope (documented follow-ups, the rest of WS-2002-UDF): the specifiers are accepted but **not bound** — no
function-prototype table is built yet, `FUNCTION ALL INTRINSIC` does not yet enable calling intrinsics without the
`FUNCTION` keyword, the `AS external-name` phrase is deferred (avoids reserving `AS`), and CLASS/INTERFACE/
PROGRAM/PROPERTY specifiers (OO) are not modelled. The next UDF slices are FUNCTION-ID…END FUNCTION as a
compilation unit and the `FUNCTION user-name(args)` invocation (which reuses the now-working RETURNING path).

Tests: `Repository_FunctionAllIntrinsic_IsAccepted` (→ `REPOK`), `Repository_MultipleEntries_IsAccepted`
(`FUNCTION SQRT INTRINSIC` + `FUNCTION MY-FUNC` → `REP2`).

Guard ALL GREEN: **1047 unit / 452 integration / 364 NIST**, 0 regressions.

