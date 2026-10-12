## Entry 1444 — 2026-09-03 10:26 PDT — Battery #42 closes the campaign's first day: every test leg green, seven differential flips all one refusal code, and a red the tracked tree never carried

Resumed from the second pause on the owner's "pick up from where you left off — continue to minimize token use".
Main `80b4f1e6`, clean, CI green on it and on the morning's scheduled run. The register got its first note before
anything else, as the pause handoff ordered: `PB377`, the Linux-only
`CollationKeyCacheTests.ConcurrentCallers_GetOneKeyPerText_AndNeverAWrongOne` transient (3 of 8 unit runs on
2026-09-02/03), flagged provisionally as a wrong-answer defect because the invariant under test is exactly "never a
key built for another text"; one fresh Opus implementer took it into a worktree from a brief on disk.

Battery #42 (owed since the golden lane `c11949e3`, twenty landings back) ran on `80b4f1e6`: Conformance 5515/5515,
Unit 5293/5293, characterization 33/33, NIST 353 MATCH / 0 REGRESSION with a clean audit, ALL GREEN guard — and
`=== BATTERY: NOT GREEN (rc=1) ===` on two legs, both attributed here rather than re-run:

- GnuCOBOL differential: 1323 cases, **7 per-case flips, every one `COBOLNET1560`** — the named
  DOCUMENTED-NON-SUPPORT refusal of the declined SCREEN SECTION / CRT STATUS surface that the A.4.2 screen-witness
  landing `60bf02d7` introduced. Three AGREE_ACCEPT → WE_REJECT_THEY_ACCEPT (`syn_definition:1485`, `syn_screen:221`,
  `syn_screen:592`) and four WE_ACCEPT_THEY_REJECT → AGREE_REJECT (`syn_misc:1202`, `syn_screen:371/506/616`);
  totals 575/471/209/68 → 578/468/213/64, exactly lander 2's rebaseline plus those seven. The baseline TSV was
  rewritten from the run's own `gnucobol-report.json` (not from a second run) and its diff is those seven rows and
  nothing else.
- Citations: **23 findings, all `[PHANTOM]`, all in `kb/Reference/**`** — gitignored output of
  `gen-vault-reference.ps1`, last generated locally on 2026-07-26, before the citation-repair sweep (`9c08e6fe`)
  took those clause numbers out of the XML docs it renders. Regenerating gave 0 findings with no tracked file
  touched. A gate whose population includes a derived, untracked view manufactures reds that CI never sees —
  registered AND FIXED as `PB378`: `citation_corpus` now admits only `git ls-files --cached --others --exclude-standard` (retiring the hand-kept `BUILD_OUTPUT` directory set that had answered the same shape once for `obj/antlr-lib`; failure branch proved with planted phantoms), so a red is a property of the
  commit and not of the machine.

Also in this commit: plan §0's CURRENT battery reference is #42 (#41 demoted to PRIOR), the evening handoff bullet
marks ① done, the ledger's in-flight narrative reads the morning's state, and `kb/Conformance` was regenerated (the
probe's staleness warning). Two audits the battery does not run were read too: semgrep's drift (4 → 36, 423 → 439) is `PB175`'s and this commit moves it neither way; `audit_doc_citations.py` reports six MISFILED citations in tracked files, two in `src/`, that no note owned — registered as `PB379`, whose fix gives that audit a battery seat. Lane 3 batch 2 resumed from its `out-b2` checkpoints (13 of 20 subjects adjudicated;
7 to adjudicate, then all 20 refuted four at a time on the `5fe593a0` pin) and golden-lane round 2 resumed for its
one undrafted file (`misc-p1`); both read-only, both started only after the battery closed, per the workstream
doctrine.

