# adj-w2-14-s1 — RECEIVE (§14.9.31) + SEND (§14.9.38), 22 rows

- Pinned sha: 0caa7d54004e81c4e34529d95764cdc1ccec1779
- Phase adjudicate: start 2026-09-24 22:18:01 UTC (setup/read) — rows written Thu Sep 24 22:20:07 UTC 2026 — end Thu Sep 24 22:20:28 UTC 2026
- Phase refute: pending (orchestrator dispatches an independent refuter)
- Verdict counts (pre-refute): NEEDS-OWNER-DECISION 22 (CONFORMS 0, PARTIAL 0, DIVERGES 0, NOT-IMPLEMENTED 0)
- Shape: record_verdicts.py --dry-run passes (22 records, GAP 1664 -> 1664)
- Reason: Annex A.3 4) MCS facility declined (CONFORMANCE §2 row 4 / §4 item 1), compiler warns COBOLNET1578 and binds BoundNop
  (probed 85/2002/2014/2023); no derived-verdicts selector covers it -> owner question kb/Work PB1198 (Question 1 names all 22 rows).
- Findings (all already filed): PB1198/PB374 (no selector; no RECEIVE-site 1578 witness), PB374 (85 recognition gate), PB937 (merged SEND formats).
- Overturns: refute pending.
