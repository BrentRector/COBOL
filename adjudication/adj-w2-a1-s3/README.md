# adj-w2-a1-s3 — Annex A.1 DOC rows, wave 2 (18 rows)

- Pinned sha: `0caa7d54004e81c4e34529d95764cdc1ccec1779`
- Adjudication phase: 2026-09-24 22:18:09 UTC → 22:32:47 UTC (~15 min wall clock, 18 rows)
- Refute phase: ~22:33 UTC → 22:37 UTC (~4 min, 3 CONFORMS rows attacked). **Result: 3 of 3 upheld, 0 overturns.** One correction without a verdict change: DOC-A.1-194's test-ref `conformance:2023/pb164_index_group_image` never subscripts with the index (it pins item 211's image), so final.jsonl uses `conformance:85/pb215_index_windows` (E(IX + 1) -> occurrence 4). DOC-A.1-201 residual: the after-text-manipulation ordering is witnessed only by refute probes r201a/r201b, not by a committed golden.
- Record shape: all 18 lines pass `scripts/spec/record_verdicts.py --dry-run` (SHAPE-ADDENDUM rules)

## Verdict counts (final = pre-refute; no overturns)
| Verdict | n |
|---|---|
| DIVERGES | 6 (186, 203, 214, 217, 218, 219) |
| PARTIAL | 6 (182, 187, 192, 193, 196, 198) |
| CONFORMS | 3 (194, 201, 220) |
| NEEDS-OWNER-DECISION | 3 (199, 200, 212) |

## Files
- `adjudicate.jsonl` — one record per row, checkpointed as each was decided
- `final.jsonl` — adjudicate.jsonl with the refute applied (no overturns; DOC-A.1-194 test-ref corrected, refuter notes appended to 194 and 201); passes record_verdicts.py --dry-run
- `findings.json` — defects clustered by mechanism, with owning kb/Work notes
- `refute.jsonl` — the refuter's per-row results (194, 201, 220 upheld); probes under `/tmp/adj/adj-w2-a1-s3/refute-probe/` (r194, r201a/b/s, r220)

## New defects without an owning note
- DOC-A.1-203: THROUGH range collating sequence (= PROGRAM COLLATING SEQUENCE) undocumented (PB372 recorded it only as a gap).
- DOC-A.1-214: MOVE gives a STRONG group's object-reference leaf an 8-space image, contradicting the §7 cell's "no character positions"; STRING of the same group compiles and aborts at run time (the STRING part is a sibling of PB244/PB1116).
