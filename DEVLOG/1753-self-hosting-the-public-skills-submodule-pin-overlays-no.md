## Entry 1753 — 2026-09-28 11:13 PDT — Self-hosting the public skills: submodule pin, overlays, no project copies of the fleet scripts (PB1699)

**What changed.** The project now consumes `BrentRector/claude-skills` instead of keeping parallel copies of it (kb/Work
PB1699, now `half`):
- `tools/claude-skills` is a git submodule pinned at tag v1.8.1.
- `.claude/settings.json` declares it as a directory marketplace and enables `brent-tools`.
- The four overlays (`workstream`, `gate`, `review`, `spec-lookup`) invoke their base skill first.
- `scripts/spec/{orient,fix_clusters,status_delta}.py` are deleted. Every caller names the submodule path directly.
  The owner said "we should ideally not have wrapper scripts" mid-task, and CLAUDE.md rule 4 now says so too.
- The project settings for those scripts live once, in `.agent-fleet.json`.

**The public side came first (v1.8.0, v1.8.1).** The two copies had diverged in both directions. The public repo is
now the superset:
- **`orient.py`**
  - LEARNED sorts newest-first by the note id's number. The public copy's string sort had ranked PB62 above PB1534.
  - C# outline rules: PascalCase members, `record struct`, and statement lines skipped.
  - `.g4` rules, fragments and modes in the outline.
  - `--cite` with a `key` group counts per clause and lists that clause's rules.
- **`fix_clusters.py`**
  - A singleton may absorb another singleton.
  - A code site counts only when it resolves to a real file on a directory boundary.
  - `--src` accepts globs and `--exclude-dir` skips directories.
  - `--json` carries `area` and `files`.
  - A malformed `--harm` is an argparse error instead of a traceback.
- **`fleet_config.py`** reads the optional repo-root `.agent-fleet.json`. Flags override it, and an unknown key is an
  error.
- **v1.8.1** corrects agent-fleet §5's liveness "why". Transcripts are not "written lazily". A tool call writes nothing
  until it returns, so every gate wait is a silence of about 580 s. And a session restart moves live agents' transcripts
  to the new session's directory. Entry 1752 records the same correction on this side.

**Parity, measured on this tree against the retired copies.**
- **`fix_clusters`:** 357 notes → 118 clusters on both sides.
  - Every membership difference traces to one of two causes.
  - The first is a project bug: a cluster emptied by absorption kept absorbing. That is why PB1160 sat under
    `ProgramEmitter.cs`, which is not its primary file. With only that bug fixed, 113 of 119 clusters are identical.
  - The second is real-file resolution. `Core/CobolData.g4` now counts, and stale paths no longer become cluster files.
- **`orient`:** identical content over 13 files, with added information only: per-rule cite detail, test file
  extensions, unquoted titles and `.g4` rules.

**The trap found on the way.** A `git worktree add` checks out NO submodules. Measured in a scratch repo: even with
`submodule.recurse=true`, the new worktree's `tools/claude-skills` was empty. So the fix covers every place the
scripts run:
- **Implementer worktrees.** The dispatch spec and MANDATORY-PRACTICES P6 tell a fresh worktree to run
  `git submodule update --init tools/claude-skills` once.
- **Cloud sessions.** The SessionStart hook inits the PUBLIC submodule first and on its own, so an unattached
  `specs-private` cannot cost it. Locally the hook stays read-only and prints the command.
- **CI.** The `audits` job inits only `tools/claude-skills`, never `--recursive`, and runs `check_practices.py`.
- **WSL.** Nothing is needed: it runs on the Windows checkout.

**Drift check.** `check_practices.py` fails, and each arm was seen to fail once, when:
- an overlay or PB1699 names a version other than the submodule's;
- `.agent-fleet.json` carries a key no fleet script accepts;
- a tracked file names a submodule script the pin lacks;
- anything outside DEVLOG and frozen evidence still calls a retired copy.

**Overlay trimming.** Only text the base carries equivalently was removed:
- workstream §5, the restart steps (the base's §12);
- gate's redirect, no-edit-during-gate and flake rules;
- review's verify, report and scale prose;
- spec-lookup's order-of-operations, specific-rule and read-the-code text.

Every dated owner decision, measurement and project command stays. Two stale statements were corrected:
- review filed findings to `CONFORMANCE-FIX-QUEUE.md`; they now go to kb/Work;
- spec-lookup called `specs/ISO_COBOL.md` a private submodule; it is tracked in this repo.

spec-lookup also gained `cite.py --check` and the owner's latitude precedence.

**Left for later (PB1699 stays `half`).**
- The rolling wave still runs the project's own `wf_rolling_wave.js`. The live wave-70 workflow reads it, so only its
  successor string changed. It migrates onto the public `rolling-wave.js` after wave 70.
- The main checkout needs `git submodule update --init tools/claude-skills` once after this lands. The SessionStart
  hook will print that command until it is run.

