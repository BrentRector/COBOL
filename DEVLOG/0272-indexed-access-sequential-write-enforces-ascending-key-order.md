## Entry 272 — Indexed ACCESS SEQUENTIAL WRITE enforces ascending key order (status 21) → IX109A/IX112A

IX109A/IX112A create an indexed file with ACCESS SEQUENTIAL, then deliberately WRITE a record whose primary
key is not greater than the previous one (e.g. key 000000049 after higher keys), expecting status 21 — the
invalid-key condition for a sequential WRITE (ISO §14.9.51 GR: in sequential access, records are released in
ascending primary-key order). `IndexedFileHandler.Write` checked only for a duplicate key (22), so the
out-of-order WRITE wrongly succeeded (00).

Added `_lastWrittenKey` (the last successfully written key, reset at OPEN). In ACCESS SEQUENTIAL, a WRITE
whose key is ≤ `_lastWrittenKey` now returns 21, checked before the duplicate-key test (an equal key is also
out-of-sequence in sequential access). RANDOM/DYNAMIC WRITE keeps its order-free duplicate-key (22) behavior.

Both tests OPEN OUTPUT their own file (self-contained), so they baseline cleanly in numeric order. IX109A
12/12, IX112A → CLEAN, baselined. Guard ALL GREEN: 1000 unit / 347 integration / **253 NIST** (94 NC + 42 IF
+ 12 SM + 4 IC + 59 SQ + 19 RL + 23 IX), 0 regressions. Remaining IX FAIL*: IX105A (variable-length indexed
records — `RECORD CONTAINS 56 TO 100`, the handler stores fixed-length and loses per-record length) and
IX218A (SELECT OPTIONAL absent-file READ → 10, blocked by shared-TF isolation). IX COMPILE_FAIL: IX108A,
IX205A–217A (other parse forms), IX401M (flagging).

