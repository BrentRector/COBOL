## Entry 1857 — 2026-10-03 21:40 PDT — `prune_worktrees.py` ignores the always-dirty `.claude/settings.local.json`; 35 stale worktrees pruned

Every worktree carries a modified `.claude/settings.local.json` (the harness rewrites it), and the dirty check in
`scripts/prune_worktrees.py` counted it, so `--apply` skipped every LANDED and MERGED worktree as "dirty" and the stale ones piled up
(38 at the start of this session). The check now excludes that one path (`git status --short -- . :!.claude/settings.local.json`),
so a worktree with real uncommitted changes is still skipped. `--apply --include-check` then archived 28 unmerged tips in a
verified bundle under `E:\Temp` and removed 35 branches with their worktrees; every one was classified LANDED (its content is on
`main` under rewritten hashes), so the bundle was deleted the same evening and nothing reads or processes it. Left on purpose: the
UNLANDED `wf_69250a68-60b-2` (PB1527, the string-pointer crash fix) and `wf_a08557c7-e3e-4` with `t1007-ckpt` and `t1007-try1`
(PB1402 group D, dropped from train 1007 on a Windows CI red). `wf_5aceadb5-9cf-4` was a CHECK and was removed because PB1705 and
PB1660 landed as `27d579574`.

**Files:** `scripts/prune_worktrees.py`. No diagnostic code used.

