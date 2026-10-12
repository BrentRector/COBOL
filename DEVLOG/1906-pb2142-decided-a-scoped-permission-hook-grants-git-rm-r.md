## Entry 1906 — 2026-10-07 02:20 PDT — PB2142 decided: a scoped permission hook grants `git rm -r` inside a worktree; the owner's never-lose-work rule recorded

**Owner decisions (2026-10-07, ~03:00 PDT).** "A scoped permission rule allows git rm -r under .claude/worktrees/" (PB2142 option 1).
Then: "Work trees should only be removed after we have committed all their work as appropriate" and "I do not want to accidentally
lose useful work."

**The grant is a hook, because a settings rule cannot see the working directory.** `scripts/hooks/worktree_rm_allow.py` (PreToolUse,
Bash and PowerShell) returns `permissionDecision: allow` only for ONE `git [-C <dir>] rm -r …` command — an optional `cd <dir> &&`
prefix and nothing chained after, since an allow covers the whole tool call — run in a directory under `<repo>/.claude/worktrees/`
(derived from the hook's own location), with every path argument inside that worktree and none of `-f`/`--force`. Without `-f`, git
itself refuses to remove a file whose content differs from the commit, so every removal the grant permits is one commit from
recovery. Anything else is no decision: the normal permission flow and the other hooks behave as before, and a deny from
`forbidden_commands.py` still wins. `scripts/hooks/test_worktree_rm_allow.py` runs 24 cases — eight that must be allowed (relative,
`--cached`, `--`, Windows spellings, `-C` from main, the `cd` prefix, an absolute path inside) and sixteen that must not (the main
checkout, the worktrees folder itself, an absolute path outside, `..`, `-f`/`--force`, no `-r`, three chain shapes, `rm -rf`, `-C` and
`cd` out of the worktree, no path, the command inside an echo) plus unparseable input. CI's audits job runs it beside the other hook
self-tests; the local gate's own hook-test step runs the same set.

**The never-lose-work rule** is now MANDATORY-PRACTICES O10 (a worktree with an uncommitted edit, an untracked file or an unlanded
commit is never removed and never reset; a superseded edit is dropped only after the commit that supersedes it is named) and P12
names the grant. `prune_worktrees.py` already archives a verified bundle, skips dirty, locked and recent worktrees and never removes
UNLANDED; the dry run after train 1025 classified wave 1026's four worktrees UNLANDED (in flight), wf_b3526bdd-e0d-4 UNLANDED
(PB1470, the pending finisher) and wf_79ef30e1-cd1-7 CHECK; nothing has been removed. PB2142 is `landed`.

**The first push went RED in CI, on Linux, and main was untouched** (push-main run 37597290463: the audits job's hook self-test
step). The self-test was 24/24 on Windows and 23/24 under WSL: `git -C E:/elsewhere rm -r src/X` from inside a worktree was
ALLOWED on Linux, because `pathlib.Path.is_absolute` answers for the host and Linux reads a drive-letter path as relative, so the
`-C` target resolved under the worktree. Root cause fixed in the hook: a path is absolute when it is absolute on EITHER platform
(a POSIX root or a drive letter), applied to the `-C` target, the `cd` target and every path argument; four more absolute-path
cases join the self-test (28 checks). The CI invariant's lesson (owner 2026-10-04: a CI red is also a defect in the local gates): the hook
self-tests ran locally only on Windows, through `run_gate_legs.py`, while CI runs them on Linux, so `scripts/linux-gate.sh` now
runs the three guard-hook self-tests on its Linux clone as a `hooks` leg before the test legs, and a red there is a red gate. Both
platforms are green on the amended commit.

