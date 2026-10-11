## Entry 1860 — 2026-10-04 08:45 PDT — Ledger refreshed after trains 1011 and 1012 (GAP 381); a per-landing trend point is now the rule

The owner pointed out the ledger had not been refreshed since train 1010 although trains 1011 and 1012 had landed (GAP 497 to
426 to 381). `gen_ledger.py` appends only the measurement for the current head, so the eight inventory-moving commits of the
two trains were back-filled into `ledger-trend.json` from each commit's own `traceability-inventory.json` (112 points; every
field read with `git show <sha>:tests/version-matrix/traceability-inventory.json`, none typed), `ledger-in-flight.md` now names
the two trains, wave 1013 and the Opus wave planned after it, and the page was regenerated and republished to the same artifact
(version 88). The orchestrator refreshes the ledger after every train it sees land, in the same turn as the landing report, not
at the end of the day.

**Files:** `docs/rearchitecture/evidence/ledger-trend.json`, `docs/rearchitecture/evidence/ledger-in-flight.md`. No code changed,
no diagnostic code used.

