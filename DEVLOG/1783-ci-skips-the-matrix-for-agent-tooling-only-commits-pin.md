## Entry 1783 — 2026-09-29 14:00 PDT — CI skips the matrix for agent-tooling-only commits; pin claude-skills v1.15.1 (the watchdog alarms on deaths)

**Why the matrix ran on "documentation".** The owner asked why the architecture-review plan (Entry 1781) needed CI.
- Every push to main runs CI, because `ci-gate` is required. But a commit whose paths all match the workflow's
  doc-only list skips the ~11-minute test matrix and finishes in seconds.
- That list was `DEVLOG.md`, `PROMPT.md`, `CLAUDE.md`, `README.md`, `docs/` and `kb/`. Entry 1781's commit also
  touched `.claude/skills/` (the new project skill and four pin lines) and the `tools/claude-skills` submodule pin, so
  it ran the full matrix.
- Neither needs it. No matrix test reads `.claude/`; the only reference is a comment in `VersionMatrixTests.cs`. No
  CI step runs the submodule's scripts. The briefs ARE validated on every push, by the audits job's fleet-practices
  check, which is not behind `run_matrix`. The audits job's own comment already treated "a `kb/` or brief edit" as a
  docs-only push, but the list never included `.claude/`.

**The change.** `IGNORE` in `.github/workflows/build-and-test.yml`'s `changes` job now also matches `\.claude/` and
`tools/claude-skills$` (the pin alone, not any path under it). Its reason line names both.
- Checked against sample paths: a skill file and the pin skip the matrix; `src/`, the workflow itself and a
  look-alike `tools/claude-skills-x/` path still run it.
- This commit changes `.github/`, so it runs the matrix once.

**The pin moves 1.15.0 → 1.15.1.** claude-skills v1.15.1 makes `stall_watch.py` exit when an agent DIES during a
live watch (a journal `failed` record). Before, it only listed the death.
- In wave 73, implementers A, B and D died near the end with no error recorded, and the orchestrator found them only
  by checking.
- Tests: a replay alarms on those three real deaths; a live watch started after them stays quiet; a simulated live
  death alarms.
- The finishers A2, B2 and D2 were dispatched from the dead agents' stamped checkpoints.

The five project skills and PB1699 name 1.15.1, and `check_practices` is GREEN.

