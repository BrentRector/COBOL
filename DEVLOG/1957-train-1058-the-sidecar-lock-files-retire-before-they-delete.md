## Entry 1957 — 2026-10-11 11:00 PDT — Train 1058: the sidecar lock files retire before they delete and hide no holder, the Linux gate puts itself on its host, and four drift tests stop re-running self-tests under a clock

Train 1058 carries two clusters by the owner's direction (2026-10-11, through the operator): PB2917+PB2936, which
train 1056b ejected and a finisher completed, and PB2999, the Linux gate fix. Both bases were `f91823991`, still
origin/main when the lander took the landing lease, so the final rebase was a no-op. Both implementers gated leg 1 only
(batched gating, PB2515), so this train's whole-population gate was each cluster's first population check. The gate
also found one red that was main's, not either cluster's; its fix is the train's third commit.

**A — PB2917 + PB2936: one lock-file primitive under two policies.** On a host without open-file-description locks
(macOS above all), a holder's lock is a file per holder in `.wolk-<hash>/` beside the physical file. PB2917 had three
arms. Testers swept one dead file and read each other as live, because probes were exclusive. An entry no host could
open (a directory, or a mode-000 file) was swept and the OPEN granted. And a lock directory made at umask 022 shut a
second user out. The fix puts the mechanism in one place, `SidecarLockFiles`: atomic pending-then-rename publication,
EXCLUSIVE holders with SHARED probes so testers never exclude each other, and a probe (`HostFile.ProbeLockFile`) that
answers Live, Dead, Gone or Unobservable. An unobservable holder refuses and is never swept (CONFORMANCE DOC-A.1-75).
No name a prober reads is deleted in place. On Windows a file deleted under an open handle is delete-pending and answers
"access denied", the unobservable answer, so `Retire` renames it to `gone-<guid>` first. A lock directory and its files
take the shared directory's permissions, and a file always keeps its owner's own read and write bits. A lock directory
the user may enter but not list is unobservable as a whole. PB2936: a shared RELATIVE or INDEXED file's record locks and
store mutex had no host expression on macOS, so two run units lost updates silently. They are now lock files in the same
directory (`SidecarRegionLocks`), behind a per-connector `RegionLocks` endpoint (`HandleRegionLocks` for Windows
`LockFileEx` and Linux OFD). `RegionLocksDriftTests` holds both hosts to one contract on every machine, and the
cross-run-unit scenarios run on both hosts (`HostRegionLocks.Forcing`). The finisher fixed a cause the note had not
named: `Publish` read a pending file that a tester reaped mid-publication as "the host takes no lock", which answered
`Unavailable`. It now retries. That was the `[Unavailable, Held]` red of
`SidecarColumnLockHostDriftTests.TwoSimultaneousExclusiveTakes_NeverBothHold` on Linux; with A in, the train's Linux
unit leg passed it. The finisher merged main into the branch. The lander checked that the merged docs keep main's text:
CONFORMANCE changes only row DOC-A.1-75 (row 68 is main's), DESIGN-runtime-library keeps main's CommitGate paragraph
(PB2848), and DRIFT_RULES gains only `RegionLocksDriftTests`. No inventory row is claimed or closed (`closes_rows: []`
with reasons). No goldens: this is a runtime host mechanism with no COBOL-visible syntax.

**B — PB2999: the Linux gate puts itself on its host.** From Git Bash, the Bash tool every session has, `bash
scripts/linux-gate.sh` used to run on Windows: it built with the Windows SDK and printed a Linux verdict about no Linux
leg. `run_on_linux` now hands the script to WSL in the same tree with the same arguments, and exits with that run's
status. It uses `--exec`, because after `--` wsl.exe re-parses the command line and every argument was lost, and
`MSYS_NO_PATHCONV=1`. A host with no WSL is NOT RUN. An unknown `--legs` name is NOT RUN before anything is cloned.
The briefs, MANDATORY-PRACTICES I8/L10, the gate skill and DESIGN-test-build-ci now name the one bare command. The
lander ran B's `--self-test` from Git Bash (9 checks GREEN) and ran the second Linux gate through the bare command from
Git Bash, which reported the hand-off and returned the WSL run's GREEN.

**PB2882 (in the train): four drift tests hold self-test arms from source, not by re-running them.** Gate 1 (run
`20261011T172547Z-c2678c`, head `4d59c6ac6`) was RED in Unit on one case:
`SelfTestDiscoveryDriftTests.TheRunner_ProvesEveryArm`, a harness non-observation. `self_tests.py --self-test` timed
out at the harness's fixed 120 s budget twice while the whole-population leg ran beside the concurrent WSL Linux gate.
In the same run, `GateLegDriftTests`' driver test (311 s) and `LandingLeaseDriftTests` (276 s) each timed out once and
recovered. Neither cluster touches the test, the script or the harness, and the self-test is ALL GREEN in 19 s alone on
the same head. So the red was main's. PB2882 had named exactly these tests as the unfixed siblings of the GateSlot red
that train 1055 fixed. A fixed time limit in a test is a defect, so the train fixes it with that train's shape, made one
mechanism. `SelfTestArmSource` reads a script's string literals, with implicitly concatenated ones joined, and reports
each required arm label it no longer declares. The four tests now hold their arms through it and run nothing; the
runner still runs all four self-tests in every gate and CI. A planted-input test sees the reader report a deleted arm
and join a split f-string label. The rule is DESIGN-test-build-ci §3.14.10. PB2882 stays open for its long poles and the
remaining re-runs, which this run measured well under the budget (the largest, `ImpactedTestsDriftTests`, 88 s).

**The train.** `train_apply.py` brought both clusters in without a conflict (A 24 paths, B 9, every one as on its
branch), and the code-citation, doc-citation and work-register audits passed. Windows lander gate 2 (run
`20261011T174354Z-1b0990`, head `bb79e17a3`): `GREEN — Conformance 11,460/11,460 · Unit 33,405/33,405 ·
Characterization 36/36`, every population EXACT, SELF-TESTS GREEN (74 ran). Linux gate on the same head: `GREEN (legs
selftests unit characterization conformance guard selftests-built)`, after gate 1's Linux run on `4d59c6ac6` was also
GREEN. Semgrep: no count moved. Arch-oracle: IDENTICAL across 7,761 cases against `7c00d5d5d729`. A's runtime files are
inputs of that baseline, so it is re-recorded as `bb79e17a3fb8`. The lander's review read the lock-file primitive, both
policies, the probe, the connectors' endpoint lifetimes and B's hand-off, and found no correctness defect. It filed one
minor defect, PB3000 (open, reproduced): run from a subdirectory on Windows, the hand-off gives WSL's bash a relative
script path that does not exist there, so it exits 127 with no verdict line, where Linux prints NOT RUN. That is a
missing verdict, never a pass, and the documented command runs at the tree root. No inventory verdicts, so the GAP is
unchanged. No diagnostic codes. Nothing was dropped or ejected.

