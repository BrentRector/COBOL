## Entry 1733 — 2026-09-27 05:03 PDT — Battery #87: every compiler leg green; 14 differential flips accepted and re-baselined; first WSL leg

Battery #87 ran in an isolated worktree pinned at `46e6c9f31` (trains 58–64, golden lane 2, DOC-rows 1–2, the Sunday
fix lane). The first attempt was stopped: PB1637 (`python3` is the Store alias) meant it ran no citation audit.
**Green:** Conformance 9054/9054 (full), Unit 29491/29491, characterization 33/33, guard witnesses and watchdog, NIST
364 MATCH / 0 REGRESSION, audit CLEAN, guard ALL GREEN, witness-loss / doc-citation / evidence audits 0.
**Code-citation audit: 1 finding**, my bare "SR15" (fixed in 398e20d8a, DEVLOG 1732; the pin predates it).
**GnuCOBOL differential: 1323 cases, 14 per-case flips vs #86**, each attributed and accepted:
- Eight new rejects, each an ISO syntax rule that GnuCOBOL's default dialect relaxes:
  - COBOLNET2404: GLOBAL/EXTERNAL at level 77 (`listings:1055`; `run_misc:1355/1413/1464/1510`). Introduced by
    313229a6b, the data-clause placement table.
  - COBOLNET2406: VALUE in an entry with or under REDEFINES, §13.18.63.3 SR12 (`run_misc:11370`,
    `syn_redefines:359`). Introduced by 29dbd4703.
  - COBOLNET1625: a level-88 VALUE its PICTURE cannot hold (`syn_misc:388`, which GnuCOBOL only warns on).
    Introduced by 29dbd4703.
- Six moved toward agreement:
  - `listings:667`, `syn_copy:409/438/923`: COPY, fixed by 9aa2dbeaa, the one §7.2.2.5 text-word scanner.
  - `syn_misc:4167`: SIGN clause, now AGREE_REJECT, from 761b2f7c1.
  - `run_file:3234`: START RELATIVE. It could not be attributed to a single landing without #86's per-case report.

`tests/external/gnucobol-verdict-baseline.tsv` was rewritten with `--write-baseline` (exactly those 14 rows), and the
re-run prints `=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`. Recorded as a MISS against #86's post-rebaseline bar.
**WSL leg (first battery with one), `--no-build` on the Windows-built bins:** characterization 33/33, Unit
29489/29491. The two failures are `ConflictMarkerDriftTests`: a Windows worktree's `.git` file names a Windows path
that Linux git cannot open. They pass 8/8 under WSL from the main checkout.
**Also:** PB1633 re-derived. §8.4.3.13.4 GR2 gives the address of an OUTERMOST program, and GR4 says NULL plus
EC-PROGRAM-NOT-FOUND when the program is not located. So NULL for a contained program may be conforming. The next
step is to probe the EC. Plan §0 now names #87 as the current battery reference.

