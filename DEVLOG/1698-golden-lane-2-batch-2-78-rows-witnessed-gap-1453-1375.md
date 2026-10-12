## Entry 1698 — 2026-09-25 00:15 PDT — Golden lane #2 batch 2: 78 rows witnessed (GAP 1453 → 1375)

The second landing of the local golden lane #2: the writer/refuter output for misc-p23…p32 (104 input rows),
integrated with `adjudication/golden-lane-1/scratch/integrate.py` and then checked row by row against each
`<slug>.refute.json`.

- **Landed:** 44 positive goldens (85 +28, 2002 +14, 2014 +2, plus the copybook `2002/l1c24ca.cpy`) and 26 negatives;
  78 witness-only CONFORMS records (+90 test-ref witnesses), **GAP 1453 → 1375**. Per slug: p23 9, p24 10, p25 8,
  p26 11, p27 7, p28 3, p29 5, p30 8, p31 8, p32 9. GR-12.3.7.4-6 is recorded on the existing
  `l1c01_native_ordinal_positions` goldens. Every recorded ref was checked to be one that an UPHELD refuter verdict for
  that row judged (0 unjudged, the batch-1 hazard).
- **A second integrate.py hazard, found and worked around:** it drops every ref named by ANY overturned verdict. So a
  golden overturned for one row as "does not exercise" is also lost for the sibling rows the refuter upheld. The fix
  was to run it on a lane copy with those two verdicts' `test_ref` blanked. The verdicts stay overturned. The two
  cases: `85/l1c26_relational_operator_spellings` lands for SR-8.7.5.2-3/-5/-7/-8/-10, where SR-8.7.5.2-1 is
  definitional and its witness is PB1034's future negative. `85/l1c28_same_area_mixed_org` lands for SR-12.4.6.4.3-4
  only, since GR-12.4.6.4.4-1 cannot be observed and stays GAP. The hazard is recorded in LANDING.md.
- **Overturned, drafts left in the lane directory for a fixer:** GR-8.4.3.13.4-3 (non-conforming source, because the
  REPOSITORY names programs defined later, §12.3.8.3 SR14; the positive and both negatives stay out), SR-8.4.2.2.1-5,
  FMT-14.9.35.2 (header citation), GR-14.9.35.4-8, GR-14.9.35.4-14 (fatal-status legs, PB322 E), GR-8.4.3.15.4-5,
  SR-13.15.3-4 (header provenance).
- **Held:** `85/l1c31_string_delimited_omitted` (SR-14.9.43.3-9, PB1567: the edition is undetermined). **Unrefuted:**
  SR-8.8.4.3.3-1. The p31 refuter returned no verdict for `negative/l1c31-simple-boolean-condition-length-8`, so that
  row does not land.
- **Suspected defects (not recorded):** SR-12.3.7.3-L7.5 and GR-12.3.7.4-L2.2 (PB1557, PB1558).
- **Not closable (15):** SR-8.4.3.13.3-4, GR-14.9.35.4-15, SR-13.8.3-1, GR-13.18.49.4-4, DOC-A.1-169,
  GR-12.4.6.4.4-5, GR-8.4.3.8.4-1/-3, GR-14.9.40.4-4, GR-12.3.5.4-2/-3, GR-14.9.43.4-10, SR-8.4.2.3.3-7, plus
  GR-12.4.6.4.4-1 and SR-8.7.5.2-1 from the refuters.
- `docs/CONFORMANCE.md` §7 `Pinned by` now names the witnesses for DOC-A.1-148, -182, -186 and -198. The A.1 audit is
  unchanged at 125 of 184.
- **Gate (L2, Normal priority, external corpus fetched):** Conformance whole assembly passed 8746 of 8746, 0 skipped.
  Unit passed 29190 of 29190. Characterization passed 33 of 33. All 70 new `.cob` cases were discovered by name.

