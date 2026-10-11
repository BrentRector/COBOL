## Entry 1706 — 2026-09-25 05:19 PDT — Register: PB1585; practices I1 (+VersionMatrix for new rejections) and P4 (no --autostash)

Register and practice checkpoint after trains 63 (PB1548, standard class BASE) and 64 (gate evidence) landed.

- **PB1585** — `scripts/push-main.sh` reports RED on a green main when a GitHub API read fails (a TLS timeout left
  `MAIN_CONCLUSION` empty; the script took the RED branch): an empty conclusion must be "no verdict", retried, never red.
- **MANDATORY-PRACTICES I1** — a change that REJECTS source it used to accept also runs `~VersionMatrix` at the
  implementer's gate: train 63's lander found 15 constructs.json samples (five constructs × three editions) that NEWed a
  class without BASE, which no implementer term had run.
- **MANDATORY-PRACTICES P4** — `git rebase --autostash` / `git pull --autostash` stash implicitly and are forbidden like
  `git stash` (the stash is shared across worktrees); train 64's lander used it once, harmlessly.

