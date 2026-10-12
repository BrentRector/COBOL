## Entry 1362 — 2026-08-29 01:52 PDT — Battery #29 over the PB124 campaign: green with one schedule-dependent legacy red the log could not NAME — so the harness learns to name them (PB127)

`bash scripts/battery.sh` over `3b3bc921`: FULL Conformance 4954/4954 · Unit 4589/4589 · characterization
33/33 · NIST 353 MATCH / 0 REGRESSION (audit CLEAN) · GnuCOBOL differential 0 PER-CASE FLIP(S). The one red:
guard-fast's `int_rc=1` — a single failure among the LEGACY CobolSharp.Tests.Integration suite's 504, whose
name the log could not carry: both .NET legs ran `--verbosity quiet` and the report grepped only summary
lines. A serial re-run returned 503/504 green (the skip is the documented D10 block), so the red was
schedule-dependent — but the no-flake-without-a-name discipline requires the NAME, and the harness could not
give one. kb/Work PB127: both legs now add `--logger "console;verbosity=minimal"` and the report greps carry
the [FAIL] lines, so the next occurrence arrives named (and, if it is the process-globals family PB126
documented for the greenfield suite, becomes its own note). The unnamed red itself is dispositioned as
logged: legacy-suite, schedule-dependent, serially green — the legacy suite is the opt-in differential net
and is deleted at P15.

