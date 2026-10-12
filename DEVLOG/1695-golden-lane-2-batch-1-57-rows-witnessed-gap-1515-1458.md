## Entry 1695 — 2026-09-24 22:58 PDT — Golden lane #2 batch 1: 57 rows witnessed (GAP 1515 → 1458)

The first landing of the local golden lane #2 (entry 1694): the writer/refuter output for dns-witness and misc-p16…p22
(78 input rows), integrated with `adjudication/golden-lane-1/scratch/integrate.py` and then checked by hand, since
integrate.py keys refuter verdicts by rule-id.

- **Landed:** 34 positive goldens (85 +17, 2002 +12, 2014 +4, 2023 +1) and 32 negatives; 57 witness-only records
  (56 CONFORMS + 1 DOCUMENTED-NON-SUPPORT, +81 test-ref witnesses), **GAP 1515 → 1458**. Per slug: dns-witness 1,
  p16 5, p17 10, p18 8, p19 9, p20 10, p21 7, p22 7.
- **Hand corrections to integrate.py's output:** DOC-A.1-7 records only its upheld ref
  (`conformance:85/l1c40_no_automatic_alignment`); the writer's second ref (the existing
  `2023/l1_sync_occurs_and_no_auto_align`) was never judged by the refuter. The witness is named in
  `docs/CONFORMANCE.md` §7 `Pinned by` (AnnexA1RegisterDriftTests).
- **Split rows (landed on the upheld refs only):** SR-8.3.5-7 on `85/l1c17_colon_separator` —
  `negative/l1c17-refmod-colon-missing` violates §8.4.2.3.3 SR2 (subscript on a non-OCCURS item), not the colon rule;
  SR-8.4.3.9.3-2 on `negative/l1c20-property-of-universal-reference` — `negative/l1c20-property-of-null` is HELD on
  kb/Work/PB1551.
- **Held, not in the tree:** `2002/l1c17_lock_mode_omitted_branches` (GR-12.4.5.9.4-1). Its b) 2. leg observes the
  undetermined implementor-default SHARING mode (PB322 determination A). The refuter ruled that the row must not close
  even with that leg removed, and recording the partial golden as a test-ref would close it. So it waits for PB322.
  `negative/l1c19-null-as-receiving-operand` (SR-8.4.3.7.3-1) is held on PB1551.
- **Overturned, left as drafted for a later fixer:** DOC-A.1-143 (a System.Console name is not a plausible non-COBOL
  program; the 2002 `.out` was copied from the compiler, and a fatal EC must end the run, see PB1549), DOC-A.1-196 (needs
  an 85 golden), SR-11.7.3-2 (OVERRIDE in an interface also breaks SR3), DOC-A.1-132 (does not exercise the
  determination).
- **Not closable (15):** DOC-A.1-106, GR-7.3.17.4-2/-3, DOC-A.1-111, GR-7.3.18.3-2…-5, GR-12.4.5.9.4-2, DOC-A.1-118,
  GR-14.9.24.4-10/-11, GR-9.3.6-L3.2, GR-7.3.19.4-2, GR-14.2.3-7. They stay GAP and need a derivation or a unit test.
- **Gate (L2, this worktree, Normal priority):** Conformance, whole assembly: Passed 8676/8676, and all 66 new goldens
  ran and passed by name in the trx. Unit: 29188/29190 on the first run. The 2 reds were
  ExternalCorpusPopulationDriftTests, and the log names the cause: "EXTERNAL POPULATION ABSENT". This fresh worktree
  had no fetched corpus. After `scripts/fetch-gnucobol-tests.ps1` they pass (3/3). Characterization: 33/33.
- **Writer/refuter claims found false:** none.

