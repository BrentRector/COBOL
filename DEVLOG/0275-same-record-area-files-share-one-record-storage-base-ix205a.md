## Entry 275 — SAME RECORD AREA: files share one record storage base → IX205A/IX206A

IX205A/206A's last failure was the SAME AREA sub-test: `SAME RECORD FOR IX-FD1, IX-FS1` makes the two files'
01 records share one storage area, so after `READ IX-FS1` the test inspects `IX-FD1R1-F-G-240` and expects
IX-FS1's record there (ISO §12.4.6.4). The clause was parsed and ignored (DEVLOG 265), so each FD had its own
record area — reading IX-FS1 left IX-FD1's area untouched (COMPUTED=IX-FD1, CORRECT=IX-FS1).

Implemented the storage aliasing:
- `FileSymbol.SameRecordAreaLeader` — the representative name of a group of files sharing a record area.
  `SemanticBuilder.VisitIoControlClause` now reads the `sameClause` and, for SAME [RECORD] AREA (not SORT /
  SORT-MERGE, which concern sort work areas), unions the named files into one group via
  `RecordSameRecordAreaGroup` (adopting an existing group's leader so chained clauses coalesce).
- `StorageLayoutComputer` FILE SECTION pass now keys each FD's base on its group leader: the first FD of a
  group claims a fresh base, the rest **reuse it** (their 01 records alias). Ungrouped FDs are unchanged —
  each still gets its own base. Reworked the cumulative `fileBase += currentFdMax` into a `leaderBase` map +
  `nextFreeBase` high-water mark, which is identical to the old behavior when no SAME clause is present (so
  every non-SAME baseline is byte-for-byte unchanged — guard confirms).

Both plain `SAME AREA` and `SAME RECORD AREA` alias the record area (the former is a superset); SQ206A's
plain `SAME AREA` baseline is unaffected. IX205A/206A → CLEAN, baselined. Guard ALL GREEN: 1000 unit / 347
integration / **257 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 27 IX), 0 regressions. Remaining
IX: IX207A (duplicate-alternate-key read 02-timing/order), generic/partial-key START (IX209A/210A/214A/215A),
variable-length indexed records (IX105A), EXTEND tail (IX216A/217A), IX218A OPTIONAL-file isolation.

