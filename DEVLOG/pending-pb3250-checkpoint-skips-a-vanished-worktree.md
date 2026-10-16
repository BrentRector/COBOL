## PB3250 filed: checkpoint.py skips its whole write when a worktree vanishes mid-survey

`checkpoint.py#worktrees` lists linked worktrees and then runs `git status` and `git rev-list` with each worktree as
the subprocess cwd. A worktree removed in between (prune or agent cleanup) raises `NotADirectoryError` and the
supervisor skips that tick's checkpoint write, leaving `checkpoint.json` stale for a handoff. Seen three times in the
2026-10-15 orchestrate log. Filed as kb/Work PB3250 (MINOR, tooling); the fix is to skip a vanished worktree, with a
self-test. No code changed in this entry.
