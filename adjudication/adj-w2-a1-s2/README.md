# adj-w2-a1-s2 — Annex A.1 adjudication batch (kb/Work PB1522 wave 2)

- Pinned sha: `0caa7d54004e81c4e34529d95764cdc1ccec1779` (read-only tree `/home/user/CobolSharp`, prebuilt Debug compiler)
- Rows: 18 (all DOC-A.1-N), 18 input files from `/tmp/adj/adj-w2-a1-s2/in/`
- Shape: `record_verdicts.py --dry-run` passes on all 18 records

## Wall-clock (UTC)
| Phase | Start | End |
|---|---|---|
| Adjudication (18 rows) | 2026-09-24 22:18:07 | 2026-09-24 22:27:30 |
| Findings + deliver | 2026-09-24 22:27:30 | 2026-09-24 22:29 |
| Refute | 2026-09-24 22:27:40 | 2026-09-24 22:29:40 |

## Verdict counts (final — unchanged by refute)
- DIVERGES 12: 146, 147, 148, 154, 155, 156, 157, 159, 160, 167, 168, 181
- NEEDS-OWNER-DECISION 5: 149, 152, 161, 162, 172
- CONFORMS 1: 169 (test-needed)

## Refute
Refuter result: 1 CONFORMS row attacked (DOC-A.1-169) — UPHELD, 0 overturns.
- DOC-A.1-169 upheld: A.1 item need not be documented (no §7 row owed); extent specified as zero (BindIoControl Format-3 no-op + COBOLNET_FILES_DESIGN.md "SAME SORT-MERGE AREA are no-ops"); independent probe `refute-probe/r1.cob` correct at 85/2002/2014/2023. Side note: adjudicator probe s2.cob violates SORT §14.9.40.3 SR10 (open kb/Work PB1139).
`final.jsonl` = `adjudicate.jsonl` (no overturns); `record_verdicts.py --dry-run` passes (18 records).

## Files
`adjudicate.jsonl` (per-row checkpoint), `final.jsonl`, `findings.json` (11 mechanisms), this README.
