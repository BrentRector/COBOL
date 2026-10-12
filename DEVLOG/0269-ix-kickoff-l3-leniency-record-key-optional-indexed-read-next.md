## Entry 269 — IX kickoff: L3 leniency (RECORD KEY optional) + indexed READ-NEXT no longer holds a live enumerator → 12 IX baselines

Opening the IX (indexed) suite. Two changes unblocked 12 tests; the per-test runtime status-code tail
(declaratives, optional-absent, 43/47/48/49) is the next round.

**1. Leniency L3 — `KEY` omitted from the RECORD KEY / ALTERNATE RECORD KEY clause.** The dominant IX
COMPILE_FAIL (IX101A/103A/104A/201A/203A/216A …) was `RECORD IX-FS1-KEY` — the RECORD KEY clause without
the required `KEY` keyword (ISO §12.4.5.12 — KEY is unbracketed → required; ~0.7% CCVS errata, like L1/L2).
Grammar now parses the permissive superset `recordKeyClause : RECORD KEY? IS? dataReference` and
`alternateKeyClause : ALTERNATE RECORD? KEY? IS? dataReference (WITH? DUPLICATES)?`; the no-KEY form is
accepted in DialectMode.Default and diagnosed under named-strict modes (new CBL3615 error / CBL3616 warning
via `SemanticBuilder.CheckRecordKeyNoiseWord`, mirroring L2's CBL3613/3614). Disambiguation from
`recordDelimiterClause` (and the FD `RECORD CONTAINS/VARYING`) is preserved because DELIMITER/CONTAINS/
VARYING are reserved tokens — `RECORD DELIMITER …` can't satisfy the key clause's dataReference and
back-tracks to the delimiter form. This is the deferred L3 the prior survey flagged: it has masking risk
(a typo'd `RECORD foo` parses as a key clause) so it is dialect-gated, never an unconditional relaxation.

**2. `IndexedFileHandler` READ NEXT no longer holds a live SortedDictionary enumerator.** Once L3 let
IX103A/104A/203A/204A compile, they crashed: `System.InvalidOperationException: Collection was modified
after the enumerator was instantiated` from `SortedSet.Enumerator.MoveNext()`. The handler cached an
`IEnumerator` over `_records` across operations, and any interleaved positioned WRITE/REWRITE/DELETE (the
ordinary DYNAMIC READ-NEXT-with-update pattern) invalidated it. Replaced the enumerator with position-by-
key re-derivation (the same robustness the relative handler got in DEVLOG 251): track `_currentKey` (last
record returned) + `_readNextInclusive` (set by START so the next READ NEXT returns the positioned record,
not the one after) + `_pastEnd`; each READ NEXT scans `_records.Keys` for the smallest key `> _currentKey`
(or `>=` when START-positioned, or smallest overall before the first read) — never a stale iterator.
ReadByKey resets the inclusive/past-end flags so a following READ NEXT continues from the next record;
Close/OPEN reset the position. ReadPrevious was already enumerator-free.

IX104A/IX204A became CLEAN outright; IX103A/203A now run (3 FAIL* each, runtime tail). Baselined 12 IX
(IX101A/102A/104A/111A/113A/117A/118A/120A/121A/201A/202A/204A; in suite/numeric order — INDEXED XXXXX###
share TF### by number but each is self-contained, recreating its file via OPEN OUTPUT). Guard ALL GREEN:
1000 unit / 347 integration / **244 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 14 IX), 0
regressions (IX107A/IX302M unaffected by the handler rewrite). Remaining IX: 12 FAIL* (status-code rules —
USE declarative on a non-at-end exception IX114A/115A/116A, DELETE-not-after-read 43 IX119A, sequential
WRITE out-of-sequence 21 IX112A, REWRITE key rules IX106A/110A, OPTIONAL indexed READ → AT END IX218A,
plus IX103A/105A/109A/203A) and ~13 COMPILE_FAIL (other parse forms IX205A–217A/IX108A/IX401M).

