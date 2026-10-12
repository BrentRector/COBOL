## Entry 391 — M2-ARITH-1 follow-up: apply OPTIONS `DEFAULT ROUNDED MODE` (ISO §11.9.6, COBOL-2002)

Closed the first of the two M2-ARITH-1 follow-ups: the OPTIONS paragraph's `DEFAULT ROUNDED MODE IS <mode>`
clause now sets the rounding mode a **bare** ROUNDED phrase uses (previously bare ROUNDED was always the fixed
NEAREST-AWAY-FROM-ZERO). The OPTIONS paragraph body is still a token blob (`optionsContent : ~DOT+`); a new
`SemanticBuilder.VisitOptionsParagraph` token-scans it for `DEFAULT ROUNDED [MODE] [IS] <mode-name>`, maps the
name to the same 0–7 ordinal as the per-statement ROUNDED MODE (`MapRoundingModeName`), and records it.
`Compilation` transfers `semanticBuilder.DefaultRoundingMode` → `SemanticModel.SetDefaultRoundingMode` (alongside
the program collating-sequence transfer); `ArithmeticStatementBinder.BindRounded` (made instance, was static)
returns `_ctx.Semantic.DefaultRoundingMode` for a bare ROUNDED. Default stays 1 (NEAREST-AWAY) when no OPTIONS
DEFAULT ROUNDED MODE is present.

Verified: `DEFAULT ROUNDED MODE IS TRUNCATION` → 2.5 yields 2 (vs the default 3); `NEAREST-EVEN` (banker's) →
2.5→2, 3.5→4, 0.5→0; a per-statement `ROUNDED MODE IS …` still **overrides** the OPTIONS default (precedence,
ISO §14.9.4); and a program with no OPTIONS keeps NEAREST-AWAY (no regression — the existing `options_paragraph`
conformance test still passes). Low regression risk overall: OPTIONS is COBOL-2002 and absent from the NIST
('85) corpus. Conformance `tests/conformance/2002/options_default_rounded`.

**Remaining (still deferred):** M2-ARITH-1 follow-up #2 — `PROHIBITED` → raise `EC-SIZE-TRUNCATION` on an inexact
result — needs the EC/exception framework (M2-PROC-4), not yet built. M2-ARITH-2 — the other OPTIONS clauses
(`ARITHMETIC IS STANDARD/…`, `INTERMEDIATE ROUNDING`, `FLOAT-BINARY/DECIMAL DEFAULT`, `ENTRY-CONVENTION`) — still
recognize-and-ignore (STANDARD arithmetic is a real intermediate-precision change).

Guard ALL GREEN (1047 unit / 480 integration / 364 NIST), 0 regressions.

