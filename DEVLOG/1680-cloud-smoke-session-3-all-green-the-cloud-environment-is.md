## Entry 1680 — 2026-09-24 13:31 PDT — Cloud smoke session #3: ALL GREEN — the cloud environment is ready for work

**What.** Smoke #3 (run `cse_01LGcfSB2a3pr4ktMVmh7RK3`, routine now named per run) verified Entry 1679 (kb/Work
PB1521) end to end: `HOME=/root` and the platform KEPT the merged `~/.claude/settings.json`; the user-level
SessionStart shim fired (`hook_spawn_completed … "SessionStart" … "duration_ms":7876,"exit_code":0`) and reported
`git submodule update --init → ok` and `GnuCOBOL corpus fetch → ok`; `specs-private` checked out at `1dc65bb`; the
corpus has 74 files; build 0 warnings (58 s); **Unit `Failed: 0, Passed: 29190`** (3m56s) — the first fully green
cloud Unit run. The setup script was re-pasted into the environment by the orchestrator through Claude in Chrome
(in-page SHA-256 matched the committed file) — the owner does not re-paste.

**One residue, fixed here.** The platform shows setup-script stdout nowhere a session can read, so no session could
verify a `[setup-env]` line. `setup-env.sh` now tees all of its output to `/var/log/cobolsharp-setup-env.log`, which
the snapshot keeps.

**Process note.** The claude.ai sidebar names a run from its PROMPT's opening, and #3's prompt opened by describing
smoke #2 — so later prompts lead with the run's own number and put the history after.

