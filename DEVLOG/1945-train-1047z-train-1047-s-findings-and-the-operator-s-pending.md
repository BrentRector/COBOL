## Entry 1945 — 2026-10-08 22:12 PDT — Train 1047z: train 1047's findings and the operator's pending notes (notes only)

**Cluster 1047Z (branch `notes-cleanup-capdesign`, head `21568f08a`, base `495e8fb6f`).** No compiler source. The
cluster files the eight review findings the train 1047 lander had no ids for, as open notes: PB2844 (a constant's
`LENGTH OF M OF V` through a TYPE-expanded group is refused with COBOLNET1547 although §13.18.57.4 2) a) gives V the
subordinate M; MAJOR, rejects legal source), PB2845 (`CobolNameFoldDriftTests` scans no runtime project and misses
`FlagDirective.cs:128`'s host `ToUpperInvariant` key), PB2846 (the emitted main calls `EnsureRegistered` outside
`RunMain`'s try, `ProgramEmitter.cs:871`), PB2847 (SET … TO ENTRY and ADDRESS OF FUNCTION drop the not-located
reason, and `ProgramTable._probeFailure` keys by name only), PB2848 (four I/O status findings from cluster 47B),
PB2849 (the PB1951 table-SORT goldens never DISPLAY the capacity), PB2850 (`CanonicalClosureDriftTests` lists its
CLDR locales by hand and misses sr, mk, cu and ko) and PB2855 (a bare `plan_wave.py --dry-run` reads no reports).
It files the operator's pending notes: PB2851 (a land unit removed a worktree the survey had flagged with an
uncommitted path; removal must refuse it in code; MAJOR), PB2852 (owner 2026-10-08: every context-limit or turn-cap
hit is a design failure; five land-unit soft-cap hits and two lander turn caps that day; MAJOR analysis) and PB2853
(the golden-lane-1 templates are stale). PB2708 gains a recurrence and two rules (cleanup is a unit's last step;
remote branches are never deleted), and PB2426 gains the 21:46 resume unit that the hand-reverted trend file caused.
The trend file carries train 1047's two points (inventory `f6e4ad94`, GAP 103; program `495e8fb6`). The
session-start skill now names the probe's mailbox line and what to do on its stop sign (PB2814's one factual gap).
The branch contains `note-t1047-carry` (720e2f537 and 62af2c59b are its ancestors; 95cb35c9a was cherry-picked as
dfd9e8898), so that branch is not carried separately. PB2854 was allocated and is unused.

**What the lander re-verified.** `work.py check` passes (2,344 items) and every new note is `status: open` with
`inventory_rows: []` and a `closes_rows_reason`, so no row is claimed or closed. No note duplicates an existing
one: the nearest neighbours (PB1941 and PB2465 for PB2844, PB1965 for PB2845, PB2097 for PB2846 and PB2847, PB2765
for PB2850, PB2708 for PB2851) are the clusters each note names, and none records the same mechanism. Each cited
clause passed `cite.py --check`: §13.18.57.4 2) a), §13.10.3 10), §8.1.3.2 3) and §9.1.13. The code sites the notes
name match the tree (`FlagDirective.cs:128`, `ProgramEmitter.cs:871`, `PtrEmitter.cs:250/294`, `ProgramTable.cs:74`,
the drift tests' project and locale lists), and so do the skill's claims (`mailbox.py`'s `NO watcher armed` line and
its `list --inbox`). The trend points are the generator's output, not hand edits: `gen_ledger.py`'s own
`trend_series` and `program_series`, run on the train tree, add no point to the file and agree with its last two.

**The train.** One cluster. The manifest named the branch at `dfd9e8898`; the implementer's DONE report, written
after the branch's last commit, names `21568f08a` (the session-start skill commit), so the train carries the
branch head. Gate `build-local -Mode lander`: GREEN, Conformance 11,319/11,319 · Unit 33,027/33,027 ·
Characterization 36/36, first run. The Linux gate is green, the oracle is IDENTICAL (7,689 cases), and semgrep
counts are unchanged. GAP 103 → 103. The review found nothing: 0 findings, 0 fixed in the train, 0 clusters dropped.

