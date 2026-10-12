## Entry 1777 — 2026-09-29 10:21 PDT — PB1732 root fix: the Linux gate exported GIT_DIR and corrupted the shared git config; it now tests a Linux clone and exports nothing

**What happened.**
- Wave 72's group G (PB1719) ran `scripts/linux-gate.sh` in its worktree, as MANDATORY-PRACTICES I8 now requires.
- The script EXPORTED `GIT_DIR` and `GIT_WORK_TREE` (Entry 1774's design), so Linux git could read a Windows
  worktree whose `.git` file names an `E:/` gitdir. The variables reached every test process.
- `gate_slot.py`'s self-test (reached through `GateSlotDriftTests` in the unit leg) runs `git init`, an empty commit
  and `git worktree add` in its own temp repositories. All three were redirected into the REAL repository:
  - `core.worktree = /mnt/e/COBOL/.claude/worktrees/wf_10d5c11f-c83-1` landed in the SHARED `E:\COBOL\.git\config`;
  - an empty "self-test" commit landed on G's branch;
  - a /tmp worktree was registered.
- Every git command in every checkout then failed: "fatal: Invalid path '/mnt'".
- G reported it at once. It undid the commit (`git reset --keep`) and pruned the worktree. The auto-mode classifier
  denied its `git config --unset` on the shared config, and the orchestrator did not route around that denial: the
  owner removed the line himself (`git -C C:/ config --file E:/COBOL/.git/config --unset core.worktree`).

**Root fix: test a Linux clone, export nothing.**
- `linux-gate.sh` now clones the tree's COMMITTED HEAD into `~/linux-gate/<tree>`. The clone is `--shared`, borrowing
  the Windows object store read-only. Each leg's project is built there with the Linux SDK and tested there, the
  shape of CI's ubuntu jobs. The GnuCOBOL corpus is copied from the tree, or fetched with CI's own script.
- The script's only git calls on the Windows repository are two reads (HEAD, and the uncommitted-change count), with
  `safe.directory` and `core.autocrlf` passed inline.
- This supersedes both of Entry 1774's designs: Windows binaries run `--no-build` (363 false reds in Conformance
  through `[CallerFilePath]`), and the exported git variables.

**Measured at `4fdc6897c`** (train 71b with F's red), all three legs in 195 s:
- unit 75 s, with ONE red, `GateLegDriftTests.Arm5` (exactly CI's);
- characterization 8 s;
- conformance 102 s, 9,321/9,321.
- The shared `.git/config` had no `worktree =` line afterwards, and no /tmp or /mnt worktree was registered.
- A first run at F's older head failed NOT RUN-style (that branch predates `test_population.py`); a detached
  worktree at the lander's commit was the right subject.

**Updated:** DESIGN-test-build-ci.md section 3.15 (records both failed designs), PB1732, DOC_INDEX,
MANDATORY-PRACTICES I8, the fix-lane implementer brief 7b, and the lander and lander-train briefs 3b. The briefs now
say to COMMIT before the gate, because it tests committed HEAD. `check_practices` is GREEN, and the caller audit is
CLEAN with 13 sites.

**The sibling guard stays with G.** It is making `gate_slot.py`'s self-test scrub the repository-selection
variables, so a test that builds scratch repositories is hermetic whatever its caller exports.

