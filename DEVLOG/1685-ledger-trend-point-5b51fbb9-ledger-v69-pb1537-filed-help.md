## Entry 1685 — 2026-09-24 16:25 PDT — Ledger trend point 5b51fbb9 (ledger v69); PB1537 filed (`--help` crashes on a cp1252 pipe)

**What.** The Conformance Ledger was regenerated at the registrar-1 landing `5b51fbb96` (GAP 1652 · closed 2696 · DNS
381) and published as v69; `gen_ledger.py` appended that point to `docs/rearchitecture/evidence/ledger-trend.json`
(81 points). Filed kb/Work PB1537: `python scripts/spec/work.py --help | head` and the same for `gen_ledger.py` raise
`UnicodeEncodeError` (cp1252) on Windows when stdout is a pipe — some scripts reconfigure stdout to UTF-8 by hand, the
rest do not; the fix is one shared helper plus a drift test over every script's `--help`, not a 41st copy. Meanwhile
PB1522 step 3 (writing the 126 missing §7 determination rows) is running in the cloud orchestrator on
`claude/adj-doc-rows-1`.

