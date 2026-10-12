## Entry 891 — 2026-07-18 00:05 PDT — PHASE-13 Wave H — SCREEN SECTION §4.2.7 non-support warning (COBOLNET1560 band)

Replaced the SCREEN SECTION silent-drop with the §4.2.6-mandated compile-time WARNING, establishing the
COBOLNET1560 non-support band that `docs/CONFORMANCE.md` §4 catalogues. A normal program's SCREEN SECTION (ISO §13.9,
an OPTIONAL facility §4.2.7 COBOL.NET does not implement) parsed but was silently dropped; now `DataBinder.Bind`
emits `warning COBOLNET1560: the SCREEN SECTION … is an optional facility (§4.2.7) that is not supported …` via
`EditionContext.Warning` (a warning, not an error — the program still compiles and runs; the screen behavior is
absent). CLI-probed: the warning appears and the program prints DONE. Closes the audit's SCREEN row. The remaining
Wave H code half — the recognize-and-name diagnostics for MCS (SEND/RECEIVE), COMMIT/ROLLBACK, and VALIDATE (which
today hit a generic COBOL0001, not a named §4.2.6/§4.2.13 non-support) — needs lexer/grammar recognition of those
keywords (→ a shared-parser change + full legacy guard) and is the next Wave H increment.

