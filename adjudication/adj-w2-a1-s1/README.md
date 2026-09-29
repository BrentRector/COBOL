# adj-w2-a1-s1 — Annex A.1 adjudication, wave 2 (kb/Work PB1522)

- **Pinned sha:** `0caa7d54004e81c4e34529d95764cdc1ccec1779` (0caa7d5)
- **Rows:** 18 (DOC-A.1-114, 116, 117, 118, 119, 120, 121, 122, 124, 126, 130, 131, 132, 138, 139, 141, 142, 143)
- **Probes:** `/tmp/adj/adj-w2-a1-s1/probe/` (ls1, ls2, lst, loc, mn, nx1, nx2, int1, int2, pg, pg3, sh, pl, pa)

## Wall-clock (UTC)

| Phase | Start | End |
|---|---|---|
| Adjudication (18 rows, checkpoints at 5/10/15) | 2026-09-24 22:18:05 | 2026-09-24 22:31:39 |
| Findings + final.jsonl + README | 2026-09-24 22:31:39 | 2026-09-24 22:33 |
| Refute (3 CONFORMS rows) | 2026-09-24 22:33 | 2026-09-24 22:36 |

## Verdict counts (post-refute; no overturns, so `final.jsonl` == `adjudicate.jsonl`)

| Verdict | Count | Rows |
|---|---|---|
| CONFORMS | 3 | 118, 119, 138 |
| PARTIAL | 2 | 126, 139 |
| DIVERGES | 11 | 114, 116, 117, 120, 121, 122, 124, 130, 131, 132, 141 |
| NOT-IMPLEMENTED | 2 | 142, 143 |

`record_verdicts.py --dry-run` over the batch: 0 shape violations; GAP 1664 -> 1661 (+3 closed).

## Refute: 3 of 3 UPHELD, 0 overturned

`refute.jsonl`; probes in `/tmp/adj/adj-w2-a1-s1/refute-probe/` (loc.cob, loc2.cob, ct.cob, pc.cob + pcbody.cpy).

- **DOC-A.1-118 upheld.** The env / culture / root arms of `LocaleState.Determine` are pinned by
  `unit:CobolCollationTests.LocaleState_DeterminesTheDefaultsFromTheEnvironment`, so the adjudicator's caveat
  ("no test pins the fallback") was wrong in the row's favour. Probes: env de/fr honoured, LANG culture fallback
  honoured, LANG=C gives root with rc 0. An unavailable env or host locale raises EC-LOCALE-MISSING, which is
  §8.2.1's own rule.
- **DOC-A.1-119 upheld.** The root default is always present. LC_CTYPE (tr-TR UPPER-CASE dotted I) and LC_TIME
  both work, which is logically-equivalent functionality.
- **DOC-A.1-138 upheld.** The after-text-manipulation caveat is closed by a direct probe. A formal parameter
  reachable only through REPLACE, together with COPY and >>DEFINE/>>IF inside the skeleton, expands correctly
  under 2002, 2014 and 2023.

Overturns: none. `record_verdicts.py --dry-run` on `final.jsonl`: 0 shape violations; GAP 1664 -> 1661.
