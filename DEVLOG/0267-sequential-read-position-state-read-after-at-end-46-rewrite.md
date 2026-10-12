## Entry 267 — Sequential read-position state: READ-after-at-end '46' + REWRITE-no-read '43' → SQ133A/136A/144A

The REWRITE/READ-after-AT-END cluster — `SequentialFileHandler` did not track the file position indicator
across operations, so a READ or REWRITE following an at-end READ returned the wrong status:
- **READ after an unsuccessful READ → '46' (ISO §14.9.30 GR21).** Once a sequential READ fails (at-end '10'
  or error) with no reposition, the next READ is itself unsuccessful — no valid next record — status 46
  (SQ136A got '10' again). Added `_lastReadUnsuccessful`, set when `ReadNext` returns non-success; a
  subsequent `ReadNext` short-circuits to '46'. (The read body moved to `ReadNextCore`; `ReadNext` now
  wraps it with the 46 gate + state update.)
- **Sequential REWRITE not immediately after a successful READ → '43' (ISO §14.9.35 GR5).** SQ133A reads to
  at-end then REWRITEs, expecting 43 (the previous I-O wasn't a successful READ) — it got '00'. Added
  `_prevOpWasSuccessfulRead` (true only right after a successful READ; the REWRITE itself clears it so a
  second REWRITE without an intervening READ also fails); `Rewrite` returns 43 when it is false, before the
  GR16 length check.
- Both flags reset at OPEN (the file position is re-established). SQ144A's "declarative not executed" then
  resolves for free — the REWRITE now returns the exception status 43, which the REWRITE USE declarative
  (DEVLOG 266) fires on.

SQ133A 15/15, SQ136A 1/1, SQ144A 1/1, all baselined. Guard ALL GREEN: 1000 unit / 347 integration /
**229 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 56 SQ + 19 RL + 2 IX), 0 regressions — the 46-on-reread and
43-on-no-prior-read changes touch every sequential READ/REWRITE; no baselined test re-reads past at-end or
REWRITEs without a prior READ except these. Remaining SQ FAIL*: the OPEN-absent/OPTIONAL cluster
(SQ141A/142A/203A).

