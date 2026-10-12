## Entry 1679 — 2026-09-24 13:02 PDT — Cloud smoke session #2: the SessionStart hook never ran (two repos attached → cwd /home/user); user-level shim + per-clone corpus fetch

**What.** Smoke #2 (run `cse_016gWPp6F4K1t7mTHam44LhS`) verified Entry 1678: Python 3.14.7 on the VM (PASS),
.NET 10.0.401 + pwsh 7.6.6, `cite.py --check`, the `derive_verdict_batch` self-test, the solution build (1m06s,
0 warnings) and 27/27 `AcceptDifferentialTests` all PASS. Two FAILs, one root cause (kb/Work PB1521):

1. **The SessionStart hook never ran.** Attaching BrentRector/CobolSharp-private (required for the submodule)
   makes the platform start Claude Code in the PARENT of both clones — the run's init line says `cwd=/home/user`
   and its log says "Found 0 total hooks in registry" — so the repo's `.claude/settings.json` never loads.
   `specs-private` stayed uninitialized until the session ran `git submodule update --init` by hand (2.2 s).
2. **Unit 2 of 29,190 red: both `ExternalCorpusPopulationDriftTests`** (the sweep exits 1). The GnuCOBOL corpus under
   `tests/external/` is git-ignored and per CLONE; CI fetches it, the cloud never did. ⚠ **Correction to Entry
   1678:** it blamed these two on Python 3.11 — wrong; only `DerivedVerdictDriftTests` was Python's (green on 3.14).

**Fix.** `setup-env.sh` (snapshotted, so it can own only VM-level state) now (a) merges a USER-level SessionStart
hook into `~/.claude/settings.json` running the shim `/usr/local/bin/cobolsharp-session-start`, which runs the
repo's `session_start.py` only when the project dir is NOT the repo (a one-repo session keeps the project hook —
never twice); (b) pre-caches the pinned GnuCOBOL 3.2 tarball (the fetch script's URL + SHA-256) in
`/opt/cobolsharp-cache/`; (c) passes `--no-warn-script-location` to pip. `session_start.py`'s cloud branch now
copies that tarball into `tests/external/` and runs `fetch-gnucobol-tests.ps1` after the submodule init. Tested
locally: bash/py syntax, the settings merge (keeps foreign keys, idempotent over two runs), the shim's two branches.

**Owner action.** Re-paste `scripts/cloud/setup-env.sh` into the environment's Setup-script field, then cloud smoke
#3 (the routine is now NAMED per run — both earlier runs were titled "CobolSharp cloud smoke session"). Unverified
until then: that the platform leaves `~/.claude/settings.json` in the snapshot alone.

