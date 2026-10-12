## Entry 886 — 2026-07-17 20:10 PDT — PHASE-13 Wave B (start) — EC-SIZE-TRUNCATION verified + goldened (§14.7.5 / VCR 53)

The P13 as-built audit (Entry, this session) showed EC-SIZE-TRUNCATION was ALREADY RAISED (the plan's
"observable level only" premise is stale): `ArithmeticEmitter` latches "EC-SIZE-TRUNCATION" on a ROUNDED MODE IS
PROHIBITED inexact store AND on receiver-capacity overflow, flowing to `ExceptionState.Set` via `EcEmitter`. CLI
probe confirmed: a `COMPUTE X ROUNDED MODE IS PROHIBITED = 0.35` (X = PIC 9V9) under `>>TURN EC-SIZE CHECKING ON`
raises the fatal EC; observed via `ON SIZE ERROR` + `FUNCTION EXCEPTION-STATUS` = `EC-SIZE-TRUNCATION`, with the
receiver LEFT UNCHANGED (§14.7.5). So this row is verify+golden, not implement: added the golden
`tests/conformance/2023/ec_size_truncation_prohibited` (GreenfieldOnly — the named-EC / >>TURN / EXCEPTION-STATUS
model is greenfield; the frozen legacy has a limited EC model). Greenfield CorpusRunner 253 green, legacy
ConformanceTests 147 green (skip confirmed), no compiler change → NIST untouched.

**EC-BOUND-OVERFLOW / EC-BOUND-REF-MOD (STAGED, documented residue):** both are catalogued (nonfatal / fatal) but
NOT raised — raising them needs the ambient-gate pipeline generalized (a per-statement `ExceptionState.
BoundOverflowChecking`/`BoundRefModChecking` flag set by `EcEmitter` from the compile-time `TurnState`, plus the
`EcBinder` wrapping every table-grow / ref-mod-bearing statement in a `BoundEcChecked`, plus the runtime raise at
`CobolDynTable` grow-past-capacity and `CobolString.RefMod` zero-length/out-of-range). That is a substantial
EC-engine change with real regression surface; deferred to a dedicated sub-increment rather than rushed into this
wave (both remain LOUD-not-silently-wrong today — the `CobolDynTable` comment already flags the site). Next: the
2023 grammar constructs (Wave C), batched by the shared full-legacy-guard gate.

