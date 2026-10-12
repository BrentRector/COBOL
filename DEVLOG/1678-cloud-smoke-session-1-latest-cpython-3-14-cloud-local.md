## Entry 1678 — 2026-09-24 12:29 PDT — Cloud smoke session #1: latest CPython (3.14) cloud + local, private submodule needs the repo attached

**What.** The first cloud smoke session (routine `trig_01SHT8jdzjtKppx7PKx7YMwG`, environment CobolSharp
`env_01MwiJDLETctGoTMH2BKfWDw`, 4 cores / 15 GB) passed 8 of 10 checks: .NET SDK 10.0.401, pwsh 7.6.6, the hooks
through `$CLAUDE_PROJECT_DIR`, `cite.py --check`, the probe, `work.py`, the solution build (57 s, 0 warnings),
27/27 filtered Conformance tests, and both custom network hosts. Two findings, both in the environment setup, not the
compiler:

1. **Python 3.11.** The image ships `/usr/local/bin/python{,3}` → python3.11 AHEAD of `/usr/bin`, so
   `python-is-python3` left `python` at 3.11; the repo's scripts use PEP 701 f-strings (3.12+), and 3 of 29,190 Unit
   tests failed on it (`DerivedVerdictDriftTests.TheSelectorEngine_ProvesEveryAxisCanFail`, two
   `ExternalCorpusPopulationDriftTests`) — `derive_verdict_batch.py --self-test` raised `SyntaxError: unterminated
   string literal` under 3.11 and ran ALL GREEN under 3.12. **Owner decision: both the cloud VM and the dev box run
   the LATEST CPython (3.14).** `setup-env.sh` installs uv from PyPI and a standalone CPython 3.14 through it
   (fallback: the newest `/usr/bin/python3.N`, N ≥ 12), points `python`/`python3` in `/usr/local/bin` at it, and
   installs the repo's only third-party imports (PyMuPDF, fontTools). The dev box moved to python.org 3.14.7 and every
   older interpreter was removed (python.org 3.13.15, Store 3.13, uv-managed 3.14.6/3.12.13, an orphaned Anaconda3
   registration); `derive_verdict_batch.py --self-test`, `work.py check` and `cite.py --check` are green on 3.14.7.
   The cloud 3.14 path is unverified until smoke session #2.
2. **`specs-private` clone refused** ("could not read Username … terminal prompts disabled"): the cloud GitHub proxy
   serves only repositories attached to the session. Attaching `BrentRector/CobolSharp-private` made the same
   `git submodule update` succeed. `setup-env.sh`'s header and the SessionStart hook's failure message now say so;
   the routine's sources carry both repositories.

**Process note (transparency).** The first launch attempt used a remote-isolation subagent, which cannot select an
environment and in fact fell back to a LOCAL worktree; it was stopped and its worktree removed (no tracked changes).
Cloud sessions are launched through the routine API with the CobolSharp environment id.

**Owner action.** Re-paste `scripts/cloud/setup-env.sh` into the environment's Setup-script field (the file is the
canonical copy); attach `CobolSharp-private` to every interactive cloud session.

