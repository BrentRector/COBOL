## Entry 364 — M2: the COBOL-2002 OPTIONS paragraph (ISO §11.9) is accepted

A survey for the next M2 piece established that WS-2002-FORMAT and the intrinsic-function set are already complete
(the runtime `IntrinsicFunctions.cs` dispatches NUMVAL-F, TEST-NUMVAL/-C, FIND-STRING, FORMATTED-*, DISPLAY-OF/
NATIONAL-OF, CONVERT, E, … — far more than the earlier compiler-project grep suggested), the EXIT forms are
already in the grammar, and **WS-2002-UDF is blocked by the same deep gap as CALL RETURNING** (a user function
returns via the RETURNING mechanism whose callee-LINKAGE→caller-pointer wiring is the unresolved runtime gap of
DEVLOG 354). So the tractable, non-blocked next piece was the **OPTIONS paragraph**.

Empirically, `OPTIONS. ARITHMETIC IS STANDARD DEFAULT ROUNDED MODE IS NEAREST-EVEN.` failed to compile
("unexpected 'IS'") — there was no grammar for it. Added:
- lexer token `OPTIONS : 'OPTIONS'` (ISO §11.9 paragraph header). Corpus-safe: the only `OPTIONS` occurrences in
  NIST/fixtures are hyphenated identifiers like `C50-PRINT-OPTIONS` (protected by maximal munch) or text inside
  comments/literals — verified, and the full guard confirms no tokenization regression.
- parser rule `optionsParagraph : OPTIONS DOT (optionsContent DOT)?` with `optionsContent : ~DOT+`, wired as the
  first alternative of `identificationParagraph`. The clause tokens (ARITHMETIC / DEFAULT ROUNDED /
  ENTRY-CONVENTION / FLOAT-BINARY / FLOAT-DECIMAL / INITIALIZE / INTERMEDIATE ROUNDING) carry no embedded periods,
  so they are swallowed up to the terminating separator period — which §11.9.3 requires only when a clause is
  present, so a bare `OPTIONS.` header parses too.

Scope (documented follow-up): the clauses are **accepted/consumed, not yet applied** — the program compiles with
default behavior. Applying DEFAULT ROUNDED MODE (the default rounding for ROUNDED phrases without an explicit
mode) and ARITHMETIC IS STANDARD-DECIMAL/-BINARY are real arithmetic changes left for later WS-2002-PROC work.

Tests: `OptionsParagraph_IsAccepted` (clauses → `OPTOK`), `OptionsParagraph_BareHeader_IsAccepted`
(bare `OPTIONS.` → `BARE`).

Guard ALL GREEN: **1047 unit / 449 integration / 364 NIST**, 0 regressions.

