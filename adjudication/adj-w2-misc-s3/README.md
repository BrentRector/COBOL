# adj-w2-misc-s3 — lane-3 adjudication (kb/Work PB1522 wave 2)

- **Pinned sha:** `0caa7d54004e81c4e34529d95764cdc1ccec1779` (probe compiler = the pre-built Debug `cobol` of that tree)
- **Inputs:** 10 files, 23 rows (§4.2.10, §4.4, §5.2/§5.5, §8.4.3.10.4, §8.8.4.2.1, §9.3.6, §11.9.5.2, §11.9.9.3, §16.2.1.2, §16.2.2.2)
- **Wall-clock (UTC, `date -u`):** adjudicate 22:18:02 → 22:32:01 (single phase; build skipped per orchestrator, inputs pre-generated) · refute: 22:33 → 22:34:30
- **Shape:** every line passes `record_verdicts.py --dry-run` (23 records; GAP 1664 → 1664 after refute (final.jsonl)).

## Verdict counts (pre-refute)

| verdict | n |
|---|---|
| CONFORMS | 1 (SR-5.5-2) |
| PARTIAL | 3 (GR-4.2.10-2, GR-5.5-3, GR-16.2.1.2-1) |
| DIVERGES | 2 (GR-4.2.10-1, GR-4.2.10-3) |
| NOT-IMPLEMENTED | 2 (GR-16.2.1.2-2, GR-16.2.2.2-1) |
| NEEDS-OWNER-DECISION | 15 |

NEEDS-OWNER-DECISION owners: PB1517 (GR-11.9.5.2-2), PB579 (SR-11.9.9.3-1..6), PB1519 (GR-9.3.6-L5.1..L5.3),
PB1198 (GR-8.4.3.10.4-4, GR-8.8.4.2.1-9), PB468 Q9 (GR-4.4-1, GR-4.4-2), new process finding (FMT-5.2).

## Findings (findings.json)

New: `no-4.2.10-extension-warning-or-register` (silent), `ec-oo-resource-never-raised` (crashes),
`runtime-integer-argument-fraction-truncated` (wrong-answer; sibling of PB617), `catalog-fmt-row-for-notation-clause`
(process). Already owned, re-measured: PB1506 (BASE / FactoryObject), PB298 (FLOAT-DECIMAL clause silent).

## Refute

Refuter attacked 1 CONFORMS row: **SR-5.5-2 OVERTURNED → PARTIAL** (0 of 1 upheld). §8.8.2 rule 5 ('The second
operand shall be an integer operand') is an SR 'integer' site that does not enforce §5.5 2): `BB B-SHIFT-L D` (D PIC
9V9) and `BB B-SHIFT-L 1.5` compile clean and run (count truncated) — ConditionBinder#BindBoolShiftSuffix, owned by
open PB1413. Other sites re-probed hold (SET SR1 2326, SEARCH VARYING 2325, STRING/UNSTRING 1651, GOBACK status 1704).
`final.jsonl` = adjudicate.jsonl with the overturn applied; passes `record_verdicts.py --dry-run` (post-refute:
CONFORMS 0, PARTIAL 4).
