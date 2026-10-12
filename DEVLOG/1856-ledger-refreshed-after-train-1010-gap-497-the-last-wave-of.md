## Entry 1856 — 2026-10-03 18:20 PDT — Ledger refreshed after train 1010 (GAP 497); the last wave of the weekly quota

Train 1010 (DEVLOG 1855) landed PB1412's EVALUATE half, PB1294 (report rolled totals) and PB1947 (a constant-name at every
report-writer integer position), GAP 504 to 497, CI green. The owner asked to use the last of the weekly quota without losing
work to a limit hit, so the wave was sized for about two points (two small Sonnet groups, 1.34 M subagent tokens in all) with a
checkpoint commit and a stamped `STATUS.md` after every step, and no second wave. `ledger-trend.json` gained the point for train
1010 (GAP 497, closed 3,850), `ledger-in-flight.md` now names eight trains since the last refresh, and the page was regenerated
(`gen_ledger.py --check` clean) and republished to the same artifact (version 87). The first publish attempt was bounced by the
server's classifier with instructions to repeat the identical call; the repeat succeeded. The lander used the Opus 5.5 trailer, as
the earlier trains did.

**Files:** `docs/rearchitecture/evidence/ledger-trend.json`, `docs/rearchitecture/evidence/ledger-in-flight.md`. No code changed,
no diagnostic code used.

