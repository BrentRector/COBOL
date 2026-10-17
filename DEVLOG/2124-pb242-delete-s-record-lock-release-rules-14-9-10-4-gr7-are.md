## Entry 2124 — 2026-10-16 17:12 PDT — PB242: DELETE's record-lock release rules (§14.9.10.4 GR7) are now observed end to end

kb/Work PB242 recorded that DELETE's GR7 record-lock release rules had only ever been read in the code. No test drove a DELETE through `FileRegistry.DeleteShared` while the deleting connector held a lock, so neither arm of the single/multiple locking dispatch had been observed. A refactor that reordered the releases or gated a) 2. on success would have passed every gate.

**Re-probe.** On this branch's build the actions are right. The a) 2. release runs first, through the shared `ReleasePriorRecordLocks`, which returns at once for multiple locking. The GR6 conflict check comes next. A successful DELETE then releases the deleted record's own lock through `PhysicalFileTable.ReleaseSingle` (a) 1. and b)). No compiler or runtime code changed. `cite.py --check` passed for §14.9.10.4 GR7's lead-in, a) 1., a) 2. and b), and for GR2, GR3, GR4, GR5 and GR6 a). It also passed for §12.4.5.9.4 GR4 to GR7, §14.9.30.4 GR9, §9.1.13.8 1), §9.1.16 and §14.9.51.4 GR32 and GR36.

**Goldens (2002; every expected line derived from the rule).**
- `pb242_delete_record_lock_release` uses a relative file with random access under MANUAL, and an observer connector that never locks.
  - Single locking: the deleted record's own lock is released at completion. A lock on another record is released by a successful DELETE, by one that fails INVALID KEY, and by one that fails the GR6 conflict (51). The last two show that a) 2. is not success-gated and runs at the beginning.
  - Multiple locking: the deleted record's lock is released and the other lock is kept. A failed DELETE releases nothing.
- `pb242_delete_lock_release_indexed` checks INDEXED under AUTOMATIC. The lock a READ set is released under the record identity the DELETE computes, both by prime key (GR3) and by the last record read under sequential access (GR2).
- The negative `pb242-delete-lock-below-2002` is refused at 85 with three COBOLNET0900 errors (SHARING, LOCK MODE, WITH LOCK). Its `.err` was missing from the first cut. The killed wave-1100 session's gate went RED on it (FileNotFoundException), and wave 1101 added the file. No other enabled negative lacks its `.err`.

**Mutation check.** Removing the a) 2. call, the completion release and the multiple-locking early return turned 8 of the relative golden's 37 lines red.

**Rows.** GR-14.9.10.4-7 went from PARTIAL to CONFORMS (batch `w1100f-pb242-delete-gr7-observed`, GAP 68 → 67). PB242 is landed.

**Self-review.** No code changed, so the architecture, performance and duplication lenses had nothing to judge. For the goldens, every PROGRAM-ID is unique, every scenario's expected value comes from the cited rule, and the observer never locks, so a 51 can only come from the connector under test.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run …` (run 20261016T233445Z-66d9cd: Conformance 6,120/11,891, Unit 33,428/33,884, Characterization 5/36; the two goldens and the negative passed in leg 1). The Linux gate is the landing's (batched gating).

