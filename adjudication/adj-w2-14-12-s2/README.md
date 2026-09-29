# adj-w2-14-12-s2 — lane-3 adjudication (kb/Work PB1522 wave 2)

- Pinned sha: `0caa7d54004e81c4e34529d95764cdc1ccec1779` (0caa7d5)
- Inputs: 10 files, 19 rows (accept-statement 5, common-phrases 1, execution 1, write-statement 1,
  alternate-record-key 2, record-key 3, apply-commit 1, i-o-status 1, code-set 1, global-clause 3)
- Wall-clock (UTC): adjudicate 22:18:02 → 22:31:24 (~13 min, 19 rows); refute ~22:32 → 22:34:33 (~3 min, 1 row)
- Probe compiler: prebuilt `src/Cobol.Net.Cli/bin/Debug/net10.0/cobol` (no build in this batch)

## Verdict counts (pre-refute)
| verdict | n |
|---|---|
| NEEDS-OWNER-DECISION | 16 |
| DIVERGES | 1 |
| PARTIAL | 1 |
| CONFORMS | 1 |

`record_verdicts.py --dry-run` on these records: shape clean, GAP 1664 → 1663.

## Owner-question rows (all already owned by open owner notes)
- PB1151: GR-14.9.1.4-24, -L2.1, -L2.2, -L3.1, -L3.2 (ACCEPT Format-3 band), GR-14.6.11-7 (MCS), GR-14.7.7-3 (standard-decimal half probe-verified; standard-binary half declined)
- PB1099 Q1: GR-12.4.5.6.4-2, SR-12.4.5.6.3-6, FMT-12.4.5.12.2, GR-12.4.5.12.4-2, SR-12.4.5.12.3-5 (A.3 item 40 SOURCE form, COBOLNET1954)
- PB1099 Q2: FMT-12.4.6.3.2 (APPLY COMMIT, COBOLNET1709; zero-operand naming gap = PB1096)
- PB1255: GR-13.18.13.4-4; PB1518: GR-9.1.13.7-5; PB1198 Q2: GR-14.9.51.4-24

## Findings (findings.json)
1. NEW, crashes: GLOBAL bridge duplicated/phantom for redefines classes — a GLOBAL FD with two records (+ any contained program) or a GLOBAL REDEFINES entry fails the backend (CS0102 / CS1061). Row GR-13.18.27.4-1 DIVERGES; contradicts inventory GR-13.18.27.4-3 CONFORMS.
2. Diagnostic-only: unpositioned `ACCEPT/DISPLAY screen-name ON EXCEPTION` gets COBOL0307, not COBOLNET1707.

## CONFORMS rows for the refuter
- SR-13.18.27.3-1

## Refute
Refuter result: 1 CONFORMS row attacked, **1 upheld, 0 overturned** (SR-13.18.27.3-1). See `refute.jsonl`;
probes under `refute-probe/` (admit arms at 85/2002/2014/2023; 77/05/88/FILLER refusals; SCREEN SECTION wholly
COBOLNET1560; COMMUNICATION SECTION unparsed). `final.jsonl` = `adjudicate.jsonl` unchanged;
`record_verdicts.py --dry-run` on final: shape clean, GAP 1664 → 1663.
