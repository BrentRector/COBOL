## Entry 1700 — 2026-09-25 00:55 PDT — Golden lane #2 batch 3: 30 rows witnessed (GAP 1375 → 1345); lane #2 closes at 165 rows

This is the third and last landing of the local golden lane #2 (entries 1694, 1695, 1698): misc-p33, p34, p35, p37 and
p38. misc-p36's 7 rows are all not closable, because §8.5.1.11.3 is permissive, so nothing was recorded for it. The
writer/refuter output went through `adjudication/golden-lane-1/scratch/integrate.py`, and then every record was
checked by hand against its `refute.json`.

- **Landed:** 25 positive goldens (85 +16, 2002 +6, 2014 +1, 2023 +2) and 6 negatives. 30 witness-only CONFORMS
  records (+37 test-ref witnesses). **GAP 1375 → 1345.** Per slug: p33 7, p34 6, p35 7, p37 7, p38 3.
- **Hand check:** every recorded ref was one the refuter judged and upheld. There were no split rows. The two
  overturned goldens are not referenced by any upheld row, so integrate.py's drop-every-overturned-ref hazard
  (batch 2) lost nothing. There are no `C3 82 C2 A7` bytes in the copied files; that is the double-encoded §
  repaired in misc-p38's drafts. The copied files also have no CR and no BOM. PROGRAM-IDs are unique on the merged
  tree.
- **kb/Work/PB963:** GR-7.3.25.4-5 is witnessed by `2002/l1c33_turn_within_statement` and moved from `inventory_rows`
  to `closes_rows`. The note stays open for -6 and -8.
- **GR-13.18.57.4-2 re-verdicted CONFORMS → PARTIAL**, claimed by **kb/Work/PB1569** (wrong answer). A TYPE subject's bit
  group does not get level-1 bit alignment: BYTE-LENGTH is 01 where §13.18.57.4 GR2 d) with §8.5.1.6.3 requires 02. The
  earlier adjudication's claim of "no observable divergence" for d) held only for SYNCHRONIZED slack. The a)–c)
  golden `2002/l1c34_type_group_hierarchy` was overturned and stays out of the tree. It lands with a d) leg once PB1569
  is fixed. DefectiveRowCoverageDriftTests is green with the claim.
- **Overturned (2):** p34 GR-13.18.57.4-2 (above). p37 GR-14.9.51.4-34 (`85/l1c37_indexed_every_key_path`): its
  2nd and 3rd READ NEXT lines are governed by §14.9.30.4 GR21 e), which is the PB1555 arm.
- **Not closable (9):** SR-13.18.57.3-11 (the probe hits the COBOLNET0899 limitation), SR-13.18.58.3-3 (subsumed),
  GR-13.18.58.4-2, GR-14.9.48.4-18 (undefined result), SR-14.9.51.3-11, GR-14.9.51.4-6, GR-14.9.51.4-13 and
  GR-14.9.51.4-16 (needs two run units; see PB707). misc-p36 adds its 7.
- **Rebase:** onto train 60 (DEVLOG 1697) and batch 2 (DEVLOG 1698). The only conflicts were the four edition
  manifests. They were resolved by taking main's manifests and re-appending this batch's names. The verdict batch was
  re-applied to main's inventory with `record_verdicts.py`, giving the same 31 rows. Post-rebase check: build green,
  inventory/drift unit subset 25/25, and all 31 new cases 31/31.
- **Gate (L2, this worktree, Normal priority, before the rebase):** Conformance, whole assembly: Passed 8707/8707. All
  31 new cases ran and passed by name (the DisplayName-filtered run printed 31/31). Characterization: 33/33. Unit:
  29189/29190 on the first run. The one red was `ConflictMarkerDriftTests.TheSweepActuallyReadsTheTrackedTree`, with
  `UnauthorizedAccessException` on an existing negative `.err` while the Conformance leg ran concurrently and held that
  file. On a re-run alone the result was 29190/29190.
- **Lane #2 totals:** 165 rows witnessed (57 + 78 + 30). The GAP went 1515 → 1345; train 60 accounts for the other 5 of
  that 170. The overturned and held rows are listed per slug, with their refute.json paths, in
  `adjudication/golden-lane-1/LANDING.md` ("Golden lane #2 — totals").

